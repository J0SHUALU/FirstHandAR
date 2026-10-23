using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using FirstHand.Core;
using FirstHand.Entities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Turns touches into tap and drag events. Taps fire on touch-down (not release) so compression
    /// timing is measured where the finger actually lands (NFR-02). Touches on UI are ignored here.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private float maxRayDistance = 6f;

        private IDraggable dragging;
        private int dragFinger = -1;
        private Plane dragPlane;

        public bool Enabled { get; set; } = true;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (!Enabled) return;
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began: OnBegan(touch); break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (touch.finger.index == dragFinger) ContinueDrag(touch.screenPosition);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.finger.index == dragFinger) EndDrag();
                        break;
                }
            }
        }

        private void OnBegan(Touch touch)
        {
            var pos = touch.screenPosition;
            if (IsOverUI(pos)) return;

            EventBus.Publish(new ScreenTapEvent { ScreenPosition = pos });

            var ray = arCamera.ScreenPointToRay(pos);
            if (FindTarget(ray, out var hit) is IInteractable target)
            {
                if (target is IDraggable d && dragging == null)
                {
                    dragging = d;
                    dragFinger = touch.finger.index;
                    dragPlane = new Plane(Vector3.up, d.DragPlanePoint); // drag along the floor, where the circle is
                    d.OnDragStart();
                    return;
                }

                target.OnTapped(hit.point); // polymorphic: hazard, body part or phone each react their own way
                EventBus.Publish(new TapInputEvent { TargetId = target.InteractId, WorldPoint = hit.point, HitInteractable = true });
            }
            else
            {
                EventBus.Publish(new TapInputEvent { TargetId = null, WorldPoint = Vector3.zero, HitInteractable = false });
            }
        }

        private readonly RaycastHit[] rayHits = new RaycastHit[16];

        /// <summary>
        /// Nearest interactable under the finger, except that a draggable handle wins even if the body is in
        /// front of it, so recovery position handles can always be grabbed.
        /// </summary>
        private IInteractable FindTarget(Ray ray, out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(ray, rayHits, maxRayDistance, interactableLayers, QueryTriggerInteraction.Collide);
            IInteractable nearest = null, drag = null;
            float nearestD = float.MaxValue, dragD = float.MaxValue;
            RaycastHit nearestHit = default, dragHit = default;
            for (int i = 0; i < n; i++)
            {
                var it = rayHits[i].collider.GetComponentInParent<IInteractable>();
                if (it == null) continue;
                float d = rayHits[i].distance;
                if (it is IDraggable && d < dragD) { drag = it; dragD = d; dragHit = rayHits[i]; }
                if (d < nearestD) { nearest = it; nearestD = d; nearestHit = rayHits[i]; }
            }
            if (drag != null) { best = dragHit; return drag; }
            best = nearestHit;
            return nearest;
        }

        private static readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>();

        private static bool IsOverUI(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = pos };
            uiHits.Clear();
            EventSystem.current.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }

        private void ContinueDrag(Vector2 screenPos)
        {
            var ray = arCamera.ScreenPointToRay(screenPos);
            if (dragPlane.Raycast(ray, out float enter)) dragging.OnDrag(ray.GetPoint(enter));
        }

        private void EndDrag()
        {
            dragging?.OnDragEnd();
            dragging = null;
            dragFinger = -1;
        }
    }
}
