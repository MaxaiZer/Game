using Assets.App.Scripts.Events;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Settings
{
    internal static class PlayerSettings
    {
        private static readonly float _defaultVolume = 0.5f;
        private static readonly float _defaultSensitivity = 1f;

        private static string _mouseSensitivityKey = "mouse_sensitivity";
        private static string _musicVolumeKey = "music_volume";
        private static string _soundsVolumeKey = "sounds_volume";

        public static float Sensitivity 
        {  
            get 
            {
                return PlayerPrefs.HasKey(_mouseSensitivityKey) ?
                    PlayerPrefs.GetFloat(_mouseSensitivityKey) :
                    _defaultSensitivity;
            } 
            set
            {
                GlobalBus.Send(new MouseSensitivityChangedEvent { newSensitivity = value });
                PlayerPrefs.SetFloat(_mouseSensitivityKey, value);
                PlayerPrefs.Save();
            }        
        }

        public static float MusicVolume
        {
            get
            {
                return PlayerPrefs.HasKey(_musicVolumeKey) ?
                    PlayerPrefs.GetFloat(_musicVolumeKey) :
                    _defaultVolume;
            }
            set
            {
                GlobalBus.Send(new VolumeChangedEvent { musicVolume = value, soundsVolume = SoundsVolume });
                PlayerPrefs.SetFloat(_musicVolumeKey, value);
                PlayerPrefs.Save();
            }
        }

        public static float SoundsVolume
        {
            get
            {
                return PlayerPrefs.HasKey(_soundsVolumeKey) ?
                    PlayerPrefs.GetFloat(_soundsVolumeKey) :
                    _defaultVolume;
            }
            set
            {
                GlobalBus.Send(new VolumeChangedEvent { musicVolume = MusicVolume, soundsVolume = value });
                PlayerPrefs.SetFloat(_soundsVolumeKey, value);
                PlayerPrefs.Save();
            }
        }
    }
}
