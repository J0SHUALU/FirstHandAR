using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using FirstHand.Assets;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Entities
{
    /// <summary>
    /// Spawns, resets and removes everything in the AR scene.
    ///  - Prefabs come from Addressables through AssetService and are released when no longer needed.
    ///  - Hazards, decoys and tap markers come from pools so retries do not allocate new objects.
    ///  - The casualty and phone are single instances that are reset, not respawned, between attempts.
    /// </summary>
    public class EntityManager : Singleton<EntityManager>
    {
        [SerializeField] private string phoneKey = "PhoneProp";
        [SerializeField] private string tapMarkerKey = "TapMarker";
        [SerializeField] private int tapMarkerPrewarm = 12;

        [Tooltip("Local spots around the casualty where props can appear (metres, scene space)")]
        [SerializeField] private Vector3[] propSlots =
        {
            new Vector3(-0.85f, 0, 0.6f), new Vector3(0.9f, 0, 0.55f), new Vector3(-1.05f, 0, -0.55f),
            new Vector3(0.95f, 0, -0.55f), new Vector3(0f, 0, 0.8f), new Vector3(0.05f, 0, -0.85f)
        };
        [SerializeField] private Vector3 phoneSlot = new Vector3(0.35f, 0, 0.3f);

        private readonly AssetService assets = new AssetService();
        private readonly Dictionary<string, ComponentPool<Hazard>> hazardPools = new Dictionary<string, ComponentPool<Hazard>>();
        private readonly List<(string key, Hazard hazard)> spawned = new List<(string, Hazard)>();
        private ComponentPool<TapMarker> markerPool;
        private Transform parking;
        private Transform sceneRoot;
        private string casualtyKey;

        public Casualty Casualty { get; private set; }
        public PhoneProp Phone { get; private set; }
        public IReadOnlyCollection<string> LoadedAssetKeys => assets.LoadedKeys;

        protected override void Awake()
        {
            base.Awake();
            parking = new GameObject("[Pool parking]").transform;
            parking.SetParent(transform, false);
        }

        /// <summary>Loads the fixed scene pieces once, when the trainee first places the scene.</summary>
        public async Task LoadSceneAsync(ScenarioDefinition scenario, Transform root)
        {
            sceneRoot = root;
            casualtyKey = scenario.casualtyKey;

            var casualtyPrefab = await assets.LoadAsync<GameObject>(casualtyKey);
            var phonePrefab = await assets.LoadAsync<GameObject>(phoneKey);
            var markerPrefab = await assets.LoadAsync<GameObject>(tapMarkerKey);

            Casualty = Instantiate(casualtyPrefab, root, false).GetComponent<Casualty>();
            Phone = Instantiate(phonePrefab, root, false).GetComponent<PhoneProp>();
            Phone.transform.localPosition = phoneSlot;

            markerPool = new ComponentPool<TapMarker>(
                () => Instantiate(markerPrefab, parking).GetComponent<TapMarker>(), parking, tapMarkerPrewarm, 40);
        }

        /// <summary>
        /// Sets up one attempt: resets the casualty and phone, returns last attempt's props to their pools,
        /// releases prefabs that this attempt does not use and spawns the chosen hazards and decoys.
        /// </summary>
        public async Task PrepareAttemptAsync(bool breathing, IList<HazardDefinition> hazards, IList<HazardDefinition> decoys, System.Random rng)
        {
            ReturnProps();
            Casualty.Configure(breathing);
            Phone.ResetEntity();

            var wanted = new HashSet<string>(hazards.Concat(decoys).Select(h => h.addressKey));
            foreach (var key in hazardPools.Keys.Where(k => !wanted.Contains(k)).ToList()) UnloadPool(key);

            var slots = propSlots.OrderBy(_ => rng.Next()).ToList();
            int slot = 0;
            foreach (var def in hazards.Concat(decoys))
            {
                var pool = await GetPoolAsync(def.addressKey);
                var h = pool.Get(sceneRoot, slots[slot++ % slots.Count], Quaternion.Euler(0, rng.Next(0, 360), 0));
                h.Assign(def.hazardId);
                spawned.Add((def.addressKey, h));
            }
        }

        public void SpawnTapMarker(Vector3 worldPos, bool good)
        {
            if (markerPool == null || sceneRoot == null) return;
            var m = markerPool.Get(sceneRoot, sceneRoot.InverseTransformPoint(worldPos), Quaternion.identity);
            m.Show(good, markerPool.Return);
        }

        /// <summary>Back to the main menu: destroy instances and release every Addressables handle.</summary>
        public void UnloadScene()
        {
            ReturnProps();
            foreach (var key in hazardPools.Keys.ToList()) UnloadPool(key);
            markerPool?.Dispose();
            markerPool = null;
            if (Casualty != null) Destroy(Casualty.gameObject);
            if (Phone != null) Destroy(Phone.gameObject);
            Casualty = null; Phone = null;
            assets.ReleaseAll();
        }

        private void ReturnProps()
        {
            foreach (var (key, hazard) in spawned)
                if (hazardPools.TryGetValue(key, out var pool)) pool.Return(hazard);
            spawned.Clear();
        }

        private async Task<ComponentPool<Hazard>> GetPoolAsync(string key)
        {
            if (hazardPools.TryGetValue(key, out var pool)) return pool;
            var prefab = await assets.LoadAsync<GameObject>(key);
            pool = new ComponentPool<Hazard>(() => Instantiate(prefab, parking).GetComponent<Hazard>(), parking, 0, 2);
            hazardPools[key] = pool;
            return pool;
        }

        private void UnloadPool(string key)
        {
            hazardPools[key].Dispose();     // instances first,
            hazardPools.Remove(key);
            assets.Release(key);            // then the prefab handle
        }

        protected override void OnDestroy()
        {
            assets.ReleaseAll();
            base.OnDestroy();
        }
    }
}
