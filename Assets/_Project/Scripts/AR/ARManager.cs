using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using FirstHand.Core;

namespace FirstHand.AR
{
    /// <summary>
    /// The only class that talks to AR Foundation: plane detection, raycasts, anchors and tracking state.
    /// Everything else gets plain results (a Pose, a bool) or events, so the simulation can run in tests
    /// without a camera.
    /// </summary>
    public class ARManager : Singleton<ARManager>
    {
        [SerializeField] private ARSession session;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private float scanHelpAfterSeconds = 8f;   // FR-06
        [SerializeField] private float statusInterval = 0.5f;

        private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private ARAnchor anchor;
        private bool scanning;
        private float scanTime, statusTimer;
        private bool lastTracking = true;
        private Vector2 minSize = new Vector2(1.5f, 1.0f);

        public bool IsTracking => ARSession.state == ARSessionState.SessionTracking;
        public Transform AnchorTransform => anchor != null ? anchor.transform : null;

        // ---------- Scanning (FR-05, FR-06) ----------

        public void BeginScan(Vector2 requiredSize)
        {
            minSize = requiredSize;
            scanning = true;
            scanTime = 0;
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            SetPlaneDetection(true);
        }

        public void EndScan()
        {
            scanning = false;
            // Mobile optimisation: once the scene is anchored, stop detecting new planes and hide the
            // overlays. This saves CPU/GPU and battery for the rest of the attempt.
            SetPlaneDetection(false);
        }

        private void Update()
        {
            bool tracking = IsTracking;
            if (tracking != lastTracking)
            {
                lastTracking = tracking;
                EventBus.Publish(new TrackingChangedEvent { IsTracking = tracking });
            }

            if (!scanning) return;
            scanTime += Time.deltaTime;
            statusTimer -= Time.deltaTime;
            if (statusTimer > 0) return;
            statusTimer = statusInterval;

            int count = 0; bool bigEnough = false;
            foreach (var p in planeManager.trackables)
            {
                if (p.alignment != PlaneAlignment.HorizontalUp) continue;
                count++;
                if (IsBigEnough(p)) bigEnough = true;
            }
            EventBus.Publish(new PlaneStatusEvent
            {
                PlaneCount = count,
                LargestBigEnough = bigEnough,
                ScanTimedOut = count == 0 && scanTime >= scanHelpAfterSeconds
            });
        }

        // ---------- Raycast + placement (FR-07) ----------

        public enum PlaceResult { NoPlane, TooSmall, Placed }

        /// <summary>Raycasts the tap onto a horizontal plane and anchors the scene there, replacing any old anchor.</summary>
        public PlaceResult TryPlace(Vector2 screenPos, Transform cameraTransform, out Pose pose)
        {
            pose = default;
            if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon)) return PlaceResult.NoPlane;

            var hit = hits[0];
            var plane = planeManager.GetPlane(hit.trackableId);
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp) return PlaceResult.NoPlane;
            if (!IsBigEnough(plane)) return PlaceResult.TooSmall;

            // Face the scene toward the trainee: yaw only, so the casualty lies flat on the floor.
            var toCam = cameraTransform.position - hit.pose.position;
            toCam.y = 0;
            var rot = toCam.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toCam) : hit.pose.rotation;
            pose = new Pose(hit.pose.position, rot);

            ClearAnchor();
            anchor = anchorManager.AttachAnchor(plane, pose);
            if (anchor == null)
            {
                // Fallback for devices that cannot attach to planes: a free-standing anchor.
                var go = new GameObject("SceneAnchor");
                go.transform.SetPositionAndRotation(pose.position, pose.rotation);
                anchor = go.AddComponent<ARAnchor>();
            }
            EventBus.Publish(new ScenePlacedEvent { Pose = pose });
            return PlaceResult.Placed;
        }

        public void ClearAnchor()
        {
            if (anchor != null) Destroy(anchor.gameObject);
            anchor = null;
        }

        /// <summary>Main menu: clear everything and start a fresh AR session.</summary>
        public void ResetSession()
        {
            ClearAnchor();
            scanning = false;
            session.Reset();
        }

        private bool IsBigEnough(ARPlane p)
        {
            var s = p.size;
            float a = Mathf.Max(s.x, s.y), b = Mathf.Min(s.x, s.y);
            return a >= minSize.x && b >= minSize.y;
        }

        private void SetPlaneDetection(bool on)
        {
            planeManager.enabled = on;
            foreach (var p in planeManager.trackables) p.gameObject.SetActive(on);
        }
    }
}
