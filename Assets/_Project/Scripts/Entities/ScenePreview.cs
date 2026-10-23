using UnityEngine;

namespace FirstHand.Entities
{
    public class ScenePreview : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying) gameObject.SetActive(false);
        }
    }
}
