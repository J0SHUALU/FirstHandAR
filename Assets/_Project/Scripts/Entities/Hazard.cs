using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Entities
{
    /// <summary>
    /// A hazard (or harmless decoy) in the trainee's room. Pooled: it is reused on every retry, so all of its
    /// state is reset in OnSpawned. Once found, a dangerous hazard is "made safe": its danger visuals switch off,
    /// a safe visual (warning sign, coiled cable, closed valve) switches on and any motion stops.
    /// </summary>
    public class Hazard : SimEntity, IPoolable
    {
        [SerializeField] private bool isDangerous = true;
        [SerializeField] private GameObject dangerVisual;
        [SerializeField] private GameObject safeVisual;
        [SerializeField] private GameObject foundRing;
        [Header("Optional motion (moving trolley)")]
        [SerializeField] private Vector3 patrolOffset;
        [SerializeField] private float patrolSpeed;
        [Header("Optional effect (smoke, gas)")]
        [SerializeField] private ParticleSystem effect;

        public bool IsDangerous => isDangerous;
        public bool Found { get; private set; }

        private Vector3 basePos;
        private float wobble;

        /// <summary>The pool hands out generic prefabs; the id is assigned per attempt by EntityManager.</summary>
        public void Assign(string id) => entityId = id;

        public void OnSpawned()
        {
            basePos = transform.localPosition;
            ResetEntity();
        }

        public void OnDespawned()
        {
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public override void ResetEntity()
        {
            Found = false;
            wobble = 0;
            if (dangerVisual != null) dangerVisual.SetActive(true);
            if (safeVisual != null) safeVisual.SetActive(false);
            if (foundRing != null) foundRing.SetActive(false);
            if (effect != null && isDangerous) effect.Play();
        }

        public override void OnTapped(Vector3 worldPoint)
        {
            if (!isDangerous) { wobble = 1f; return; }
            if (Found) return;
            Found = true;
            if (dangerVisual != null && safeVisual != null) dangerVisual.SetActive(false);
            if (safeVisual != null) safeVisual.SetActive(true);
            if (foundRing != null) foundRing.SetActive(true);
            if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void Update()
        {
            if (!Found && patrolSpeed > 0f)
                transform.localPosition = basePos + patrolOffset * Mathf.Sin(Time.time * patrolSpeed);

            if (wobble > 0f)
            {
                wobble = Mathf.MoveTowards(wobble, 0f, Time.deltaTime * 2f);
                transform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 40f) * 8f * wobble, 0);
            }
        }
    }
}
