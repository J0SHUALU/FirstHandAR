using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Entities
{
    /// <summary>
    /// Glowing grab point on a limb. The trainee drags it toward its drop target; on release it reports
    /// whether it got there and springs back to its rest position (the Casualty then animates the real limb).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LimbHandle : MonoBehaviour, IDraggable
    {
        [SerializeField] private string limbId;
        [SerializeField] private Transform dropTarget;
        [SerializeField] private float acceptRadius = 0.15f;
        [SerializeField] private Renderer visual;
        [SerializeField] private Color idleColor = new Color(1f, 0.75f, 0.1f);
        [SerializeField] private Color doneColor = new Color(0.2f, 0.9f, 0.4f);

        private Vector3 restLocal;
        private bool dragging;
        private bool done;
        private MaterialPropertyBlock block;

        public string InteractId => "limb_" + limbId;

        private void Awake() => restLocal = transform.localPosition;

        public void ResetHandle()
        {
            dragging = false;
            transform.localPosition = restLocal;
            if (dropTarget != null) dropTarget.gameObject.SetActive(false);
        }

        public void SetDone(bool isDone)
        {
            done = isDone;
            SetColor(done ? doneColor : idleColor);
            GetComponent<Collider>().enabled = !done;
        }

        public void OnTapped(Vector3 worldPoint) { }

        public void OnDragStart()
        {
            if (done) return;
            dragging = true;
            if (dropTarget != null) dropTarget.gameObject.SetActive(true);
        }

        public void OnDrag(Vector3 worldPoint)
        {
            if (!dragging) return;
            var p = worldPoint;
            p.y = transform.position.y; // keep it at limb height
            transform.position = p;
        }

        public void OnDragEnd()
        {
            if (!dragging) return;
            bool reached = dropTarget != null && Vector3.Distance(transform.position, dropTarget.position) <= acceptRadius;
            ResetHandle();
            EventBus.Publish(new DragEndedEvent { LimbId = limbId, ReachedTarget = reached });
        }

        private void SetColor(Color c)
        {
            if (visual == null) return;
            block ??= new MaterialPropertyBlock();
            visual.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            visual.SetPropertyBlock(block);
        }
    }
}
