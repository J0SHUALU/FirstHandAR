using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstHand.EditorTools
{
    public static class BuilderUtil
    {
        public const string Root = "Assets/_Project";
        public const string Materials = Root + "/Materials";
        public const string Prefabs = Root + "/Prefabs";
        public const string HazardPrefabs = Root + "/Prefabs/Hazards";
        public const string DataDir = Root + "/Data";
        public const string Sfx = Root + "/Audio/SFX";
        public const string VoEn = Root + "/Audio/VO/en";
        public const string VoPcm = Root + "/Audio/VO/pcm";
        public const string Sprites = Root + "/UI/Sprites";
        public const string Scenes = Root + "/Scenes";

        public static void EnsureFolders()
        {
            foreach (var p in new[] { Materials, Prefabs, HazardPrefabs, DataDir, Sfx, VoEn, VoPcm, Sprites, Scenes })
                EnsureFolder(p);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        private static Shader UnlitShader => Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        public static Material Mat(string name, Color color, float smoothness = 0.35f, bool transparent = false, bool unlit = false, Color? emission = null)
        {
            string path = $"{Materials}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(unlit ? UnlitShader : LitShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = unlit ? UnlitShader : LitShader;
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (transparent) MakeTransparent(mat);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material OverlayMat(string name, Color color)
        {
            string path = $"{Materials}/{name}.mat";
            var shader = Shader.Find("FirstHand/Overlay");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.renderQueue = 4000;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale,
                                      Material mat, bool keepCollider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Segment(Transform parent, string name, float length, float thickness, Material mat, float yOffset = 0)
            => Prim(PrimitiveType.Capsule, parent, name, new Vector3(length / 2f, yOffset, 0),
                    new Vector3(thickness, length / 2f, thickness), mat, false, new Vector3(0, 0, 90));

        public static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static BoxCollider Box(GameObject go, Vector3 center, Vector3 size, bool trigger = true)
        {
            var c = go.AddComponent<BoxCollider>();
            c.center = center; c.size = size; c.isTrigger = trigger;
            return c;
        }

        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Builder] {target.GetType().Name}.{field} not found"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                default: Debug.LogError($"[Builder] Unsupported type for {field}"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
