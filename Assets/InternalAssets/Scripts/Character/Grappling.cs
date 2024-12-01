using Assets.App.Scripts.Events;
using Assets.Scripts.Character;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Character
{
    [RequireComponent(typeof(Movement))]
    internal class Grappling: NetworkBehaviour
    {
        [SerializeField]
        PlayerState _playerState;
        [SerializeField]
        private LayerMask _whatIsGrappleable;
        [SerializeField]
        private float _maxGrappleDistance = 30;
        [SerializeField]
        private float _grappleDelayTime = 0.2f;
        [SerializeField]
        private float _overshootYAxis = 2;
        [SerializeField]
        private float _grappleCooldown = 2;
        [SerializeField]
        private LineRenderer _lineRenderer;
        [SerializeField]
        private Transform _lineRenderStart;

        private Movement _movement;
        private Vector3 _grapplePoint;
        private bool _canGrapple = true;
        private bool _isGrappling = false;

        PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new();
            _movement = GetComponent<Movement>();
        }

        private void OnEnable()
        {
            _actions.Enable();
            _playerState.PlayerDead += OnDeath;
        }

        private void OnDisable()
        {
            _actions.Disable();
            _playerState.PlayerDead -= OnDeath;
        }

        private void Update()
        {
            if (!base.IsOwner) return;

            if (_actions.Default.Grapple.triggered)
                PrepareGrapple();
        }

        private void LateUpdate()
        {
             if (_isGrappling)
                _lineRenderer.SetPosition(0, _lineRenderStart.position);
        }

        private void OnDeath()
        {
            if (base.IsOwner && _isGrappling)
                StopGrapple();
        }

        [ServerRpc]
        private void ServerOnStartGrabble(Vector3 grapplePoint) =>
            ObserversOnStartGrabble(grapplePoint);

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversOnStartGrabble(Vector3 grapplePoint)
        {
            _isGrappling = true;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(1, grapplePoint);
        }

        [ServerRpc]
        private void ServerOnStopGrabble() => ObserversOnStopGrabble();

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversOnStopGrabble()
        {
            _isGrappling = false;
            _lineRenderer.enabled = false;
        }

        private void PrepareGrapple()
        {
            if (!_canGrapple) return;         

            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 
                _maxGrappleDistance, _whatIsGrappleable))
            {
                return;
            }

            _isGrappling = true;
            _grapplePoint = hit.point;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(1, _grapplePoint);

            StartCoroutine(CanGrappleUpdater());
            Invoke(nameof(StartGrapple), _grappleDelayTime);

            ServerOnStartGrabble(_grapplePoint);
        }

        private void StartGrapple()
        {
            //    pm.freeze = false;
            Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
            float grapplePointRelativeYPos = _grapplePoint.y - lowestPoint.y;
            float highestPointOnArc = grapplePointRelativeYPos + _overshootYAxis;

            if (grapplePointRelativeYPos < 0) highestPointOnArc = _overshootYAxis;

            _movement.JumpToPosition(_grapplePoint, highestPointOnArc);

            Invoke(nameof(StopGrapple), 1f);
        }

        private void StopGrapple()
        {
            //  pm.freeze = false;
            Debug.Log("Stopped grapple");
            _isGrappling = false;
            _lineRenderer.enabled = false;
            ServerOnStopGrabble();
        }

        private IEnumerator CanGrappleUpdater()
        {
            _canGrapple = false;
            yield return new WaitForSeconds(_grappleCooldown);
            _canGrapple = true;
        }


    }
}
