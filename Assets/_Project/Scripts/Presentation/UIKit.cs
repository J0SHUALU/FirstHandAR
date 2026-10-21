using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstHand.Presentation
{
    public enum ButtonStyle { Primary, Secondary, Success, Ghost, Danger }

    /// <summary>
    /// Small builder for uGUI so every screen is assembled the same way, from the same theme tokens.
    /// All text goes through LocText (language switching) and is registered for text scaling (FR-04).
    /// </summary>
    public static class UIKit
    {
        public static UITheme Theme;
        private static readonly List<(TextMeshProUGUI label, float baseSize)> scalable = new List<(TextMeshProUGUI, float)>();
        private static float textScale = 1f;

        public static void SetTextScale(float scale)
        {
            textScale = scale;
            scalable.RemoveAll(s => s.label == null);
            foreach (var (label, size) in scalable) label.fontSize = size * scale;
        }

        // ---------- Layout ----------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Anchor to top / bottom edge, full width, fixed height.</summary>
        public static RectTransform Dock(RectTransform rt, bool top, float height, float inset = 0)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.sizeDelta = new Vector2(-2 * inset, height);
            rt.anchoredPosition = new Vector2(0, top ? -inset : inset);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite ?? Theme.rounded;
            img.type = img.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = true;
            return img;
        }

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing = -1, float padding = -1, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing < 0 ? Theme.spacing : spacing;
            int p = Mathf.RoundToInt(padding < 0 ? Theme.padding : padding);
            v.padding = new RectOffset(p, p, p, p);
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing = -1, float padding = 0)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing < 0 ? Theme.spacing : spacing;
            int p = Mathf.RoundToInt(padding);
            h.padding = new RectOffset(p, p, p, p);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            return h;
        }

        public static LayoutElement Size(Component c, float height = -1, float width = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) { le.minHeight = height; le.preferredHeight = height; }
            if (width >= 0) { le.minWidth = width; le.preferredWidth = width; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static void Fit(Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ---------- Content ----------

        public static TextMeshProUGUI Label(Transform parent, string key, float size = -1, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool display = false)
        {
            var rt = Rect(parent, "Text_" + key);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = display && Theme.displayFont != null ? Theme.displayFont : Theme.font;
            if (font != null) t.font = font;
            float baseSize = size < 0 ? Theme.bodySize : size;
            t.fontSize = baseSize * textScale;
            t.color = color ?? Theme.text;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            scalable.Add((t, baseSize));
            var loc = rt.gameObject.AddComponent<LocText>();
            if (!string.IsNullOrEmpty(key)) loc.Set(key);
            return t;
        }

        /// <summary>A label whose text is set directly (names, numbers) rather than from the text table.</summary>
        public static TextMeshProUGUI Raw(Transform parent, string text, float size = -1, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool display = false)
        {
            var t = Label(parent, null, size, color, align, bold, display);
            UnityEngine.Object.DestroyImmediate(t.GetComponent<LocText>());
            t.text = text;
            return t;
        }

        public static Image Icon(Transform parent, string name, float size, Color? tint = null)
        {
            var rt = Rect(parent, "Icon_" + name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.Icon(name);
            img.preserveAspect = true;
            img.color = tint ?? Theme.text;
            img.raycastTarget = false;
            img.enabled = img.sprite != null;
            Size(img, size, size);
            return img;
        }

        public static Button Button(Transform parent, string key, ButtonStyle style, Action onClick, string icon = null, float height = -1)
        {
            var bg = Panel(parent, "Btn_" + key, StyleColor(style), Theme.pill ?? Theme.rounded);
            var btn = bg.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(1, 1, 1, 0.35f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            Size(bg, height < 0 ? Theme.buttonHeight : height);

            // Label fills the button and is centred; an optional icon sits on the left edge.
            bool hasIcon = !string.IsNullOrEmpty(icon) && Theme.Icon(icon) != null;
            var color = style == ButtonStyle.Ghost ? Theme.text : Color.white;
            if (hasIcon)
            {
                var ic = Icon(bg.transform, icon, 60, color);
                var irt = ic.rectTransform;
                irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
                irt.pivot = new Vector2(0, 0.5f);
                irt.sizeDelta = new Vector2(60, 60);
                irt.anchoredPosition = new Vector2(36, 0);
            }
            var label = Label(bg.transform, key, Theme.bodySize, color, TextAlignmentOptions.Center, true);
            Stretch(label.rectTransform, hasIcon ? 100 : 24, hasIcon ? 100 : 24, 0, 0);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMax = Theme.bodySize;
            label.fontSizeMin = Theme.bodySize * 0.6f;
            return btn;
        }

        public static Color StyleColor(ButtonStyle s)
        {
            switch (s)
            {
                case ButtonStyle.Primary: return Theme.primary;
                case ButtonStyle.Success: return Theme.success;
                case ButtonStyle.Danger: return new Color(0.55f, 0.12f, 0.15f);
                case ButtonStyle.Ghost: return new Color(1, 1, 1, 0.08f);
                default: return Theme.surfaceRaised;
            }
        }

        public static TMP_InputField Input(Transform parent, string placeholderKey, int limit, TMP_InputField.ContentType type)
        {
            var bg = Panel(parent, "Input", Theme.surfaceRaised);
            Size(bg, Theme.buttonHeight);
            var area = Rect(bg.transform, "Text Area");
            Stretch(area, 36, 36, 10, 10);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Label(area, placeholderKey, Theme.bodySize, Theme.textMuted, TextAlignmentOptions.Left);
            Stretch(ph.rectTransform);
            var txt = Raw(area, "", Theme.bodySize, Theme.text, TextAlignmentOptions.Left);
            Stretch(txt.rectTransform);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = limit;
            input.contentType = type;
            return input;
        }

        public static Image Bar(Transform parent, float height, out Image fill, Color fillColor)
        {
            var bg = Panel(parent, "Bar", new Color(1, 1, 1, 0.12f), Theme.pill);
            Size(bg, height);
            fill = Panel(bg.transform, "Fill", fillColor, Theme.pill);
            fill.raycastTarget = false;
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return bg;
        }

        public static void SetBar(Image fill, float t)
        {
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(t), 1);
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Vertical scroll area; returns the content transform to fill (auto-sized by its layout).</summary>
        public static RectTransform Scroll(RectTransform viewportParent, float spacing = -1, float padding = -1)
        {
            var viewport = Rect(viewportParent, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            VStack(content, spacing, padding);
            Fit(content);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
