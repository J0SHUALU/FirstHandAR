using System;
using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Entities
{
    /// <summary>
    /// Short ripple shown where a compression landed (green on target, red off target). Up to 60 appear per
    /// attempt, so they come from a pool and hand themselves back when the ripple finishes.
    /// </summary>
    public class TapMarker : MonoBehaviour, IPoolable
    {
        [SerializeField] private Renderer ring;
        [SerializeField] private float lifetime = 0.45f;
        [SerializeField] private float maxScale = 0.12f;

        private float age;
        private Action<TapMarker> returnToPool;
        private MaterialPropertyBlock block;
        private Color color;

        public void Show(bool good, Action<TapMarker> onDone)
        {
            returnToPool = onDone;
            color = good ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.95f, 0.3f, 0.25f);
        }

        public void OnSpawned() { age = 0; transform.localScale = Vector3.zero; }
        public void OnDespawned() { returnToPool = null; }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            transform.localScale = Vector3.one * maxScale * t;
            if (ring != null)
            {
                block ??= new MaterialPropertyBlock();
                var c = color; c.a = 1f - t;
                block.SetColor("_BaseColor", c);
                ring.SetPropertyBlock(block);
            }
            if (t >= 1f) returnToPool?.Invoke(this);
        }
    }
}
