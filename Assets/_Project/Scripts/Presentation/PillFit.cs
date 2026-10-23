using UnityEngine;
using UnityEngine.UI;

namespace FirstHand.Presentation
{
    /// <summary>Keeps a sliced pill sprite fully rounded at any height by scaling its border to half the rect height.</summary>
    [RequireComponent(typeof(Image))]
    public class PillFit : MonoBehaviour
    {
        private Image image;

        private void Awake() => image = GetComponent<Image>();
        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (image == null) image = GetComponent<Image>();
            if (image == null || image.sprite == null) return;
            var rt = (RectTransform)transform;
            float h = Mathf.Min(rt.rect.height, rt.rect.width);
            float border = image.sprite.border.y;
            if (h <= 0.01f || border <= 0) return;
            float scale = image.sprite.pixelsPerUnit / 100f;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, border / scale / (h * 0.5f));
        }
    }
}
