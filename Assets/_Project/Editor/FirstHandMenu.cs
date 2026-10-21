using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FirstHand.EditorTools
{
    /// <summary>One-click project setup. Safe to run again: every step overwrites its own output.</summary>
    public static class FirstHandMenu
    {
        [MenuItem("FirstHand/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            BuildContent();
            BuildScene();
            ConfigurePlayer();
            Debug.Log("[FirstHand] Build Everything finished.");
        }

        [MenuItem("FirstHand/1. Content (prefabs, data, audio, Addressables)", priority = 20)]
        public static void BuildContent()
        {
            BuilderUtil.EnsureFolders();
            PrefabFactory.BuildAll();
            ContentFactory.BuildScenario();
            ContentFactory.BuildTextTables();
            ContentFactory.BuildTheme();
            AudioFactory.BuildSfx();
            AddressablesSetup.Configure();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("FirstHand/2. Bootstrap scene", priority = 21)]
        public static void BuildScene()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<FirstHand.Data.ScenarioDefinition>(ContentFactory.ScenarioPath) ?? ContentFactory.BuildScenario();
            var theme = AssetDatabase.LoadAssetAtPath<FirstHand.Presentation.UITheme>(ContentFactory.ThemePath) ?? ContentFactory.BuildTheme();
            SceneBuilder.Build(scenario, theme);
        }

        [MenuItem("FirstHand/3. Android player settings", priority = 22)]
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "LayersbyJ";
            PlayerSettings.productName = "FirstHand AR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.layersbyj.firsthandar");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;     // NFR-09: Android 8.0+
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;               // NFR-09: portrait
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            EditorUserBuildSettings.buildAppBundle = false;
            ApplyAppIcon();
        }

        private const string IconDir = "Assets/_Project/UI/Icon/";

        /// <summary>App icon designed in Affinity: full icon for legacy/round, layered for Android adaptive icons.</summary>
        [MenuItem("FirstHand/App icon", priority = 41)]
        public static void ApplyAppIcon()
        {
            Texture2D Load(string file)
            {
                string path = IconDir + file;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Default;
                    imp.mipmapEnabled = false;
                    imp.alphaIsTransparency = true;
                    imp.npotScale = TextureImporterNPOTScale.None;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            var full = Load("AppIcon-1024.png");
            var fg = Load("AppIcon-adaptive-foreground.png");
            var bg = Load("AppIcon-adaptive-background.png");
            if (full == null) { Debug.LogWarning("[FirstHand] App icon PNGs missing in " + IconDir); return; }

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);

            var android = NamedBuildTarget.Android;
            foreach (var kind in new[] { UnityEditor.Android.AndroidPlatformIconKind.Legacy, UnityEditor.Android.AndroidPlatformIconKind.Round })
            {
                var icons = PlayerSettings.GetPlatformIcons(android, kind);
                foreach (var i in icons) i.SetTexture(full);
                PlayerSettings.SetPlatformIcons(android, kind, icons);
            }
            var adaptive = PlayerSettings.GetPlatformIcons(android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
            foreach (var i in adaptive) { i.SetTexture(bg, 0); i.SetTexture(fg, 1); }
            PlayerSettings.SetPlatformIcons(android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("FirstHand/Narration placeholders (macOS say)", priority = 40)]
        public static void Narration()
        {
            int n = AudioFactory.BuildNarration();
            AddressablesSetup.Configure();
            Debug.Log($"[FirstHand] Generated {n} narration clips.");
        }
    }
}
