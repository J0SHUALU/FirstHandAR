using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstHand.Presentation
{
    [CreateAssetMenu(menuName = "FirstHand/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Serializable] public struct NamedSprite { public string name; public Sprite sprite; }

        public const float Scale = 1080f / 390f;
        public static float D(float conceptPx) => conceptPx * Scale;

        [Header("Colours")]
        public Color paper = Hex("F4EBDD");
        public Color paper2 = Hex("EADBC4");
        public Color card = Hex("FFF8EE");
        public Color ink = Hex("13302A");
        public Color ink2 = Hex("2E4A42");
        public Color muted = Hex("6B7F77");
        public Color line = Hex("D6C6AC");
        public Color coral = Hex("FF6A2B");
        public Color coralDeep = Hex("D94E14");
        public Color coralTint = Hex("FFE0CF");
        public Color teal = Hex("2FA36B");
        public Color tealDeep = Hex("1E7A4E");
        public Color tealTint = Hex("D9F0E1");
        public Color amber = Hex("F2B233");
        public Color amberDeep = Hex("8F6300");
        public Color amberTint = Hex("FBEBC4");
        public Color scrim = new Color(0.075f, 0.19f, 0.165f, 0.45f);
        public float shadow = 4f;

        [Header("Fonts (TextMesh Pro)")]
        public TMP_FontAsset headBold;
        public TMP_FontAsset headSemi;
        public TMP_FontAsset body;
        public TMP_FontAsset bodyMedium;
        public TMP_FontAsset mono;
        public TMP_FontAsset monoMedium;

        [Header("Shapes (generated, 9-sliced)")]
        public Sprite card20;
        public Sprite card20Line;
        public Sprite button16;
        public Sprite button16Line;
        public Sprite chip12;
        public Sprite sheetTop;
        public Sprite sheetTopLine;
        public Sprite pill;
        public Sprite circle;
        public Sprite circleLine;
        public Sprite ring;
        public Sprite mark;

        [Header("Icons (from Affinity, white, tinted at runtime)")]
        public List<NamedSprite> icons = new List<NamedSprite>();

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
            int v = Convert.ToInt32(hex, 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }
    }
}
