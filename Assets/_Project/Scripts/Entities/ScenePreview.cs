using UnityEngine;

namespace FirstHand.Entities
{
    /// <summary>
    /// Editor-only layout preview so the Scene view shows the casualty and props. Tagged EditorOnly (stripped
    /// from builds) and switched off on play, because at runtime everything is spawned through Addressables.
    /// </summary>
    public class ScenePreview : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying) gameObject.SetActive(false);
        }
    }
}
