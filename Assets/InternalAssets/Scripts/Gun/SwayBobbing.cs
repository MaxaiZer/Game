using Assets.App.Scripts.Events;
using Assets.App.Scripts.Input;
using Assets.Scripts.Character;
using FishNet.Object;
using System;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEventBus;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Assets.App.Scripts.Character
{
    internal class SwayBobbing: NetworkBehaviour
    {
        [SerializeField]
        private bool _enableSway;
        [SerializeField]
        private bool _enableBobbing;
        [SerializeField]
        private Rigidbody _rb;
        [SerializeField]
        private Movement _movement;

        [SerializeField]
        private float _smooth = 10f;
        [SerializeField]
        private float _smoothRot = 12f;

        [SerializeField]
        private float _bobSpeed = 1f;

        private Vector2 _movementInput;
        private Vector2 _mouseInput;

        #region sway

        private float _step = 0.01f;
        private float _maxStepDistance = 0.06f;
        private Vector3 _swayPos;

        private float _rotationStep = 4f;
        private float _maxRotationStep = 5f;
        private Vector3 _swayEulerRot;

        #endregion

        #region bob

        private float _speedCurve;
        private float CurveSin => Mathf.Sin(_speedCurve);
        private float CurveCos => Mathf.Cos(_speedCurve);

        private Vector3 _travelLimit = Vector3.one * 0.025f;
        private Vector3 _bobLimit = Vector3.one * 0.01f;

        private Vector3 _bobPosition;
        private Vector3 _bobEulerRotation;

        #endregion

        PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new();
        }

        private void OnEnable() => _actions.Enable();

        private void OnDisable() => _actions.Disable();

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner) this.enabled = false;
        }

        private void GetInput()
        {
            _movementInput = _movement.InputEnabled ? _actions.Default.Move.ReadValue<Vector2>() : Vector2.zero;
            _mouseInput = _movement.InputEnabled ? _actions.Default.Mouse.ReadValue<Vector2>() : Vector2.zero;
        }

        private void Sway()
        {
            Vector2 invertLook = _mouseInput * -_step;
            invertLook.x = Math.Clamp(invertLook.x, -_maxStepDistance, _maxStepDistance);
            invertLook.y = Math.Clamp(invertLook.y, -_maxStepDistance, _maxStepDistance);

            _swayPos = invertLook;
        }

        private void SwayRotation()
        {
            Vector2 invertLook = _mouseInput * -_rotationStep;
            invertLook.x = Math.Clamp(invertLook.x, -_maxRotationStep, _maxRotationStep);
            invertLook.y = Math.Clamp(invertLook.y, -_maxRotationStep, _maxRotationStep);

            _swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
        }

        private void CompositePositionRotation()
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, 
                _swayPos + _bobPosition, Time.deltaTime * _smooth);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, 
                Quaternion.Euler(_swayEulerRot) * Quaternion.Euler(_bobEulerRotation), Time.deltaTime * _smoothRot);
        }

        private void BobOffset()
        {
            _speedCurve += Time.deltaTime * (_movement.IsGrounded ? _rb.velocity.magnitude : 1f) + 0.01f;

            _bobPosition.x = (CurveCos * _bobLimit.x * (_movement.IsGrounded ? 1 : 0))
                - (_movementInput.x * _travelLimit.x);

            _bobPosition.y = (CurveSin * _bobLimit.y) - (_rb.velocity.y * _travelLimit.y);
            _bobPosition.z = - (_movementInput.y * _travelLimit.y);
        }

        private void BobRotation()
        {
            _bobEulerRotation.x = _movementInput != Vector2.zero ? _bobSpeed * Mathf.Sin(2 * _speedCurve) :
               // _bobSpeed * (Mathf.Sin(2 * _speedCurve));
               0;

            _bobEulerRotation.y = _movementInput != Vector2.zero ? _bobSpeed * CurveCos : 0;
            _bobEulerRotation.z = _movementInput != Vector2.zero ? _bobSpeed * CurveCos : _movementInput.x;
        }

        private void Update()
        {
            GetInput();

            if (_enableSway)
            { 
                Sway();
                SwayRotation();
            }

            if (_enableBobbing)
            {
                BobOffset();
                BobRotation();
            }

            CompositePositionRotation();
        }
    }
}
