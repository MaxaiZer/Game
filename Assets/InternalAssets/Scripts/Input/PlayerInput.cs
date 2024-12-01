/*using Assets.App.Scripts.Events;
using Assets.App.Scripts.State;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Input
{
    internal class PlayerInput : MonoBehaviour,
        IListener<RespawnEvent>,
        IListener<DeathEvent>
    {
        public static PlayerInput Instance { get; private set; }

        public PlayerInputActions Actions { get; private set; }

        private bool _gamePaused = false;
        private bool _playerDead = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Actions = new PlayerInputActions();
            Actions.Enable();
        }

        private void OnEnable()
        {
            GlobalBus.Subscribe(this);
            GameStateManager.Instance.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GlobalBus.UnSubscribe(this);
            GameStateManager.Instance.GameStateChanged -= OnGameStateChanged;
        }

        public void React(in RespawnEvent e)
        {
            _playerDead = false;
            SetInput();
        }

        public void React(in DeathEvent e)
        {
            _playerDead = true;
            SetInput();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused)
                _gamePaused = true;
            else
                _gamePaused = false;

            SetInput();
        }

        private void SetInput()
        {
            Actions.Disable();
            Actions.Menu.Pause.Enable();

            if (_gamePaused)
                return;

            if (_playerDead)
            {
                Actions.Default.Mouse.Enable();
                return;
            }

            Actions.Enable();
        }

    }
}
*/