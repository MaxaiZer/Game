using Assets.App.Scripts.UI;
using System;
using UnityEngine;

namespace Assets.App.Scripts.State
{
    internal enum GameState
    {
        Running,
        Paused
    }

    internal class GameStateManager: MonoBehaviour
    {
        [SerializeField]
        private PauseMenu _pauseMenu;
        [SerializeField]
        private SettingsMenu _settingsMenu;

        public static GameStateManager Instance { get; private set; }

        public event Action<GameState> GameStateChanged;

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            _pauseMenu.OnShowEvent.AddListener(UpdateGameState);
            _pauseMenu.OnHideEvent.AddListener(UpdateGameState);
            _settingsMenu.OnShowEvent.AddListener(UpdateGameState);
            _settingsMenu.OnHideEvent.AddListener(UpdateGameState);
        }

        private void OnDisable()
        {
            _pauseMenu.OnShowEvent.RemoveListener(UpdateGameState);
            _pauseMenu.OnHideEvent.RemoveListener(UpdateGameState);
            _settingsMenu.OnShowEvent.RemoveListener(UpdateGameState);
            _settingsMenu.OnHideEvent.RemoveListener(UpdateGameState);
        }

        private void UpdateGameState()
        {
            bool gamePaused = false;
            if (_pauseMenu.IsVisible || _settingsMenu.IsVisible)
                gamePaused = true;

            Debug.Log("NewState");

            if (gamePaused && CurrentState != GameState.Paused)
            {
                CurrentState = GameState.Paused;
                GameStateChanged?.Invoke(GameState.Paused);
            }

            if (!gamePaused && CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Running;
                GameStateChanged?.Invoke(GameState.Running);
            }
        }

    }
}
