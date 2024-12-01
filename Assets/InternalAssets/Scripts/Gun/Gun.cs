using Assets.App.Scripts.State;
using Assets.Scripts;
using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.App.Scripts.Character
{
    [RequireComponent(typeof(AudioSource), typeof(Animator))]
    internal class Gun: NetworkBehaviour
    {
        public PlayerState _playerState;
        public event Action<Gun> AmmoChanged;
        public Vector3 positionRelativeToCamera = new Vector3(1f, 0f, 0);

        public int CurrentMagazineSize => _currentMagazineSize;
        public int CurrentAmmoTotal => _ammoTotal;

        [SerializeField]
        private ViewController _viewController;
        [SerializeField]
        private AudioClip _fireClip;
        [SerializeField]
        private ParticleSystem _muzzleFlash;
        [SerializeField]
        private float _recoil;
        [SerializeField]
        private DamageConfig _damageConfig;
        [SerializeField]
        private float _fireRate;
        [SerializeField]
        private int _magazineSize, _ammoTotal;
        [SerializeField]
        private bool _isAutomatic;

        private int _ammoTotalCopy;
        private int _currentMagazineSize;
        private AudioSource _audioSource;
        private Animator _animator;
        private int _raycastMaxDistance = 100;

        private float _lastShootTime = 0f;

        private Func<bool> _needShoot;
        private PlayerInputActions _actions;
        private bool InputEnabled => 
            GameStateManager.Instance.CurrentState == GameState.Running && 
            !_playerState.IsDead;

        private void Awake()
        {
            _currentMagazineSize = _magazineSize;
            _audioSource = this.GetComponent<AudioSource>();
            _animator = this.GetComponent<Animator>();
            _ammoTotalCopy = _ammoTotal;

            _actions = new();

            if (_isAutomatic)
                _needShoot = () => _actions.Default.Fire.IsPressed();
            else
                _needShoot = () => _actions.Default.Fire.triggered;
        }

        private void OnEnable()
        {
            _actions.Enable();
            _actions.Default.Reload.performed += OnReloadButtonPressed;
        }

        private void OnDisable()
        {
            _actions.Disable();
            _actions.Default.Reload.performed -= OnReloadButtonPressed;
        }

        public void RestoreAmmo()
        {
            _ammoTotal = _ammoTotalCopy;
            _currentMagazineSize = _magazineSize;
            AmmoChanged?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (base.IsOwner)
            {
                foreach (var child in this.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = 3; //playerWeapon
                }

                _animator.enabled = true;
            }
            else
            {
                //this.enabled = false;            
            }             
        }

        public void AddAmmo(int magazinesCount)
        {
            if (magazinesCount > 0)              
            {
                _ammoTotal += _magazineSize * magazinesCount;
                AmmoChanged?.Invoke(this);
            }             
        }

        private bool CanShoot()
        {
            return InputEnabled &&
                (Time.time - _lastShootTime) > (1f / _fireRate) &&
                _currentMagazineSize != 0 &&
                !_animator.GetBool(GunAnimatorKeys.isReloadKey) &&
                _animator.GetBool(GunAnimatorKeys.isReadyKey);
        }

        private bool CanReload()
        {
            return _currentMagazineSize != _magazineSize
                && _ammoTotal > 0
                && !_animator.GetBool(GunAnimatorKeys.isReloadKey);
        }

        private void OnReloadButtonPressed(InputAction.CallbackContext context)
        {
            if (!enabled) return;
            if (!InputEnabled) return; 
            if (!base.IsOwner) return;
            if (!CanReload()) return;

            _animator.SetBool(GunAnimatorKeys.isReloadKey, true);
            StartCoroutine(UpdateAmmoAfterReloadEnd());
        }

        private void UpdateAmmoOnReload()
        {
            _ammoTotal += _currentMagazineSize;

            int newMagazine = _ammoTotal > _magazineSize ? _magazineSize : _ammoTotal;
            _ammoTotal -= newMagazine;
            _currentMagazineSize = newMagazine;

            AmmoChanged?.Invoke(this);
        }

        private void Update()
        {
            if (_needShoot())
            {
                if (CanShoot())
                {
                    Shoot();
                }              
            }              
        }

        private void Shoot()
        {        
            _lastShootTime = Time.time;

            _currentMagazineSize--;
            AmmoChanged?.Invoke(this);

            _viewController.RecoilShift(_recoil);

            _audioSource.PlayOneShot(_fireClip);
            _muzzleFlash.Play();
            _animator.SetBool(GunAnimatorKeys.isFiringKey, true);

            RpcVisualizeShoot(); 
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
                out hit, _raycastMaxDistance))
                return;

            HandleRaycastHit(hit);
            //RpcRaycast(Camera.main.transform.position, Camera.main.transform.forward);
        }

        private void HandleRaycastHit(RaycastHit hit)
        {
            Debug.Log("Damage: " + (int)_damageConfig.CalculateDamage(hit.distance));

            var health = hit.transform.gameObject.GetComponent<HealthController>();
            if (!health) return;

            health.RpcTakeDamage(OwnerId, (int)_damageConfig.CalculateDamage(hit.distance));

            var bloodSpawner = hit.transform.gameObject.GetComponent<BloodSpawner>();
            if (!bloodSpawner) return;

            bloodSpawner.RpcSpawn(Camera.main.transform.position, hit.point);
        }

        [ServerRpc]
        private void RpcRaycast(Vector3 start, Vector3 forwardVector)
        {
            ObserversVisualizeShoot();
            long ping = this.TimeManager.RoundTripTime;

            RaycastHit hit;
            if (!Physics.Raycast(start, forwardVector, out hit, _raycastMaxDistance))
                return;

            var health = hit.transform.gameObject.GetComponent<HealthController>();
            if (!health) return;

            health.ServerTakeDamage(OwnerId, (int)_damageConfig.CalculateDamage(hit.distance));

            var bloodSpawner = hit.transform.gameObject.GetComponent<BloodSpawner>();
            if (!bloodSpawner) return;

            //bloodSpawner.RpcSpawn(Camera.main.transform.position, hit.point);
            bloodSpawner.SpawnForAllPlayers(Camera.main.transform.position, hit.point);        
        }

        [ServerRpc]
        private void RpcVisualizeShoot()
        {
            ObserversVisualizeShoot();
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversVisualizeShoot()
        {
            _audioSource.PlayOneShot(_fireClip);
            _muzzleFlash.Play();
        }

        private IEnumerator UpdateAmmoAfterReloadEnd()
        {
            yield return new WaitUntil(() => !_animator.GetBool(GunAnimatorKeys.isReloadKey));
            UpdateAmmoOnReload();
        }
    }
}
