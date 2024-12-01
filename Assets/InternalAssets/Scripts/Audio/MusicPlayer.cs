using Assets.App.Scripts.Network;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.App.Scripts.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioClip[] _songs = new AudioClip[0];

        private AudioSource _source;
        private int _currentSongIdx = 0;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _songs.Shuffle();
        }

        private void Update()
        {
            if (!_source.isPlaying)
                PlayNextSong();
        }

        private void PlayNextSong()
        {
            _currentSongIdx = (_currentSongIdx + 1) % _songs.Length;          
            PlaySongWithIdx(_currentSongIdx);
        }

        private void PlaySongWithIdx(int idx)
        {
            _source.clip = _songs[idx];
            _source.Play();
        }
    }
}
