using Assets.App.Scripts.Character;
using Assets.App.Scripts.Events;
using Assets.App.Scripts.Network;
using Assets.App.Scripts.State;
using FishNet.Object;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Character
{

    [RequireComponent(typeof(Rigidbody))]
    internal class Movement : NetworkBehaviour {
        internal enum State
        {
            Idling,
            Walking,
            Jumping,
            Falling
        }

        public event Action<State> StateChanged;

        public bool IsGrounded { get => _collidedGrounds > 0; }
        public bool InputEnabled =>
            GameStateManager.Instance.CurrentState == GameState.Running &&
            !_playerState.IsDead;

        [SerializeField]
        PlayerState _playerState;
        [SerializeField]
        private float _moveSpeed = 10f;
        [SerializeField]
        private float _jumpForce = 8f;

        private Rigidbody _rigidBody;
        private const string _groundTag = "Ground";
        private const float _minValidY = -50f;   
        private bool _isDead = false;
        private int _collidedGrounds = 0;

        private State _curState = State.Idling;
        private float _timeStateChanged;

        PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new();
            _rigidBody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            _playerState.PlayerRespawned += OnRespawn;

            _actions.Enable();
            _actions.Default.Jump.performed += OnJump;
        }

        private void OnDisable()
        {
            _playerState.PlayerRespawned -= OnRespawn;

            _actions.Disable();
            _actions.Default.Jump.performed -= OnJump;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner) this.enabled = false;
        }

        public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
        {
            _rigidBody.velocity = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
            SetState(State.Jumping);
        }

        [ServerRpc]
        private void RpcReportAboutDeath()
        {
            PlayersManager.Instance.HandleDeath(OwnerId);
        }

        private void OnRespawn(RespawnEvent e)
        {
            transform.position = e.newPosition;
            _rigidBody.isKinematic = false;
            _isDead = false;
        }

        private void OnDeath()
        {
            RpcReportAboutDeath();
            _isDead = true;
            _rigidBody.isKinematic = true;
        }

        private void FixedUpdate()
        {
            if (this.transform.position.y < _minValidY && !_isDead)
            {
                OnDeath();
                return;
            }

            Vector2 input = InputEnabled ? _actions.Default.Move.ReadValue<Vector2>() : Vector2.zero;

            if (IsGrounded)
            {
                if (input == Vector2.zero)
                    SetState(State.Idling);
                else if (!(_curState == State.Jumping && Time.time - _timeStateChanged < 0.1f))
                    SetState(State.Walking);
            }
            else if (_curState != State.Jumping)
                SetState(State.Falling);

             _rigidBody.MovePosition(_rigidBody.position + 
                    (transform.forward * input.y + transform.right * input.x) * _moveSpeed * Time.fixedDeltaTime);
        }

        private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
        {
            float gravity = Physics.gravity.y;
            float displacementY = endPoint.y - startPoint.y;
            Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

            Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
            Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
                + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

            return velocityXZ + velocityY;
        }

        private void OnJump(InputAction.CallbackContext callbackContext)
        {
            if (!base.IsOwner) return;
            if (!IsGrounded) return;

            _rigidBody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            SetState(State.Jumping);
        }

        private void SetState(State state)
        {
            if (_curState != state)
            {
                _curState = state;
                _timeStateChanged = Time.time;
                StateChanged?.Invoke(state);
                RpcSetState(state);
            }
        }

        [ServerRpc]
        private void RpcSetState(State state) => ObserversSetState(state);

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversSetState(State state) => StateChanged?.Invoke(state);

        private void OnCollisionEnter(Collision collisionInfo)
        {
            if (collisionInfo.gameObject.CompareTag(_groundTag))
                _collidedGrounds++;
        }

        private void OnCollisionExit(Collision collisionInfo)
        {
            if (collisionInfo.gameObject.CompareTag(_groundTag))
                _collidedGrounds--;
        }
    }
}
