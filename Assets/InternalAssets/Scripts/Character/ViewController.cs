using Assets.App.Scripts.Character;
using Assets.App.Scripts.Events;
using FishNet.Object;
using System;
using UnityEngine;
using UnityEventBus;
using Assets.App.Scripts.Input;
using Assets.App.Scripts.Settings;
using Assets.App.Scripts.State;

namespace Assets.Scripts
{
    [RequireComponent(typeof(GunsController))]
    internal class ViewController: NetworkBehaviour, IListener<MouseSensitivityChangedEvent>
    {
        [SerializeField]
        PlayerState _playerState;
        [SerializeField]
        private Transform cameraPoint;
        [SerializeField]
        private int maxLookAngle;
        [SerializeField] 
        private int minLookAngle;

        private Camera _camera;
        private float _sensitivity = 1f;

        [Header("Smoothness Settings"), SerializeField]
        protected bool useSmoothness = true;

        [SerializeField, Range(0.01f, 1f)]
        protected float smoothnessTime = 0.1f;

        private Vector3 cameraRotation;
        private Vector3 currentCameraRotationVelocity;
        private Vector3 smoothedCameraRotation;

        private PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new();        
        }

        private void OnEnable()
        {
            GlobalBus.Subscribe(this);
            _sensitivity = PlayerSettings.Sensitivity;
            _actions.Enable();
        }

        private void OnDisable()
        {
            GlobalBus.UnSubscribe(this);
            _actions.Disable();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsOwner)
            {
                if (Camera.main == null)
                    Debug.LogError("Main camera is required in a scene");

                _camera = Camera.main;
                _camera.transform.parent = this.transform;
                this.GetComponent<GunsController>().SetGunsParentAsCamera(_camera.transform);

                _camera.transform.position = cameraPoint.position;

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                this.enabled = false;
            }
        }

        public void React(in MouseSensitivityChangedEvent e)
        {
            _sensitivity = e.newSensitivity;
        }

        public void RecoilShift(float shiftVectorLength)
        {
            Vector2 recoil = new(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0, 1f));
            recoil = recoil.normalized * shiftVectorLength;

            cameraRotation.y += recoil.x * _sensitivity;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x - recoil.y * _sensitivity, minLookAngle, maxLookAngle);
        }

        private bool InputEnabled() => GameStateManager.Instance.CurrentState == GameState.Running;

        private void UpdateRotation(Vector2 input)
        {
            cameraRotation.y += input.x * _sensitivity * 2 * Time.fixedDeltaTime;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x - input.y * _sensitivity * 2 * Time.fixedDeltaTime, 
                minLookAngle, maxLookAngle);

            if (useSmoothness)
            {
                transform.rotation = Quaternion.Euler(0f, smoothedCameraRotation.y, 0f);
                smoothedCameraRotation = Vector3.SmoothDamp(smoothedCameraRotation, cameraRotation, 
                    ref currentCameraRotationVelocity, smoothnessTime);
                _camera.transform.rotation = Quaternion.Euler(smoothedCameraRotation.x, smoothedCameraRotation.y, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, cameraRotation.y, 0f);
                _camera.transform.rotation = Quaternion.Euler(cameraRotation.x, cameraRotation.y, 0f);
            }
        }

        private void Update()
        {
            if (_camera == null) return;
            if (!InputEnabled()) return;

            UpdateRotation(_actions.Default.Mouse.ReadValue<Vector2>());    
        }
    }
}
