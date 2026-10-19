using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstHand.Core
{
    /// <summary>Anything that is reused by a pool resets itself on spawn and cleans up on return.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Object Pool for the objects that are created and thrown away again and again during an attempt:
    /// hazard props on every retry, compression ripple markers (30 to 60 per attempt), and tap markers.
    /// Instances are prewarmed once, deactivated on return and reset through IPoolable.
    /// </summary>
    public class ComponentPool<T> where T : Component, IPoolable
    {
        private readonly Stack<T> inactive = new Stack<T>();
        private readonly HashSet<T> active = new HashSet<T>();
        private readonly Func<T> create;
        private readonly Transform parking;
        private readonly int maxSize;

        public int ActiveCount => active.Count;
        public int InactiveCount => inactive.Count;
        public int CreatedCount { get; private set; }

        public ComponentPool(Func<T> create, Transform parking, int prewarm = 0, int maxSize = 64)
        {
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.parking = parking;
            this.maxSize = maxSize;
            for (int i = 0; i < prewarm; i++) Return(CreateNew());
        }

        public T Get(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            T item = inactive.Count > 0 ? inactive.Pop() : CreateNew();
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPos;
            item.transform.localRotation = localRot;
            item.gameObject.SetActive(true);
            active.Add(item);
            item.OnSpawned();
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            active.Remove(item);
            item.OnDespawned();
            if (inactive.Count >= maxSize)
            {
                Kill(item.gameObject);
                return;
            }
            item.gameObject.SetActive(false);
            item.transform.SetParent(parking, false);
            inactive.Push(item);
        }

        public void ReturnAll()
        {
            foreach (var item in new List<T>(active)) Return(item);
        }

        /// <summary>Destroys every instance; called before the source prefab is released from Addressables.</summary>
        public void Dispose()
        {
            ReturnAll();
            while (inactive.Count > 0)
            {
                var item = inactive.Pop();
                if (item != null) Kill(item.gameObject);
            }
        }

        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go); // edit-mode tests and editor tooling
        }

        private T CreateNew()
        {
            CreatedCount++;
            var item = create();
            item.gameObject.SetActive(false);
            return item;
        }
    }
}
