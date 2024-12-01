using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace Assets.App.Scripts.Character
{
    [CreateAssetMenu(fileName = "Damage Config", menuName = "Guns/Damage Config", order = 1)]
    internal class DamageConfig : ScriptableObject
    {
        public MinMaxCurve damageCurve;

        private void Reset()
        {
            damageCurve.mode = ParticleSystemCurveMode.Curve;
        }

        public float CalculateDamage(float distance = 0)
        {
            return Mathf.CeilToInt(damageCurve.Evaluate(distance, UnityEngine.Random.value));
        }
    }
}
