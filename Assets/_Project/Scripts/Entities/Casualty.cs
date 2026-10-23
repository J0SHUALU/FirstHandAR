using System;
using System.Collections.Generic;
using UnityEngine;
using FirstHand.Core;
using FirstHand.Steps;

namespace FirstHand.Entities
{
    public class Casualty : SimEntity, ICasualtyView
    {
        [Serializable]
        public class RecoveryLimb
        {
            public string limbId;
            public Transform pivot;
            [Tooltip("Local rotation of the pivot once this stage is done (baked by the prefab builder for this rig)")]
            public Quaternion targetLocalRotation = Quaternion.identity;
            public LimbHandle handle;
            [NonSerialized] public Quaternion startRotation;
            [NonSerialized] public Quaternion placedRotation;
        }

        [Header("Body")]
        [SerializeField] private Transform chestTarget;
        [SerializeField] private Transform chestMesh;
        [SerializeField] private float breathAmplitude = 0.012f;
        [SerializeField] private float compressionDip = 0.03f;

        [Header("Recovery position (order is set by the scenario asset, not here)")]
        [SerializeField] private List<RecoveryLimb> limbs = new List<RecoveryLimb>();
        [Tooltip("Stage order used to animate; must match the scenario's recovery order")]
        [SerializeField] private List<string> stageOrder = new List<string> { "near_arm", "far_hand", "far_knee", "roll", "head_tilt" };
        [SerializeField] private float poseLerpSpeed = 6f;

        public bool IsBreathing { get; private set; }
        public Vector3 ChestTarget => chestTarget.position;

        private Vector3 chestRestLocal;
        private float dip;
        private readonly HashSet<string> placed = new HashSet<string>();
        private readonly Dictionary<Transform, Quaternion> goals = new Dictionary<Transform, Quaternion>();

        private void Awake()
        {
            chestRestLocal = chestMesh != null ? chestMesh.localPosition : Vector3.zero;
            foreach (var l in limbs)
            {
                if (l.pivot == null) continue;
                l.startRotation = l.pivot.localRotation;
                l.placedRotation = l.targetLocalRotation;
            }
        }

        private void OnEnable() => EventBus.Subscribe<RateUpdatedEvent>(OnCompression);
        private void OnDisable() => EventBus.Unsubscribe<RateUpdatedEvent>(OnCompression);

        public void Configure(bool breathing)
        {
            IsBreathing = breathing;
            ResetEntity();
        }

        public override void ResetEntity()
        {
            placed.Clear();
            dip = 0;
            for (int i = limbs.Count - 1; i >= 0; i--)
            {
                var l = limbs[i];
                if (l.pivot != null) l.pivot.localRotation = l.startRotation;
                if (l.handle != null) { l.handle.ResetHandle(); l.handle.SetDone(false); l.handle.gameObject.SetActive(false); }
            handlesOn = false;
            }
        }

        private void Update()
        {
            if (chestMesh != null)
            {
                float rise = IsBreathing ? Mathf.Sin(Time.time * Mathf.PI * 2f / 4f) * breathAmplitude : 0f;
                dip = Mathf.MoveTowards(dip, 0f, Time.deltaTime * 0.3f);
                Vector3 offset = transform.up * (rise - dip);
                chestMesh.localPosition = chestRestLocal + (chestMesh.parent != null ? chestMesh.parent.InverseTransformVector(offset) : offset);
            }

            goals.Clear();
            foreach (var l in limbs)
            {
                if (l.pivot == null) continue;
                if (placed.Contains(l.limbId)) goals[l.pivot] = l.placedRotation;
                else if (!goals.ContainsKey(l.pivot)) goals[l.pivot] = l.startRotation;
            }
            foreach (var kv in goals)
                kv.Key.localRotation = Quaternion.Slerp(kv.Key.localRotation, kv.Value, Time.deltaTime * poseLerpSpeed);
        }

        public void SetRecoveryStage(int completedStages)
        {
            placed.Clear();
            for (int i = 0; i < completedStages && i < stageOrder.Count; i++) placed.Add(stageOrder[i]);
            for (int i = 0; i < limbs.Count; i++)
                if (limbs[i].handle != null)
                    limbs[i].handle.SetDone(placed.Contains(limbs[i].limbId));
            RefreshHandles();
        }

        private bool handlesOn;

        private void RefreshHandles()
        {
            string nextId = placed.Count < stageOrder.Count ? stageOrder[placed.Count] : null;
            foreach (var l in limbs)
                if (l.handle != null) l.handle.gameObject.SetActive(handlesOn && l.limbId == nextId);
        }

        public void SnapLimbBack(string limbId)
        {
            foreach (var l in limbs)
                if (l.limbId == limbId && l.handle != null) l.handle.ResetHandle();
        }

        public void SetStageOrder(IEnumerable<string> order) => stageOrder = new List<string>(order);

        public void SetLimbHandlesActive(bool active)
        {
            handlesOn = active;
            RefreshHandles();
        }

        private void OnCompression(RateUpdatedEvent e) => dip = compressionDip;
    }
}
