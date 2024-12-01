using Assets.App.Scripts.Input;
using Assets.Scripts.Character;
using FishNet.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class WeaponBobbing: MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _rb;
        [SerializeField]
        private Movement _movement;

        [SerializeField]
        private float _sharpness = 6f;
        [SerializeField]
        private float _bobAmount = 0.02f;
        [SerializeField]
        private float _frequency = 8;

        private float _bobFactor = 0;
        private PlayerInputActions _actions;

        private void Awake() => _actions = new();

        private void OnEnable() => _actions.Enable();

        private void OnDisable() => _actions.Disable();

        private void Update()
        {
            Vector2 movementInput = _movement.InputEnabled ? _actions.Default.Move.ReadValue<Vector2>() : Vector2.zero;
            float movementFactor = 0;

            if (_movement.IsGrounded && movementInput.magnitude > 0)
            {
                movementFactor = 1;
            }

            _bobFactor = Mathf.Lerp(_bobFactor, movementFactor, _sharpness * Time.deltaTime);

            float hBobValue = Mathf.Sin(Time.time * _frequency) * _bobAmount * _bobFactor;
            float vBobValue = ((Mathf.Sin(Time.time * _frequency * 2f) * 0.5f) + 0.5f) * _bobAmount *
                              _bobFactor;

            this.transform.localPosition = new Vector3(hBobValue, Mathf.Abs(vBobValue));
        }
    }
}
