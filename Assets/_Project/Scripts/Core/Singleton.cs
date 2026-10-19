using UnityEngine;

namespace FirstHand.Core
{
    /// <summary>
    /// Limited Singleton: only scene managers derive from this and they all live in the Bootstrap scene.
    /// Steps and scoring never touch Instance; they receive dependencies through StepContext.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
