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

        // ---------- Theme: shapes, fonts, icons ----------

        public const string Fonts = Root + "/UI/Fonts";
        public const string IconDir = Root + "/UI/Icon";

        public static UITheme BuildTheme()
        {
            EnsureFolders();
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { theme = ScriptableObject.CreateInstance<UITheme>(); AssetDatabase.CreateAsset(theme, ThemePath); }

            // Palette ("Warm Lagos street"). Set here so an existing asset picks up changes.
            theme.paper = UITheme.Hex("F4EBDD"); theme.paper2 = UITheme.Hex("EADBC4"); theme.card = UITheme.Hex("FFF8EE");
            theme.ink = UITheme.Hex("13302A"); theme.ink2 = UITheme.Hex("2E4A42"); theme.muted = UITheme.Hex("6B7F77"); theme.line = UITheme.Hex("D6C6AC");
            theme.coral = UITheme.Hex("FF6A2B"); theme.coralDeep = UITheme.Hex("D94E14"); theme.coralTint = UITheme.Hex("FFE0CF");
            theme.teal = UITheme.Hex("2FA36B"); theme.tealDeep = UITheme.Hex("1E7A4E"); theme.tealTint = UITheme.Hex("D9F0E1");
            theme.amber = UITheme.Hex("F2B233"); theme.amberDeep = UITheme.Hex("8F6300"); theme.amberTint = UITheme.Hex("FBEBC4");
            theme.scrim = new Color(0.075f, 0.19f, 0.165f, 0.45f);
            theme.shadow = 4f;

            // Shapes at canvas resolution (1 px = 1 canvas unit), radii taken from the Affinity concepts.
            int R(float concept) => Mathf.RoundToInt(UITheme.D(concept));
            theme.card20 = Shape("shape_card20", R(20), 0, false);
            theme.card20Line = Shape("shape_card20_line", R(20), 6, false);
            theme.button16 = Shape("shape_button16", R(16), 0, false);
            theme.button16Line = Shape("shape_button16_line", R(16), 6, false);
            theme.chip12 = Shape("shape_chip12", R(12), 0, false);
            theme.sheetTop = Shape("shape_sheet_top", R(28), 0, true);
            theme.sheetTopLine = Shape("shape_sheet_top_line", R(28), 6, true);
            theme.pill = Shape("shape_pill", 64, 0, false);
            theme.circle = Disc("shape_circle", 256, 0);
            theme.circleLine = Disc("shape_circle_line", 256, 18);
            theme.ring = Disc("shape_ring", 256, 26);

            // Fonts (Bricolage Grotesque, Instrument Sans, Geist Mono; all SIL OFL)
            theme.headBold = FontAsset("BricolageGrotesque-ExtraBold");
            theme.headSemi = FontAsset("BricolageGrotesque-Bold");
            theme.body = FontAsset("InstrumentSans_400Regular");
            theme.bodyMedium = FontAsset("InstrumentSans_500Medium");
            theme.mono = FontAsset("GeistMono-Regular");
            theme.monoMedium = FontAsset("GeistMono-Medium");

            // Icons exported from Affinity (icon_<name>.png, white, tinted at runtime)
            theme.icons.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Sprites }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var file = Path.GetFileNameWithoutExtension(p);
                if (!file.StartsWith("icon_")) continue;
                theme.icons.Add(new UITheme.NamedSprite { name = file.Substring(5), sprite = ImportSprite(p, Vector4.zero) });
            }

            // Brand mark = the app icon artwork
            string markPath = $"{Sprites}/brand_mark.png";
            if (!File.Exists(markPath) && File.Exists($"{IconDir}/AppIcon-1024.png")) { File.Copy($"{IconDir}/AppIcon-1024.png", markPath); AssetDatabase.ImportAsset(markPath); }
            if (File.Exists(markPath)) theme.mark = ImportSprite(markPath, Vector4.zero, 256);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        private static TMP_FontAsset FontAsset(string file)
        {
            string assetPath = $"{Fonts}/{file} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{Fonts}/{file}.ttf");
            if (font == null) { Debug.LogWarning("[Theme] Missing font " + file); return TMP_Settings.defaultFontAsset; }
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                                                   AtlasPopulationMode.Dynamic, true);
            fa.name = file + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.atlasTextures[0].name = file + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = file + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz{|}~·×—–’");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        /// <summary>Anti-aliased rounded rectangle (filled or outline), imported 9-sliced.</summary>
        private static Sprite Shape(string name, int radius, int stroke, bool topOnly)
        {
            string path = $"{Sprites}/{name}.png";
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                bool roundCorner = !topOnly || fy > size / 2f; // texture y is up: top half has the rounded corners
                float r = roundCorner ? radius : 0;
                float cx = Mathf.Clamp(fx, r, size - r), cy = Mathf.Clamp(fy, r, size - r);
                float d = r > 0 ? Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy)) - r
                                : -Mathf.Min(Mathf.Min(fx, size - fx), Mathf.Min(fy, size - fy));
                if (r > 0 && (fx > r && fx < size - r || fy > r && fy < size - r))
                    d = -Mathf.Min(Mathf.Min(fx, size - fx), Mathf.Min(fy, size - fy));
                float a = Mathf.Clamp01(0.5f - d);
                if (stroke > 0) a *= Mathf.Clamp01(0.5f + d + stroke);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            int b = radius + 1;
            return ImportSprite(path, topOnly ? new Vector4(b, 1, b, b) : new Vector4(b, b, b, b));
        }

        /// <summary>Anti-aliased disc or ring.</summary>
        private static Sprite Disc(string name, int size, int stroke)
        {
            string path = $"{Sprites}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float c = size / 2f, r = size / 2f - 1;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) - r;
                float a = Mathf.Clamp01(0.5f - d);
                if (stroke > 0) a *= Mathf.Clamp01(0.5f + d + stroke);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return ImportSprite(path, Vector4.zero);
        }

        private static Sprite ImportSprite(string path, Vector4 border, int maxSize = 2048)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.spriteBorder = border;
            imp.spritePixelsPerUnit = 100;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
