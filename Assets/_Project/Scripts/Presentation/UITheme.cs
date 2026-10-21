using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Visual tokens for the runtime-built UI: colours, font, and the sprites exported from the Affinity
    /// designs (rounded panels, icons). Swapping the look is an asset change, not a code change.
    /// </summary>
    [CreateAssetMenu(menuName = "FirstHand/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Serializable] public struct NamedSprite { public string name; public Sprite sprite; }

        [Header("Colours")]
        public Color background = new Color32(0x0E, 0x1A, 0x2B, 0xF2);
        public Color surface = new Color32(0x17, 0x2A, 0x42, 0xF5);
        public Color surfaceRaised = new Color32(0x22, 0x3A, 0x58, 0xFF);
        public Color text = new Color32(0xF7, 0xF3, 0xEA, 0xFF);
        public Color textMuted = new Color32(0xA9, 0xB7, 0xC9, 0xFF);
        public Color primary = new Color32(0xE6, 0x39, 0x46, 0xFF);   // emergency red
        public Color success = new Color32(0x2B, 0xB6, 0x73, 0xFF);
        public Color warning = new Color32(0xF4, 0xA3, 0x00, 0xFF);
        public Color info = new Color32(0x3A, 0x9B, 0xDC, 0xFF);
        public Color scrim = new Color(0, 0, 0, 0.55f);

        [Header("Type")]
        public TMP_FontAsset font;
        public TMP_FontAsset displayFont;
        public float bodySize = 44;
        public float titleSize = 64;
        public float displaySize = 150;

        [Header("Sprites (from Affinity)")]
        public Sprite rounded;      // 9-sliced rounded rectangle
        public Sprite pill;         // 9-sliced fully rounded
        public Sprite circle;
        public Sprite logo;
        public List<NamedSprite> icons = new List<NamedSprite>();

        [Header("Sizes")]
        public float buttonHeight = 150;  // ~48dp+ at the 1080x1920 reference (NFR-05)
        public float spacing = 28;
        public float padding = 48;

        public Sprite Icon(string name)
        {
            foreach (var i in icons) if (i.name == name) return i.sprite;
            return null;
        }
    }
}
