using Assets.App.Scripts.Character;
using UnityEngine;

namespace Assets.App.Scripts.Animations
{
    [RequireComponent(typeof(Animator), typeof(Gun))]
    internal class GunAnimator: MonoBehaviour
    {
        public float enablingAnimationSpeed;
        public float reloadAnimationSpeed;

        private Animator _animator;
        private Gun _gun;

        private void Awake()
        {
            _animator = this.GetComponent<Animator>();
            _gun = this.GetComponent<Gun>();
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {

        }

        public bool IsReloadAnimationPlaying()
            => _animator.GetBool(GunAnimatorKeys.isReloadKey);

        private void OnFire()
        {
            _animator.SetBool(GunAnimatorKeys.isFiringKey, true);
        }

        private void OnReload()
        {
            _animator.SetBool(GunAnimatorKeys.isReloadKey, true);
        }

    }
}
