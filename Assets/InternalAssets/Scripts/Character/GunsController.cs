using Assets.App.Scripts.Events;
using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEventBus;

namespace Assets.App.Scripts.Character
{
    internal class GunsController: NetworkBehaviour
    {
        public List<Gun> guns = new();
        public GameObject gunsHolder;

        [SerializeField]
        PlayerState _playerState;

        private int _selectedGunIdx;
        private float _lastTimeWeaponChanged = 0;
        private float _changeWeaponCooldown = 0.3f;
        private PlayerInputActions _actions;

        private void Awake()
        {
            _actions = new();
           // InitializeGuns();
        }

        private void OnEnable()
        {
            _playerState.PlayerDead += OnDie;

            _actions.Enable();
            _actions.Default.SelectWeapon.performed += TrySelectGunByIdx;
            _actions.Default.ChangeWeapon.performed += TrySelectGunByScroll;

            guns.ForEach(gun => gun.AmmoChanged += OnAmmoChanged);
        }

        private void OnDisable()
        {
            _playerState.PlayerDead -= OnDie;

            _actions.Disable();
            _actions.Default.SelectWeapon.performed -= TrySelectGunByIdx;
            _actions.Default.ChangeWeapon.performed -= TrySelectGunByScroll;

            guns.ForEach(gun => gun.AmmoChanged -= OnAmmoChanged);
        }

        //set certain guns not active only after start client (not in awake!!!), otherwise fishnet will deactivate network object :(
        public override void OnStartClient()
        {
            base.OnStartClient();
            InitializeGuns();
        }

        public void SetGunsParentAsCamera(Transform camera)
        {
            gunsHolder.transform.parent = camera;
            gunsHolder.transform.localPosition = Vector3.zero;

            foreach (var gun in guns)
            {
                gun.transform.localPosition = gun.positionRelativeToCamera;
            }
        }

        [TargetRpc]
        public void AddAmmoToGuns(NetworkConnection conn, int magazinesCount)
        {
            guns.ForEach(gun => gun.AddAmmo(magazinesCount));
        }

        private void OnDie()
        {
            if (base.IsOwner)
                guns.ForEach(gun => gun.RestoreAmmo());
        }

        private void TrySelectGunByIdx(InputAction.CallbackContext context)
        {
            if (!base.IsOwner) return;    

            int idx = (int)context.ReadValue<float>();
            TryChangeGun(idx);
        }

        private void TrySelectGunByScroll(InputAction.CallbackContext context)
        {
            if (!base.IsOwner) return;
   
            float z = context.ReadValue<float>();
            int newGunIdx = 0;

            if (z > 0)
                newGunIdx = _selectedGunIdx + 1;
            else if (z < 0)
                newGunIdx = _selectedGunIdx - 1;

            if (newGunIdx < 0) newGunIdx = guns.Count - 1;
            if (newGunIdx >= guns.Count) newGunIdx = 0;

            TryChangeGun(newGunIdx);
        }

        private void InitializeGuns()
        {
            _selectedGunIdx = 0;
            guns[0].gameObject.SetActive(true);

            for (int i = 1; i < guns.Count; i++)
            {
                guns[i].gameObject.SetActive(false);
            }

            InvokeAmmoChangedEvent(guns[0]);
            guns.ForEach(gun => { gun._playerState = _playerState; });
        }

        private void TryChangeGun(int newGunIdx)
        {
            if (newGunIdx < 0 || 
                newGunIdx >= guns.Count || 
                newGunIdx == _selectedGunIdx) 
                return;

            if (Time.time - _lastTimeWeaponChanged < _changeWeaponCooldown) return;
            _lastTimeWeaponChanged = Time.time;

            ChangeGun(newGunIdx);
            RpcChangeGun(newGunIdx);

            InvokeAmmoChangedEvent(guns[newGunIdx]);
        }

        private void ChangeGun(int newGunIdx)
        {
            guns[_selectedGunIdx].gameObject.SetActive(false);
            guns[newGunIdx].gameObject.SetActive(true);
            _selectedGunIdx = newGunIdx;
        }

        [ServerRpc]
        private void RpcChangeGun(int newGunIdx) => ObserversChangeGun(newGunIdx);

        [ObserversRpc(ExcludeOwner = true, BufferLast = true)]
        private void ObserversChangeGun(int newGunIdx) => ChangeGun(newGunIdx);

        private void OnAmmoChanged(Gun gun)
        {
            if (gun == guns[_selectedGunIdx])
            {
                InvokeAmmoChangedEvent(gun);
            }
        }

        private void InvokeAmmoChangedEvent(Gun gun)
        {
            GlobalBus.Send(new SelectedGunAmmoChangedEvent
            {
                magazineAmmoLeft = gun.CurrentMagazineSize,
                ammoTotal = gun.CurrentAmmoTotal
            });
        }
    }
}
