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

        private Vector3 baseScale = Vector3.one;

        private void Awake() { restLocal = transform.localPosition; baseScale = transform.localScale; }

        public void ResetHandle()
        {
            dragging = false;
            transform.localPosition = restLocal;
            transform.localScale = baseScale;
        }

        // The drop target is visible whenever this handle is the one to drag next.
        private void OnEnable() { if (dropTarget != null) dropTarget.gameObject.SetActive(!done); }
        private void OnDisable() { if (dropTarget != null) dropTarget.gameObject.SetActive(false); }

        private void Update()
        {
            if (done || dragging) return;
            transform.localScale = baseScale * (1f + 0.18f * Mathf.Sin(Time.time * 5f)); // pulse so it reads as "grab me"
        }

        public void SetDone(bool isDone)
        {
            done = isDone;
            SetColor(done ? doneColor : idleColor);
            GetComponent<Collider>().enabled = !done;
            if (dropTarget != null) dropTarget.gameObject.SetActive(!done && gameObject.activeInHierarchy);
        }

        public void OnTapped(Vector3 worldPoint) { }

        public void OnDragStart()
        {
            if (done) return;
            dragging = true;
            transform.localScale = baseScale * 1.2f;
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
            // Compare on the floor plane: the ball floats above the limb, the target lies on the floor.
            bool reached = false;
            if (dropTarget != null)
            {
                Vector3 a = transform.position, b = dropTarget.position; a.y = b.y = 0f;
                reached = Vector3.Distance(a, b) <= acceptRadius;
            }
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
