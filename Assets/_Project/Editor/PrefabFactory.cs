using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FirstHand.Entities;
using static FirstHand.EditorTools.BuilderUtil;
using Object = UnityEngine.Object;

namespace FirstHand.EditorTools
{
    /// <summary>
    /// Builds the scene prefabs from the free models in _Project/Models (credits in Docs/CREDITS.md):
    /// the casualty, six hazards with a "made safe" state, four harmless decoys, the phone and the tap marker.
    /// Layout: the casualty lies along X with the head at -X, face up; the trainee kneels on the +Z side.
    /// </summary>
    public static class PrefabFactory
    {
        public const string Models = Root + "/Models";
        private static Material handleMat, targetMat, guideMat, ringMat, water, safeGreen;

        private static void LoadMaterials()
        {
            handleMat = OverlayMat("Handle", new Color32(0xFF, 0x6A, 0x2B, 0xFF));                  // sunset orange
            targetMat = OverlayMat("DropTarget", new Color(0.18f, 0.64f, 0.42f, 0.6f));             // leaf green, see-through
            guideMat = OverlayMat("DragGuide", new Color(1f, 1f, 1f, 0.9f));
            ringMat = Mat("FoundRing", new Color(0.17f, 0.71f, 0.45f, 0.85f), 0.1f, true, true);
            water = Mat("Water", new Color(0.35f, 0.6f, 0.9f, 0.55f), 0.98f, true);
            safeGreen = Mat("SafeGreen", new Color32(0x2B, 0xB6, 0x73, 0xFF), 0.5f);
        }

        public static void BuildAll()
        {
            EnsureFolders();
            LoadMaterials();
            BuildCasualty();
            BuildPhone();
            BuildTapMarker();
            BuildHazards();
            BuildDecoys();
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        // Model helper
        // =====================================================================

        /// <summary>
        /// Instantiates a model, strips animation components, scales it so its largest (or vertical) size equals
        /// <paramref name="size"/>, and sits it on y = 0 centred on <paramref name="localPos"/>.
        /// </summary>
        public static GameObject Model(Transform parent, string file, float size, Vector3 localPos, float yaw = 0f, bool byHeight = false)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{file}.glb");
            if (src == null) throw new Exception($"Model {file}.glb is missing or not imported (is glTFast installed?)");
            var go = (GameObject)Object.Instantiate(src);
            go.name = file;
            foreach (var a in go.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go);
            float current = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (current > 1e-5f) go.transform.localScale *= size / current;
            b = Bounds(go);
            var wanted = parent.TransformPoint(localPos);
            go.transform.position += new Vector3(wanted.x - b.center.x, wanted.y - b.min.y, wanted.z - b.center.z);
            return go;
        }

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs)
            {
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        // =====================================================================
        // Casualty (Quaternius "Worker", CC0)
        // =====================================================================

        private static Transform Bone(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            throw new Exception($"Bone {name} not found in casualty model");
        }

        /// <summary>Rotates <paramref name="bone"/> so the direction to <paramref name="tip"/> points along dir (casualty frame).</summary>
        private static void Aim(Transform frame, Transform bone, Transform tip, Vector3 dir)
        {
            var current = tip.position - bone.position;
            var wanted = frame.TransformDirection(dir.normalized);
            bone.rotation = Quaternion.FromToRotation(current, wanted) * bone.rotation;
        }

        private static void BuildCasualty()
        {
            var root = new GameObject("Casualty");
            var casualty = root.AddComponent<Casualty>();

            // Model standing, facing +Z, 1.75 m tall; then laid down face up with the head toward -X.
            var model = Model(root.transform, "fh_casualty", 1.75f, Vector3.zero, 0, true);
            var rollPivot = Pivot(root.transform, "RollPivot", new Vector3(0, 0.12f, 0.2f)); // roll about the near-side line, slightly raised so he lands on the floor
            model.transform.SetParent(rollPivot, true);
            model.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.left) * model.transform.rotation;
            // Centre on the origin and rest on the floor.
            var b = Bounds(model);
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);

            var f = root.transform;
            // Quaternius rigs keep the feet as IK targets under Root; parent them to the shins so they follow the legs.
            var shinL = Bone(model, "LowerLeg.L"); var shinR = Bone(model, "LowerLeg.R");
            Bone(model, "Foot.L").SetParent(shinL, true);
            Bone(model, "Foot.R").SetParent(shinR, true);

