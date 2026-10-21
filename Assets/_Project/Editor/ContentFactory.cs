using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using FirstHand.Data;
using FirstHand.Presentation;
using static FirstHand.EditorTools.BuilderUtil;

namespace FirstHand.EditorTools
{
    /// <summary>Scenario data, text tables and the UI theme (with generated base sprites).</summary>
    public static class ContentFactory
    {
        public const string ScenarioPath = DataDir + "/Scenario_OfficeCollapse.asset";
        public const string ThemePath = DataDir + "/UITheme.asset";

        public static readonly string[] RecoveryOrder = { "near_arm", "far_hand", "far_knee", "roll", "head_tilt" };

        public static ScenarioDefinition BuildScenario()
        {
            EnsureFolders();
            var s = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ScenarioPath);
            if (s == null) { s = ScriptableObject.CreateInstance<ScenarioDefinition>(); AssetDatabase.CreateAsset(s, ScenarioPath); }

            s.scenarioId = "office_collapse_v1";
            s.titleKey = "scenario.title";
            s.casualtyKey = "Casualty";
            s.breathingChance = 0.5f;
            s.hazardsPerAttempt = 2;
            s.decoysPerAttempt = 2;
            s.passMark = 80;
            s.minPlaneSize = new Vector2(1.5f, 1.0f);

            s.hazardPool = new List<HazardDefinition>
            {
                H("wet_floor", "Hazard_WetFloor"), H("loose_cable", "Hazard_LooseCable"), H("broken_glass", "Hazard_BrokenGlass"),
                H("gas_cylinder", "Hazard_GasCylinder"), H("smoke", "Hazard_Smoke"), H("moving_trolley", "Hazard_MovingTrolley")
            };
            s.decoyPool = new List<HazardDefinition>
            {
                H("decoy_bag", "Decoy_Bag", false), H("decoy_chair", "Decoy_Chair", false),
                H("decoy_bottle", "Decoy_Bottle", false), H("decoy_plant", "Decoy_Plant", false)
            };

            s.steps = new List<StepDefinition>
            {
                new StepDefinition { stepId = "hazards", type = StepType.HazardScan, weight = 15, timeLimitSeconds = 45, maxMistakes = 2, targetCount = 2,
                    instructionKey = "instr.hazards", hintKey = "hint.hazards", tipKey = "tip.hazards" },
                new StepDefinition { stepId = "response", type = StepType.ResponseCheck, weight = 10, timeLimitSeconds = 20, maxMistakes = 0,
                    orderedIds = new List<string> { "casualty_shoulder_l", "casualty_shoulder_r" },
                    instructionKey = "instr.response", hintKey = "hint.response", tipKey = "tip.response" },
                new StepDefinition { stepId = "call", type = StepType.CallHelp, weight = 20, critical = true, timeLimitSeconds = 60, maxMistakes = 2,
                    expectedText = "112", orderedIds = new List<string> { "location", "what_happened", "breathing" },
                    instructionKey = "instr.call", hintKey = "hint.call", tipKey = "tip.call" },
                new StepDefinition { stepId = "breathing", type = StepType.BreathingCheck, weight = 10, timeLimitSeconds = 25, maxMistakes = 1,
                    minValue = 5, maxValue = 10,
                    instructionKey = "instr.breathing", hintKey = "hint.breathing", tipKey = "tip.breathing" },
                new StepDefinition { stepId = "compressions", type = StepType.Compressions, branch = StepBranch.NotBreathing, weight = 45, critical = true,
                    timeLimitSeconds = 90, maxMistakes = 2, targetCount = 30, assessmentRounds = 2, minValue = 100, maxValue = 120,
                    radius = 0.06f, requiredInRange = 0.7f, requiredOnTarget = 0.8f,
                    instructionKey = "instr.compressions", hintKey = "hint.compressions", tipKey = "tip.compressions" },
                new StepDefinition { stepId = "recovery", type = StepType.RecoveryPosition, branch = StepBranch.Breathing, weight = 45, critical = true,
                    timeLimitSeconds = 90, maxMistakes = 2, orderedIds = new List<string>(RecoveryOrder),
                    instructionKey = "instr.recovery", hintKey = "hint.recovery.near_arm", tipKey = "tip.recovery" },
            };
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            return s;
        }

        private static HazardDefinition H(string id, string key, bool danger = true)
            => new HazardDefinition { hazardId = id, addressKey = key, nameKey = "hazard." + id, isDangerous = danger };

        // ---------- Text ----------

        public static (TextTable en, TextTable pcm) BuildTextTables()
        {
            EnsureFolders();
            var en = Table("Text_en", Language.English, 1);
            var pcm = Table("Text_pcm", Language.Pidgin, 2);
            return (en, pcm);
        }

        private static TextTable Table(string name, Language lang, int column)
        {
            string path = $"{DataDir}/{name}.asset";
            var t = AssetDatabase.LoadAssetAtPath<TextTable>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<TextTable>(); AssetDatabase.CreateAsset(t, path); }
            t.language = lang;
            t.entries.Clear();
            var rows = TextContent.Rows;
            for (int i = 0; i < rows.GetLength(0); i++)
                t.entries.Add(new TextTable.Entry { key = rows[i, 0], text = rows[i, column] });
            EditorUtility.SetDirty(t);
            return t;
        }

        // ---------- Theme + base sprites ----------

        public static UITheme BuildTheme()
        {
            EnsureFolders();
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { theme = ScriptableObject.CreateInstance<UITheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }

            theme.rounded = RoundedSprite("ui_rounded", 128, 36, 40);
            theme.pill = RoundedSprite("ui_pill", 128, 63, 63);
            theme.circle = RoundedSprite("ui_circle", 256, 127, 0);
            if (theme.font == null) theme.font = TMP_Settings.defaultFontAsset;
            if (theme.displayFont == null) theme.displayFont = theme.font;

            // Affinity exports dropped into UI/Sprites/Icons as icon_<name>.png are picked up automatically.
            theme.icons.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { Sprites }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var file = Path.GetFileNameWithoutExtension(p);
                if (!file.StartsWith("icon_")) continue;
                theme.icons.Add(new UITheme.NamedSprite { name = file.Substring(5), sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p) });
            }
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}/logo.png");
            if (logo != null) theme.logo = logo;

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>White anti-aliased rounded rectangle, imported as a 9-sliced sprite.</summary>
        private static Sprite RoundedSprite(string name, int size, int radius, int border)
        {
            string path = $"{Sprites}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spriteBorder = new Vector4(border, border, border, border);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
