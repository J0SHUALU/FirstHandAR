using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using FirstHand.AR;
using FirstHand.Data;
using FirstHand.Entities;
using FirstHand.Presentation;
using FirstHand.Simulation;
using static FirstHand.EditorTools.BuilderUtil;

namespace FirstHand.EditorTools
{
    /// <summary>Builds the single Bootstrap scene: AR Session, XR Origin with managers, the app managers and UI canvas.</summary>
    public static class SceneBuilder
    {
        public const string ScenePath = Scenes + "/Bootstrap.unity";

        public static void Build(ScenarioDefinition scenario, UITheme theme)
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- AR: use AR Foundation's own menu items so the camera rig is exactly what the package expects
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");

            var session = Object.FindAnyObjectByType<ARSession>();
            var origin = Object.FindAnyObjectByType<XROrigin>();
            if (session == null || origin == null)
            {
                Debug.LogError("[SceneBuilder] AR Foundation menu items did not create the AR Session / XR Origin. Is AR Foundation installed?");
                return;
            }

            var planes = origin.GetComponent<ARPlaneManager>() ?? origin.gameObject.AddComponent<ARPlaneManager>();
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planes.planePrefab = BuildPlanePrefab();
            var raycasts = origin.GetComponent<ARRaycastManager>() ?? origin.gameObject.AddComponent<ARRaycastManager>();
            var anchors = origin.GetComponent<ARAnchorManager>() ?? origin.gameObject.AddComponent<ARAnchorManager>();

            var cam = origin.Camera;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f; // small scene: tighter depth range helps precision and culling

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            // --- App managers
            var managers = new GameObject("[Managers]");
            var ar = managers.AddComponent<ARManager>();
            Set(ar, "session", session);
            Set(ar, "planeManager", planes);
            Set(ar, "raycastManager", raycasts);
            Set(ar, "anchorManager", anchors);

            var sim = managers.AddComponent<SimulationManager>();
            Set(sim, "scenario", AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ContentFactory.ScenarioPath) ?? scenario);
            Set(sim, "arCamera", cam);

            managers.AddComponent<EntityManager>();
            managers.AddComponent<DataManager>();
            managers.AddComponent<LocalizationService>();

            var input = managers.AddComponent<InputManager>();
            Set(input, "arCamera", cam);

            var audio = managers.AddComponent<AudioManager>();
            Set(audio, "sfxSource", Source(managers.transform, "SFX"));
            Set(audio, "voiceSource", Source(managers.transform, "Voice"));
            Set(audio, "metronomeSource", Source(managers.transform, "Metronome"));

            // --- UI
            var canvasGo = new GameObject("UI Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var ui = managers.AddComponent<UIManager>();
            Set(ui, "theme", AssetDatabase.LoadAssetAtPath<UITheme>(ContentFactory.ThemePath) ?? theme);
            Set(ui, "canvas", canvas);

            BuildPreview();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// Editor-only layout preview at the origin: what the trainee will see once the scene is placed
        /// (casualty, phone, two hazards, two decoys, a 2 x 1.5 m floor patch). Not included in builds.
        /// </summary>
        public static void BuildPreview()
        {
            var old = GameObject.Find("Scene Preview (editor only)");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("Scene Preview (editor only)");
            root.tag = "EditorOnly";
            root.AddComponent<ScenePreview>();
            // Same as a real placement: 1.2 m in front of the AR camera, near side (+Z) facing the trainee.
            root.transform.SetPositionAndRotation(new Vector3(0, 0, 1.2f), Quaternion.Euler(0, 180, 0));

            Prim(PrimitiveType.Cube, root.transform, "Floor 2 x 1.5 m", new Vector3(0, -0.005f, 0.15f), new Vector3(2.2f, 0.01f, 1.7f),
                             Mat("PreviewFloor", new Color(0.23f, 0.61f, 0.86f, 0.18f), 0.1f, true, true));

            void Put(string path, Vector3 pos, float yaw = 0)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) return;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            }
            Put($"{Prefabs}/Casualty.prefab", Vector3.zero);
            Put($"{Prefabs}/PhoneProp.prefab", new Vector3(0.35f, 0, 0.3f), 20);
            Put($"{HazardPrefabs}/Hazard_WetFloor.prefab", new Vector3(-0.75f, 0, 0.55f));
            Put($"{HazardPrefabs}/Hazard_GasCylinder.prefab", new Vector3(0.8f, 0, -0.5f));
            Put($"{HazardPrefabs}/Decoy_Bag.prefab", new Vector3(0.8f, 0, 0.5f), 30);
            Put($"{HazardPrefabs}/Decoy_Plant.prefab", new Vector3(-1.05f, 0, -0.55f));

            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.LookAt(new Vector3(0, 0.15f, 1.1f), Quaternion.Euler(38, 0, 0), 1.9f);
                sv.Repaint();
            }
        }

        private static AudioSource Source(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        /// <summary>Translucent plane overlay in the app's info blue, with a crisp edge line.</summary>
        private static GameObject BuildPlanePrefab()
        {
            string path = $"{Prefabs}/ARPlane.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("ARPlane");
            go.AddComponent<ARPlane>();
            go.AddComponent<ARPlaneMeshVisualizer>();
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat("PlaneFill", new Color(0.23f, 0.61f, 0.86f, 0.25f), 0.1f, true, true);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<MeshCollider>();
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.widthMultiplier = 0.006f;
            line.loop = true;
            line.sharedMaterial = Mat("PlaneEdge", new Color(0.23f, 0.61f, 0.86f, 0.9f), 0.1f, true, true);
            return SavePrefab(go, path);
        }
    }
}
