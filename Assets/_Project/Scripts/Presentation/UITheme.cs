using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Design tokens from the Affinity concepts ("Clinical light"). Sizes are written in concept pixels
    /// (390 x 844 phone) and converted with <see cref="D"/>, so code reads the same as the design file.
    /// </summary>
    [CreateAssetMenu(menuName = "FirstHand/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Serializable] public struct NamedSprite { public string name; public Sprite sprite; }

        public const float Scale = 1080f / 390f; // concept px -> canvas units (reference 1080 x 2337)
        public static float D(float conceptPx) => conceptPx * Scale;

        [Header("Colours")]
        public Color paper = Hex("FAFAF7");
        public Color paper2 = Hex("F1F0EB");
        public Color card = Hex("FFFFFF");
        public Color ink = Hex("0F172A");
        public Color ink2 = Hex("334155");
        public Color muted = Hex("64748B");
        public Color line = Hex("E4E2DC");
        public Color coral = Hex("FF4D4F");
        public Color coralDeep = Hex("E23B3D");
        public Color coralTint = Hex("FFE8E8");
        public Color teal = Hex("14B8A6");
        public Color tealDeep = Hex("0F766E");
        public Color tealTint = Hex("DDF5F2");
        public Color amber = Hex("F59E0B");
        public Color amberDeep = Hex("B45309");
        public Color amberTint = Hex("FEF1D6");
        public Color scrim = new Color(0.06f, 0.09f, 0.16f, 0.38f);

        [Header("Fonts (TextMesh Pro)")]
        public TMP_FontAsset headBold;    // Inter Tight Bold
        public TMP_FontAsset headSemi;    // Inter Tight SemiBold
        public TMP_FontAsset body;        // Instrument Sans Regular
        public TMP_FontAsset bodyMedium;  // Instrument Sans Medium
        public TMP_FontAsset mono;        // Geist Mono Regular
        public TMP_FontAsset monoMedium;  // Geist Mono Medium

        [Header("Shapes (generated, 9-sliced)")]
        public Sprite card20;      // radius 20 (cards)
        public Sprite card20Line;  // radius 20 outline
        public Sprite button16;    // radius 16 (buttons, tiles)
        public Sprite button16Line;
        public Sprite chip12;      // radius 12 (chips, small panels)
        public Sprite sheetTop;    // radius 28, top corners only (bottom sheets)
        public Sprite pill;        // fully round
        public Sprite circle;
        public Sprite circleLine;
        public Sprite ring;        // thick ring for progress (radial fill)
        public Sprite mark;        // brand mark (app icon)

        [Header("Icons (from Affinity, white, tinted at runtime)")]
        public List<NamedSprite> icons = new List<NamedSprite>();

        // Back-compat for code that still reads these
        public Color background => paper;
        public Color surface => card;
        public Color text => ink;
        public Color textMuted => muted;
        public Color primary => coral;
        public Color success => teal;
        public Color warning => amber;

        public Sprite Icon(string name)
        {
            foreach (var i in icons) if (i.name == name) return i.sprite;
            return null;
        }

        public static Color Hex(string hex)
        {
            // Plain parsing (no Unity API) so it is safe in field initialisers during serialisation.
            int v = Convert.ToInt32(hex, 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }
    }
}
