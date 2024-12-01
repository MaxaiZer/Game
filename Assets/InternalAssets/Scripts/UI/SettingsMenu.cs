using UnityEngine;
using UnityEngine.UI;
using MasterServerToolkit.UI;

namespace Assets.App.Scripts.UI
{
    internal class SettingsMenu: UIView
    {
        [SerializeField]
        private Toggle _screenToggle;
        [SerializeField]
        private Slider _sensitivityScrollBar;
        [SerializeField]
        private Slider _musicVolumeSlider;
        [SerializeField]
        private Slider _soundsVolumeSlider;

        private readonly float _minSensitivity = 0.3f;
        private readonly float _maxSensitivity = 2.5f;

        protected override void Awake()
        {
            base.Awake();

            _sensitivityScrollBar.minValue = _minSensitivity;
            _sensitivityScrollBar.maxValue = _maxSensitivity;
        }

        private void OnEnable()
        {
            _sensitivityScrollBar.value = Settings.PlayerSettings.Sensitivity;
            _screenToggle.isOn = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
            _musicVolumeSlider.value = Settings.PlayerSettings.MusicVolume;
            _soundsVolumeSlider.value = Settings.PlayerSettings.SoundsVolume;
        }

        public void SaveSensitivity(float value)
        {
            Settings.PlayerSettings.Sensitivity = value;
        }

        public void SaveFullscreen(bool isFullscreen)
        {
            Screen.fullScreenMode = isFullscreen ? 
                FullScreenMode.FullScreenWindow : 
                FullScreenMode.Windowed;
        }

        public void SaveMusicVolume(float value)
        {
            Debug.Log("Saved music : " + value);
            Settings.PlayerSettings.MusicVolume = value;
        }

        public void SaveSoundsVolume(float value)
        {
            Settings.PlayerSettings.SoundsVolume = value;
        }
    }
}
