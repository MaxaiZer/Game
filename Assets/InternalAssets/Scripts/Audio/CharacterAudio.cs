using Assets.App.Scripts.Settings;
using Assets.Scripts.Character;
using System.Collections;
using UnityEngine;

namespace Assets.App.Scripts.Audio
{
    [RequireComponent(typeof(AudioSource))]
    internal class CharacterAudio: MonoBehaviour
    {
        [SerializeField]
        private Movement _movement;
        [SerializeField]
        private AudioClip _walkClip;
        [SerializeField]
        private AudioClip _jumpClip;

        private AudioSource _source;

        private Coroutine _nextClipCoroutine;
        private Movement.State _curState;

        private void Awake()
        {
            _source = this.GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _movement.StateChanged += OnMovementStateChange;
        }

        private void OnDisable()
        {
            _movement.StateChanged -= OnMovementStateChange;
        }

        private void OnMovementStateChange(Movement.State state)
        {
            _curState = state;

            if (state == Movement.State.Idling)
            {
                if (_source.clip == _walkClip)
                    _source.Stop();
            }             
            if (state == Movement.State.Walking)
            {
                SetNewClipAfterCurrent(Movement.State.Walking, _walkClip, true);
            }
            if (state == Movement.State.Jumping)
            {
                // _source.clip = _jumpClip;
                // _source.loop = false;
                // _source.Play();
                _source.Stop();
            }      
            if (state == Movement.State.Falling)
            {
                _source.Stop();
            }
        }

        private void SetNewClipAfterCurrent(Movement.State targetState, AudioClip clip, bool isLooping)
        {
            if (_nextClipCoroutine != null)
                StopCoroutine(_nextClipCoroutine);

            _nextClipCoroutine = StartCoroutine(PlayClipAfterCurrent(targetState, clip, isLooping));
        }

        private IEnumerator PlayClipAfterCurrent(Movement.State targetState, AudioClip clip, bool isLooping)
        {
            yield return new WaitUntil(() => !_source.isPlaying);
            if (_curState != targetState) yield break;

            _source.clip = clip;
            _source.loop = isLooping;
            _source.Play();
        }

    }
}
