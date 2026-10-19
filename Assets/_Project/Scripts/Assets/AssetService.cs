using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FirstHand.Assets
{
    /// <summary>
    /// Single entry point to Addressables. Callers ask for an asset by key and later release it by key.
    /// Handles are reference counted so two systems can share the same prefab or clip and it is only
    /// unloaded when the last user releases it. Nothing outside this class touches AsyncOperationHandle.
    /// </summary>
    public class AssetService
    {
        private class Entry
        {
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        private readonly Dictionary<string, Entry> loaded = new Dictionary<string, Entry>();

        public IReadOnlyCollection<string> LoadedKeys => loaded.Keys;

        public async Task<T> LoadAsync<T>(string key) where T : Object
        {
            if (loaded.TryGetValue(key, out var existing))
            {
                existing.RefCount++;
                if (!existing.Handle.IsDone) await existing.Handle.Task;
                return existing.Handle.Result as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            var entry = new Entry { Handle = handle, RefCount = 1 };
            loaded[key] = entry;
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AssetService] Failed to load '{key}'");
                loaded.Remove(key);
                Addressables.Release(handle);
                return null;
            }
            return handle.Result;
        }

        /// <summary>Loads every asset carrying a label (for example a whole narration language).</summary>
        public async Task<IList<T>> LoadLabelAsync<T>(string label) where T : Object
        {
            string key = "label:" + label;
            if (loaded.TryGetValue(key, out var existing))
            {
                existing.RefCount++;
                if (!existing.Handle.IsDone) await existing.Handle.Task;
                return existing.Handle.Result as IList<T>;
            }

            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            loaded[key] = new Entry { Handle = handle, RefCount = 1 };
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning($"[AssetService] Failed to load label '{label}'");
                loaded.Remove(key);
                Addressables.Release(handle);
                return new List<T>();
            }
            return handle.Result;
        }

        public void Release(string key)
        {
            if (!loaded.TryGetValue(key, out var entry)) return;
            entry.RefCount--;
            if (entry.RefCount > 0) return;
            Addressables.Release(entry.Handle);
            loaded.Remove(key);
        }

        public void ReleaseLabel(string label) => Release("label:" + label);

        public void ReleaseAll()
        {
            foreach (var entry in loaded.Values) Addressables.Release(entry.Handle);
            loaded.Clear();
        }
    }
}
