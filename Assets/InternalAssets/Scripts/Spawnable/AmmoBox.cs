using FishNet;
using FishNet.Object;
using System;
using UnityEngine;

namespace Assets.App.Scripts.Character
{
    internal class AmmoBox: NetworkBehaviour
    {
        public event Action<AmmoBox> PlayerInteracted;
        [SerializeField]
        private int _ammoMagazinesToAdd = 3;

        private bool _playerInteracted = false;

        private void OnEnable()
        {
            _playerInteracted = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!InstanceFinder.IsServer) return;
            if (_playerInteracted) return;

            if (other.gameObject.CompareTag("Player"))
            {
                _playerInteracted = true;
                var controller = other.gameObject.GetComponent<GunsController>();
                controller.AddAmmoToGuns(controller.Owner, _ammoMagazinesToAdd);
                PlayerInteracted?.Invoke(this);               
            }
        }
    }
}
