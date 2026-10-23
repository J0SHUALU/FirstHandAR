using UnityEngine;

namespace FirstHand.Presentation
{
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;
        private void Update()
        {
            var sa = Screen.safeArea;
            if (sa == applied || Screen.width == 0) return;
            applied = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.x / Screen.width, sa.y / Screen.height);
            rt.anchorMax = new Vector2((sa.x + sa.width) / Screen.width, (sa.y + sa.height) / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
