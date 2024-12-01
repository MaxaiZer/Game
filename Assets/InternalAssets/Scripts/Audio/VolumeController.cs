using Assets.App.Scripts.Events;
using Assets.App.Scripts.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEventBus;

namespace Assets.App.Scripts.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class VolumeController : MonoBehaviour, IListener<VolumeChangedEvent>
    {
        enum AudioSourceType
        {
            Music,
            Sound
        }

        [SerializeField]
        private AudioSourceType _type;

        private AudioSource _source;

        private void Awake()
        {          
            _source = this.GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            GlobalBus.Subscribe(this);

            if (_type == AudioSourceType.Music)
                _source.volume = PlayerSettings.MusicVolume;
            else
                _source.volume = PlayerSettings.SoundsVolume;
        }

        private void OnDisable()
        {
            GlobalBus.UnSubscribe(this);
        }

        void IListener<VolumeChangedEvent>.React(in VolumeChangedEvent e)
        {
            if (_type == AudioSourceType.Music)
                _source.volume = e.musicVolume;
            else
                _source.volume = e.soundsVolume;
        }
    }
}