            // Laid down with LookRotation(up, left): the model's left side faces +Z (the trainee) = near side.
            var upL = Bone(model, "UpperArm.L"); var loL = Bone(model, "LowerArm.L"); var wrL = Bone(model, "Wrist.L");
            var upR = Bone(model, "UpperArm.R"); var loR = Bone(model, "LowerArm.R"); var wrR = Bone(model, "Wrist.R");
            var thL = Bone(model, "UpperLeg.L"); var endL = Bone(model, "LowerLeg.L_end");
            var thR = Bone(model, "UpperLeg.R"); var endR = Bone(model, "LowerLeg.R_end");
            var head = Bone(model, "Head"); var headEnd = Bone(model, "Head_end");
            var chest = Bone(model, "Chest"); var torso = Bone(model, "Torso");

            // Rest pose: arms at the sides, legs straight.
            Aim(f, upL, loL, new Vector3(1, -0.05f, 0.22f)); Aim(f, loL, wrL, new Vector3(1, -0.05f, 0.08f));
            Aim(f, upR, loR, new Vector3(1, -0.05f, -0.22f)); Aim(f, loR, wrR, new Vector3(1, -0.05f, -0.08f));
            Aim(f, thL, shinL, new Vector3(1, 0, 0.06f)); Aim(f, shinL, endL, new Vector3(1, 0, 0.04f));
            Aim(f, thR, shinR, new Vector3(1, 0, -0.06f)); Aim(f, shinR, endR, new Vector3(1, 0, -0.04f));
            b = Bounds(model);
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);

            var posed = new List<Transform> { upL, loL, upR, loR, thR, shinR, rollPivot, head };
            var rest = new Dictionary<Transform, Quaternion>();
            foreach (var t in posed) rest[t] = t.localRotation;

            // Reference points at rest (world == root space here, root is at the origin).
            Vector3 shoulderL = upL.position, handL = wrL.position, handR = wrR.position, kneeR = shinR.position,
                    hipR = thR.position, headPos = head.position;
            var chestPoint = Vector3.Lerp(chest.position, torso.position, 0.5f);
            float chestTop = Bounds(model).max.y;

            // Recovery stages, applied in order so each target is the cumulative pose.
            var limbs = new List<(string id, Transform pivot, Quaternion target)>();
            void Record(string id, params Transform[] ts) { foreach (var t in ts) limbs.Add((id, t, t.localRotation)); }

            Aim(f, upL, loL, new Vector3(0, 0, 1)); Aim(f, loL, wrL, new Vector3(-1, 0.05f, 0)); Record("near_arm", upL, loL);
            Aim(f, upR, loR, new Vector3(-0.15f, 0.55f, 1)); Aim(f, loR, wrR, new Vector3(-1, 0.15f, 0.25f)); Record("far_hand", upR, loR);
            Aim(f, thR, shinR, new Vector3(0.55f, 0.8f, 0.1f)); Aim(f, shinR, endR, new Vector3(0.6f, -0.8f, 0.05f)); Record("far_knee", thR, shinR);
            rollPivot.localRotation = Quaternion.Euler(82, 0, 0) * rollPivot.localRotation;
            // The near arm stays flat on the floor while the body rolls onto its side.
            Aim(f, upL, loL, new Vector3(-0.25f, -0.55f, 1)); Aim(f, loL, wrL, new Vector3(-1, -0.05f, 0.1f));
            Record("roll", rollPivot, upL, loL);
            Aim(f, head, headEnd, f.InverseTransformDirection(headEnd.position - head.position) + new Vector3(0, 0, -0.45f)); Record("head_tilt", head);

            foreach (var kv in rest) kv.Key.localRotation = kv.Value;

            // Tap targets (fixed colliders, only used before the recovery stage).
            var chestTarget = Pivot(f, "ChestTarget", new Vector3(chestPoint.x, chestTop - 0.02f, chestPoint.z));
            AddPartCollider(f, "Chest", "chest", new Vector3(chestPoint.x, chestTop - 0.06f, 0), new Vector3(0.3f, 0.12f, 0.3f));
            AddPartCollider(f, "Shoulder_L", "shoulder_l", shoulderL + new Vector3(0, 0.08f, 0), new Vector3(0.16f, 0.16f, 0.16f));
            AddPartCollider(f, "Shoulder_R", "shoulder_r", upR.position + new Vector3(0, 0.08f, 0), new Vector3(0.16f, 0.16f, 0.16f));
            AddPartCollider(f, "Head", "head", headPos + new Vector3(0, 0.06f, 0), new Vector3(0.24f, 0.2f, 0.24f));
            Box(root, new Vector3(0, 0.1f, 0), new Vector3(1.75f, 0.2f, 0.5f));

            // Drag handles (root space, stay at their height) and drop targets.
            var hNear = Handle(f, "near_arm", handL + Vector3.up * 0.04f, shoulderL + new Vector3(0.05f, 0, 0.5f), wrL);
            var hFar = Handle(f, "far_hand", handR + Vector3.up * 0.04f, headPos + new Vector3(0.05f, 0, 0.18f), wrR);
            var hKnee = Handle(f, "far_knee", kneeR + Vector3.up * 0.04f, kneeR + new Vector3(-0.1f, 0, 0.42f), shinR);
            var hRoll = Handle(f, "roll", hipR + Vector3.up * 0.04f, hipR + new Vector3(0, 0, 0.6f), thR);
            var hHead = Handle(f, "head_tilt", headPos + Vector3.up * 0.04f, headPos + new Vector3(-0.3f, 0, 0), head);
            var handles = new Dictionary<string, LimbHandle> { ["near_arm"] = hNear, ["far_hand"] = hFar, ["far_knee"] = hKnee, ["roll"] = hRoll, ["head_tilt"] = hHead };

            var so = new SerializedObject(casualty);
            so.FindProperty("entityId").stringValue = "casualty";
            so.FindProperty("chestTarget").objectReferenceValue = chestTarget;
            so.FindProperty("chestMesh").objectReferenceValue = chest;
            so.FindProperty("breathAmplitude").floatValue = 0.008f;
            so.FindProperty("compressionDip").floatValue = 0.02f;
            var arr = so.FindProperty("limbs");
            arr.arraySize = limbs.Count;
            var handled = new HashSet<string>();
            for (int i = 0; i < limbs.Count; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("limbId").stringValue = limbs[i].id;
                e.FindPropertyRelative("pivot").objectReferenceValue = limbs[i].pivot;
                e.FindPropertyRelative("targetLocalRotation").quaternionValue = limbs[i].target;
                e.FindPropertyRelative("handle").objectReferenceValue = handled.Add(limbs[i].id) ? handles[limbs[i].id] : null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, $"{Prefabs}/Casualty.prefab");
        }

        private static void AddPartCollider(Transform root, string name, string partId, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            Box(go, Vector3.zero, size);
            var part = go.AddComponent<FirstHand.Entities.BodyPart>();
            Set(part, "partId", partId);
        }

        private static LimbHandle Handle(Transform root, string limbId, Vector3 pos, Vector3 targetPos, Transform follow)
        {
            // A small orange ball on the limb; the overlay shader keeps it visible through the body.
            var h = Prim(PrimitiveType.Sphere, root, "Handle_" + limbId, pos, Vector3.one * 0.055f, handleMat, true);
            var sc = h.GetComponent<SphereCollider>();
            sc.radius = 1.6f; // 5.5 cm ball, about 18 cm touch area so it is easy to grab
            sc.isTrigger = true;
            targetPos.y = 0.005f; // drop circle lies on the floor
            var target = Prim(PrimitiveType.Cylinder, root, "Target_" + limbId, targetPos, new Vector3(0.18f, 0.002f, 0.18f), targetMat);
            // White guide line on the floor from the limb to the circle, so it is clear where to drag.
            var guideGo = new GameObject("Guide_" + limbId);
            guideGo.transform.SetParent(root, false);
            var guide = guideGo.AddComponent<LineRenderer>();
            guide.useWorldSpace = false;
            guide.positionCount = 2;
            guide.SetPosition(0, pos); // from the ball itself
            guide.SetPosition(1, new Vector3(targetPos.x, 0.006f, targetPos.z));
            guide.widthMultiplier = 0.012f;
            guide.numCapVertices = 4;
            guide.sharedMaterial = guideMat;
            guide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            guideGo.SetActive(false);
            target.SetActive(false);
            var lh = h.AddComponent<LimbHandle>();
            Set(lh, "limbId", limbId);
            Set(lh, "dropTarget", target.transform);
            Set(lh, "visual", h.GetComponent<Renderer>());
            Set(lh, "guide", guide);
            Set(lh, "follow", follow);
            Set(lh, "acceptRadius", 0.12f);
            h.SetActive(false);
            return lh;
        }

        // =====================================================================
        // Phone + tap marker
        // =====================================================================

        private static void BuildPhone()
        {
            var root = new GameObject("PhoneProp");
            var m = Model(root.transform, "fh_smartphone", 0.155f, Vector3.zero);
            // Lay it flat if the model stands upright.
            var b = Bounds(m);
            if (b.size.y > b.size.x && b.size.y > b.size.z)
            {
                m.transform.Rotate(90, 0, 0, Space.World);
                b = Bounds(m);
                m.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            }
            var glow = Prim(PrimitiveType.Cube, root.transform, "Glow", new Vector3(0, Bounds(m).max.y + 0.001f, 0), new Vector3(0.09f, 0.002f, 0.17f),
                            Mat("PhoneGlow", new Color(0.23f, 0.61f, 0.86f, 0.6f), 0.9f, true, true));
            glow.SetActive(false);
            Box(root, new Vector3(0, 0.03f, 0), new Vector3(0.16f, 0.08f, 0.22f));
            var phone = root.AddComponent<PhoneProp>();
            Set(phone, "entityId", "phone");
            Set(phone, "screenGlow", glow);
            SavePrefab(root, $"{Prefabs}/PhoneProp.prefab");
        }

        private static void BuildTapMarker()
        {
            var root = new GameObject("TapMarker");
            var ring = Prim(PrimitiveType.Cylinder, root.transform, "Ring", Vector3.zero, new Vector3(1f, 0.02f, 1f),
                            Mat("TapRing", new Color(1, 1, 1, 0.8f), 0.1f, true, true));
            var marker = root.AddComponent<TapMarker>();
            Set(marker, "ring", ring.GetComponent<Renderer>());
            SavePrefab(root, $"{Prefabs}/TapMarker.prefab");
        }

        // =====================================================================
        // Hazards: a "danger" look and a "made safe" look
        // =====================================================================

        private static void BuildHazards()
        {
            // Wet floor: a puddle; made safe = a wet floor sign is put up next to it.
            Hazard("Hazard_WetFloor", "wet_floor", new Vector3(0.6f, 0.4f, 0.5f), (danger, safe) =>
            {
                Prim(PrimitiveType.Cylinder, danger, "Puddle", new Vector3(0, 0.002f, 0), new Vector3(0.55f, 0.002f, 0.4f), water);
                Prim(PrimitiveType.Cylinder, safe, "Puddle", new Vector3(0, 0.002f, 0), new Vector3(0.55f, 0.002f, 0.4f), water);
                Model(safe, "fh_wetfloorsign", 0.6f, new Vector3(0.05f, 0, -0.05f), 20, true);
            });

            // Loose cable across the walkway; made safe = pulled against the side, out of the path.
            Hazard("Hazard_LooseCable", "loose_cable", new Vector3(0.7f, 0.15f, 0.4f), (danger, safe) =>
            {
                Model(danger, "fh_cable", 0.7f, Vector3.zero, 35);
                Model(safe, "fh_cable", 0.45f, new Vector3(0, 0, 0.18f), 90);
            });

            // Broken glass; made safe = swept into a pile (smaller, gathered).
            Hazard("Hazard_BrokenGlass", "broken_glass", new Vector3(0.55f, 0.15f, 0.45f), (danger, safe) =>
            {
                Model(danger, "fh_smashedglass", 0.5f, Vector3.zero, 0);
                Model(safe, "fh_smashedglass", 0.18f, Vector3.zero, 0);
            });

            // Gas cylinder hissing; made safe = valve closed (green cap), hiss stops.
            Hazard("Hazard_GasCylinder", "gas_cylinder", new Vector3(0.4f, 0.75f, 0.4f), (danger, safe) =>
            {
                Model(danger, "fh_gastank", 0.6f, Vector3.zero, 0, true);
                Model(safe, "fh_gastank", 0.6f, Vector3.zero, 0, true);
                Prim(PrimitiveType.Cylinder, safe, "ClosedCap", new Vector3(0, 0.62f, 0), new Vector3(0.09f, 0.03f, 0.09f), safeGreen);
            }, particles: new ParticleSpec { color = new Color(0.9f, 0.95f, 1f, 0.35f), size = 0.05f, speed = 0.4f, rate = 30, y = 0.62f });

            // Smoking bin; made safe = lid on, smoke stops.
            Hazard("Hazard_Smoke", "smoke", new Vector3(0.45f, 0.9f, 0.45f), (danger, safe) =>
            {
                Model(danger, "fh_trashcan", 0.5f, Vector3.zero, 0, true);
                Model(safe, "fh_trashcan", 0.5f, Vector3.zero, 0, true);
                Prim(PrimitiveType.Cylinder, safe, "Lid", new Vector3(0, 0.51f, 0), new Vector3(0.36f, 0.015f, 0.36f), safeGreen);
            }, particles: new ParticleSpec { color = new Color(0.35f, 0.35f, 0.38f, 0.6f), size = 0.18f, speed = 0.25f, rate = 14, y = 0.5f });

            // Moving trolley rolls back and forth; made safe = stopped with the brake on.
            Hazard("Hazard_MovingTrolley", "moving_trolley", new Vector3(0.6f, 0.9f, 0.45f), (danger, safe) =>
            {
                Model(danger, "fh_cart", 0.9f, Vector3.zero, 90, true);
                Model(safe, "fh_cart", 0.9f, Vector3.zero, 90, true);
                Prim(PrimitiveType.Cube, safe, "Brake", new Vector3(0.28f, 0.03f, 0), new Vector3(0.05f, 0.05f, 0.34f), safeGreen);
            }, patrol: new Vector3(0.18f, 0, 0), patrolSpeed: 1.4f);
        }

        private static void BuildDecoys()
        {
            Decoy("Decoy_Bag", "decoy_bag", "fh_backpack", 0.45f, new Vector3(0.35f, 0.45f, 0.3f));
            Decoy("Decoy_Chair", "decoy_chair", "fh_officechair", 1.0f, new Vector3(0.6f, 1.0f, 0.6f));
            Decoy("Decoy_Bottle", "decoy_bottle", "fh_waterbottle", 0.25f, new Vector3(0.15f, 0.3f, 0.15f));
            Decoy("Decoy_Plant", "decoy_plant", "fh_houseplant", 0.6f, new Vector3(0.35f, 0.6f, 0.35f));
        }

        // ---------- hazard helpers ----------

        private struct ParticleSpec { public Color color; public float size, speed, y; public int rate; }

        private static void Hazard(string assetName, string id, Vector3 colliderSize, Action<Transform, Transform> build,
                                   ParticleSpec? particles = null, Vector3 patrol = default, float patrolSpeed = 0f)
        {
            var root = new GameObject(assetName);
            var danger = Pivot(root.transform, "Danger", Vector3.zero);
            var safe = Pivot(root.transform, "Safe", Vector3.zero);
            build(danger, safe);
            safe.gameObject.SetActive(false);

            float ringSize = Mathf.Max(colliderSize.x, colliderSize.z) + 0.1f;
            var ring = Prim(PrimitiveType.Cylinder, root.transform, "FoundRing", new Vector3(0, 0.003f, 0), new Vector3(ringSize, 0.002f, ringSize), ringMat);
            ring.SetActive(false);
            Box(root, new Vector3(0, colliderSize.y / 2f, 0), colliderSize);

            var hz = root.AddComponent<FirstHand.Entities.Hazard>();
            Set(hz, "entityId", id);
            Set(hz, "isDangerous", true);
            Set(hz, "dangerVisual", danger.gameObject);
            Set(hz, "safeVisual", safe.gameObject);
            Set(hz, "foundRing", ring);
            if (patrolSpeed > 0) { Set(hz, "patrolOffset", patrol); Set(hz, "patrolSpeed", patrolSpeed); }
            if (particles.HasValue) Set(hz, "effect", AddParticles(root.transform, particles.Value, assetName));

            SavePrefab(root, $"{HazardPrefabs}/{assetName}.prefab");
        }

        private static void Decoy(string assetName, string id, string model, float height, Vector3 colliderSize)
        {
            var root = new GameObject(assetName);
            Model(root.transform, model, height, Vector3.zero, 0, true);
            Box(root, new Vector3(0, colliderSize.y / 2f, 0), colliderSize);
            var hz = root.AddComponent<FirstHand.Entities.Hazard>();
            Set(hz, "entityId", id);
            Set(hz, "isDangerous", false);
            SavePrefab(root, $"{HazardPrefabs}/{assetName}.prefab");
        }

        private static ParticleSystem AddParticles(Transform parent, ParticleSpec spec, string name)
        {
            var go = new GameObject("Effect");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, spec.y, 0);
            go.transform.localEulerAngles = new Vector3(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = spec.color;
            main.startSize = spec.size;
            main.startSpeed = spec.speed;
            main.startLifetime = 2.2f;
            main.maxParticles = 60;               // capped for mobile overdraw
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var em = ps.emission; em.rateOverTime = spec.rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12; shape.radius = 0.04f;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.6f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(spec.color, 0), new GradientColorKey(spec.color, 1) },
                         new[] { new GradientAlphaKey(spec.color.a, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            string matPath = $"{Materials}/Particle_{name}.mat";
            var pmat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (pmat == null)
            {
                pmat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                MakeTransparent(pmat);
                pmat.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(pmat, matPath);
            }
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = pmat;
            return ps;
        }
    }
}
