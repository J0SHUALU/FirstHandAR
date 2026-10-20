using UnityEngine;

namespace FirstHand.Entities
{
    /// <summary>
    /// Collider on part of the casualty (shoulders, chest, body). Reports "casualty_&lt;part&gt;" so steps can
    /// tell a shoulder tap from a chest tap. A short pulse shows the tap registered.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BodyPart : MonoBehaviour, IInteractable
    {
        [SerializeField] private string partId = "body";
        [SerializeField] private Renderer highlight;
        private float pulse;
        private MaterialPropertyBlock block;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public string InteractId => "casualty_" + partId;

        public void OnTapped(Vector3 worldPoint) => pulse = 1f;

        private void Update()
        {
            if (highlight == null || pulse <= 0f) return;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 3f);
            block ??= new MaterialPropertyBlock();
            highlight.GetPropertyBlock(block);
            block.SetColor(EmissionId, new Color(0.2f, 0.8f, 1f) * pulse);
            highlight.SetPropertyBlock(block);
        }
    }
}
