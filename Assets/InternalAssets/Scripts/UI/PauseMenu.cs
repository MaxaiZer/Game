using MasterServerToolkit.MasterServer;
using MasterServerToolkit.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.App.Scripts.UI
{
    internal class PauseMenu: UIView
    {
        public event Action GamePaused;

        [SerializeField]
        private UIView _settingsMenu;

        private bool _isPaused = false;
        private PlayerInputActions _actions;

        protected override void Awake()
        {
            base.Awake();
            _actions = new();
        }

        private void OnEnable()
        {
            _actions.Enable();
            _actions.Menu.Pause.performed += OnPauseButtonPressed;
        }

        private void OnDisable()
        {
            _actions.Disable();
            _actions.Menu.Pause.performed -= OnPauseButtonPressed;
        }

        public void ExitGame()
        {
            Mst.Events.Invoke(MstEventKeys.leaveRoom);
        }

        public void OpenSettingsMenu()
        {
            Hide();
            _settingsMenu.Show();
        }

        private void OnPauseButtonPressed(InputAction.CallbackContext context)
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                Show();

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Hide();
                _settingsMenu.Hide();

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
                
        }
    }
}
