using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Entities
{
    [RequireComponent(typeof(Collider))]
    public class LimbHandle : MonoBehaviour, IDraggable
    {
        [SerializeField] private string limbId;
        [SerializeField] private Transform dropTarget;
        [SerializeField] private LineRenderer guide;
        [Tooltip("Bone the ball sits on, so it stays on the limb while the body moves")]
        [SerializeField] private Transform follow;
        [SerializeField] private float acceptRadius = 0.12f;
        [SerializeField] private Renderer visual;
        [SerializeField] private Color idleColor = new Color(1f, 0.416f, 0.169f);
        [SerializeField] private Color doneColor = new Color(0.18f, 0.64f, 0.42f);

        private Vector3 restLocal;
        private Vector3 baseScale = Vector3.one, targetScale = Vector3.one;
        private bool dragging;
        private bool done;
        private MaterialPropertyBlock block;

        public string InteractId => "limb_" + limbId;
        public Vector3 DragPlanePoint => dropTarget != null ? dropTarget.position : transform.position;

        private bool inited;

        private void EnsureInit()
        {
            if (inited) return;
            inited = true;
            restLocal = transform.localPosition;
            baseScale = transform.localScale;
            if (dropTarget != null) targetScale = dropTarget.localScale;
        }

        private void Awake() => EnsureInit();

        private void OnEnable() { EnsureInit(); ShowGuides(!done); }
        private void OnDisable() => ShowGuides(false);

        private void ShowGuides(bool show)
        {
            if (dropTarget != null) dropTarget.gameObject.SetActive(show);
            if (guide != null) guide.gameObject.SetActive(show);
        }

        private void Update()
        {
            if (done) return;
            if (!dragging && follow != null)
            {
                transform.position = follow.position + Vector3.up * 0.04f;
                if (guide != null)
                {
                    guide.SetPosition(0, guide.transform.InverseTransformPoint(transform.position));
                }
            }
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 5f);
            if (!dragging) transform.localScale = baseScale * pulse;
            if (dropTarget != null) dropTarget.localScale = new Vector3(targetScale.x * pulse, targetScale.y, targetScale.z * pulse);
        }

        public void ResetHandle()
        {
            EnsureInit();
            dragging = false;
            transform.localPosition = restLocal;
            if (follow != null) transform.position = follow.position + Vector3.up * 0.04f;
            transform.localScale = baseScale;
        }

        public void SetDone(bool isDone)
        {
            EnsureInit();
            done = isDone;
            SetColor(done ? doneColor : idleColor);
            GetComponent<Collider>().enabled = !done;
            ShowGuides(!done && gameObject.activeInHierarchy);
        }

        public void OnTapped(Vector3 worldPoint) { }

        public void OnDragStart()
        {
            if (done) return;
            dragging = true;
            transform.localScale = baseScale * 1.3f;
        }

        public void OnDrag(Vector3 floorPoint)
        {
            if (!dragging) return;
            transform.position = floorPoint + Vector3.up * 0.03f;
            if (InCircle()) Complete(true);
        }

        public void OnDragEnd()
        {
            if (!dragging) return;
            Complete(InCircle());
        }

        private bool InCircle()
        {
            if (dropTarget == null) return false;
            Vector3 a = transform.position, b = dropTarget.position;
            a.y = b.y = 0f;
            return Vector3.Distance(a, b) <= acceptRadius;
        }

        private void Complete(bool reached)
        {
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
