using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstHand.Presentation
{
    public enum ButtonStyle { Primary, Teal, Secondary, Soft, Ghost, Danger }
    public enum TextStyle { Display, H1, H2, Title, Body, BodyMedium, Small, Label, Mono, MonoLarge, Number }

    /// <summary>
    /// Builds uGUI from the Affinity design tokens. Positions and sizes are given in concept pixels
    /// (390 x 844 frame) and converted with UITheme.D, so values can be read straight off the design.
    /// All text goes through LocText (language switching) and is registered for text scaling (FR-04).
    /// </summary>
    public static class UIKit
    {
        public static UITheme Theme;
        static UITheme T => Theme;
        public static float D(float v) => UITheme.D(v);

        private static readonly List<(TextMeshProUGUI label, float baseSize)> scalable = new List<(TextMeshProUGUI, float)>();
        private static float textScale = 1f;

        public static void SetTextScale(float scale)
        {
            textScale = scale;
            scalable.RemoveAll(s => s.label == null);
            foreach (var (label, size) in scalable)
            {
                label.fontSize = size * scale;
                if (label.enableAutoSizing) label.fontSizeMax = size * scale;
            }
        }

        // ---------- Rects ----------

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
            rt.offsetMin = new Vector2(D(l), D(b)); rt.offsetMax = new Vector2(-D(r), -D(t));
            return rt;
        }

        /// <summary>Place by top-left corner, measured from the parent's top-left (concept px).</summary>
        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(D(x), -D(y));
            rt.sizeDelta = new Vector2(D(w), D(h));
            return rt;
        }

        /// <summary>Full-width strip with side margins, anchored to the top or bottom edge (concept px).</summary>
        public static RectTransform Band(RectTransform rt, bool top, float offset, float height, float side = 24)
        {
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.sizeDelta = new Vector2(-2 * D(side), D(height));
            rt.anchoredPosition = new Vector2(0, top ? -D(offset) : D(offset));
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite == T.pill) rt.gameObject.AddComponent<PillFit>();
            return img;
        }

        public static Image Card(Transform parent, string name, Color? color = null, bool outline = false)
        {
            var img = Img(parent, name, T.card20, color ?? T.card, true);
            // Street poster look: every card is a sticker with an ink outline and a hard offset shadow.
            // (The old 'outline' flag only chose a light hairline; all cards are outlined now.)
            Sticker(img, T.card20Line);
            return img;
        }

        /// <summary>Ink outline overlay plus a hard, unblurred shadow down and to the right.</summary>
        public static void Sticker(Image img, Sprite outlineSprite, Color? shadowColor = null, bool outline = true)
        {
            if (outline && outlineSprite != null)
            {
                var o = Img(img.transform, "Outline", outlineSprite, T.ink);
                Stretch((RectTransform)o.transform); Overlay(o);
            }
            HardShadow(img, shadowColor ?? T.ink);
        }

        public static void HardShadow(Graphic g, Color color, float distance = -1)
        {
            float d = D(distance < 0 ? T.shadow : distance);
            var sh = g.GetComponent<Shadow>() ?? g.gameObject.AddComponent<Shadow>();
            sh.effectColor = color;
            sh.effectDistance = new Vector2(d, -d);
            sh.useGraphicAlpha = true;
        }

        /// <summary>Keeps decorative overlays (outlines, selection rings) out of layout groups.</summary>
        public static void Overlay(Component c)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            if (c is Graphic g) g.raycastTarget = false;
        }

        public const float Bleed = 400f;

        public static Image Fill(Transform parent, string name, Color color)
        {
            var img = Img(parent, name, null, color, true);
            // Full screen fills sit inside the safe area; bleed past it so notches and home bars are covered too.
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-Bleed, -Bleed); rt.offsetMax = new Vector2(Bleed, Bleed);
            return img;
        }

        // ---------- Layout ----------

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, float padH = 0, float padV = 0, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = D(spacing);
            v.padding = new RectOffset((int)D(padH), (int)D(padH), (int)D(padV), (int)D(padV));
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, float padH = 0, float padV = 0, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = D(spacing);
            h.padding = new RectOffset((int)D(padH), (int)D(padH), (int)D(padV), (int)D(padV));
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Size(Component c, float height = -1, float width = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) { le.minHeight = D(height); le.preferredHeight = D(height); }
            if (width >= 0) { le.minWidth = D(width); le.preferredWidth = D(width); }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static void Fit(Component c, bool vertical = true, bool horizontal = false)
        {
            var f = c.gameObject.GetComponent<ContentSizeFitter>() ?? c.gameObject.AddComponent<ContentSizeFitter>();
            if (vertical) f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (horizontal) f.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static RectTransform Space(Transform parent, float height)
        {
            var rt = Rect(parent, "Space");
            Size(rt, height);
            return rt;
        }

        // ---------- Text ----------

        static (TMP_FontAsset font, float size, float spacing) Style(TextStyle s)
        {
            switch (s)
            {
                case TextStyle.Display: return (T.headBold, 120, -3f);
                case TextStyle.H1: return (T.headBold, 36, -1.5f);
                case TextStyle.H2: return (T.headSemi, 22, -1f);
                case TextStyle.Title: return (T.headSemi, 17, 0);
                case TextStyle.Body: return (T.body, 15, 0);
                case TextStyle.BodyMedium: return (T.bodyMedium, 15, 0);
                case TextStyle.Small: return (T.body, 13, 0);
                case TextStyle.Label: return (T.mono, 11, 6f);
                case TextStyle.Mono: return (T.mono, 13, 0);
                case TextStyle.MonoLarge: return (T.monoMedium, 62, -2f);
                case TextStyle.Number: return (T.headSemi, 24, -1f);
                default: return (T.body, 15, 0);
            }
        }

        public static TextMeshProUGUI Text(Transform parent, string key, TextStyle style, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Left, float? size = null, params object[] args)
        {
            var rt = Rect(parent, "T_" + (key ?? "raw"));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var (font, s, spacing) = Style(style);
            if (font != null) t.font = font;
            float baseSize = D(size ?? s);
            t.fontSize = baseSize * textScale;
            t.characterSpacing = spacing;
            t.color = color ?? (style == TextStyle.Label ? T.muted : T.ink);
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.lineSpacing = style == TextStyle.H1 ? -12 : 0;
            scalable.Add((t, baseSize));
            var loc = rt.gameObject.AddComponent<LocText>();
            loc.Upper = style == TextStyle.Label;
            if (!string.IsNullOrEmpty(key)) loc.Set(key, args);
            return t;
        }

        /// <summary>Text set directly (names, numbers), not from the text table.</summary>
        public static TextMeshProUGUI Raw(Transform parent, string text, TextStyle style, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Left, float? size = null)
        {
            var t = Text(parent, null, style, color, align, size);
            UnityEngine.Object.DestroyImmediate(t.GetComponent<LocText>());
            t.text = style == TextStyle.Label ? text.ToUpperInvariant() : text;
            return t;
        }

        // ---------- Controls ----------

        public static Image Icon(Transform parent, string name, float size, Color color)
        {
            var img = Img(parent, "Icon_" + name, T.Icon(name), color);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(D(size), D(size));
            Size(img, size, size);
            return img;
        }

        public static Button Button(Transform parent, string key, ButtonStyle style, Action onClick, float height = 56, bool arrow = true)
        {
            Color bg, fg;
            Sprite sprite = T.button16;
            switch (style)
            {
                case ButtonStyle.Teal: bg = T.teal; fg = T.ink; break;
                case ButtonStyle.Secondary: bg = T.card; fg = T.ink; break;
                case ButtonStyle.Soft: bg = T.paper2; fg = T.ink; break;
                case ButtonStyle.Ghost: bg = new Color(1, 1, 1, 0); fg = T.ink; break;
                case ButtonStyle.Danger: bg = T.coralDeep; fg = Color.white; break;
                default: bg = T.coral; fg = T.ink; break;
            }
            var img = Img(parent, "Btn_" + key, sprite, bg, true);
            if (style != ButtonStyle.Ghost) Sticker(img, T.button16Line);
            var btn = img.gameObject.AddComponent<Button>();
            var c = btn.colors; c.pressedColor = new Color(0.85f, 0.85f, 0.85f); c.disabledColor = new Color(1, 1, 1, 0.4f); btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());
            Size(img, height);

            bool showArrow = arrow && (style == ButtonStyle.Primary || style == ButtonStyle.Teal);
            var label = Text(img.transform, key, TextStyle.Title, fg, showArrow ? TextAlignmentOptions.Left : TextAlignmentOptions.Center, 16);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true; label.fontSizeMax = label.fontSize; label.fontSizeMin = label.fontSize * 0.7f;
            Stretch(label.rectTransform, 20, showArrow ? 52 : 20, 0, 0);
            if (showArrow)
            {
                var a = Img(img.transform, "Arrow", T.Icon("arrow"), fg);
                a.preserveAspect = true;
                var rt = a.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(D(20), D(20)); rt.anchoredPosition = new Vector2(-D(20), 0);
            }
            return btn;
        }

        /// <summary>Round icon button (close, settings, rotate).</summary>
        public static Button IconButton(Transform parent, string icon, Action onClick, float size = 40, Color? bg = null, Color? fg = null, bool outline = false)
        {
            var img = Img(parent, "IconBtn_" + icon, T.circle, bg ?? T.card, true);
            img.rectTransform.sizeDelta = new Vector2(D(size), D(size));
            Size(img, size, size);
            Sticker(img, T.circleLine, null, true);
            HardShadow(img, T.ink, 2.5f);
            var ic = Img(img.transform, "Icon", T.Icon(icon), fg ?? T.ink);
            ic.preserveAspect = true;
            ic.rectTransform.sizeDelta = new Vector2(D(size * 0.5f), D(size * 0.5f));
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        /// <summary>Small round status badge with an icon (check, !).</summary>
        public static Image Badge(Transform parent, string icon, Color bg, float size = 22)
        {
            var img = Img(parent, "Badge", T.circle, bg);
            img.rectTransform.sizeDelta = new Vector2(D(size), D(size));
            Size(img, size, size);
            if (icon != null)
            {
                var ic = Img(img.transform, "Icon", T.Icon(icon), Color.white);
                ic.preserveAspect = true;
                ic.rectTransform.sizeDelta = new Vector2(D(size * 0.62f), D(size * 0.62f));
            }
            return img;
        }

        /// <summary>White bottom sheet with a grab handle; returns the content area (padded, top-down stack).</summary>
        public static RectTransform Sheet(RectTransform parent, float height, out Image sheet)
        {
            sheet = Img(parent, "Sheet", T.sheetTop, T.card, true);
            var rt = sheet.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, D(height) + Bleed); rt.anchoredPosition = new Vector2(0, -Bleed); // white runs under the home bar
            var edge = Img(rt, "Outline", T.sheetTopLine, T.ink); Stretch((RectTransform)edge.transform); Overlay(edge);
            var handle = Img(rt, "Handle", T.pill, T.ink);
            var h = handle.rectTransform;
            h.anchorMin = h.anchorMax = new Vector2(0.5f, 1); h.pivot = new Vector2(0.5f, 1);
            h.sizeDelta = new Vector2(D(44), D(5)); h.anchoredPosition = new Vector2(0, -D(10));
            var content = Rect(rt, "Content");
            Stretch(content, 24, 24, 34, 24);
            content.offsetMin = new Vector2(content.offsetMin.x, content.offsetMin.y + Bleed);
            return content;
        }

        /// <summary>Thin progress bar; returns the fill so callers can move it with SetBar.</summary>
        public static Image Bar(Transform parent, float height, out Image fill, Color fillColor, Color? track = null)
        {
            var bg = Img(parent, "Bar", T.pill, track ?? T.paper2);
            Size(bg, height);
            fill = Img(bg.transform, "Fill", T.pill, fillColor);
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

        /// <summary>The ECG line from the brand, as a tinted sprite.</summary>
        public static Image Ecg(Transform parent, Color color, float height = 36)
        {
            var img = Img(parent, "ECG", T.Icon("ecg"), color);
            img.preserveAspect = false;
            Size(img, height);
            return img;
        }

        public static TMP_InputField Input(Transform parent, string placeholderKey, int limit, TMP_InputField.ContentType type)
        {
            var bg = Img(parent, "Input", T.button16, T.paper2, true);
            Size(bg, 56);
            var area = Rect(bg.transform, "Text Area");
            Stretch(area, 16, 16, 6, 6);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(area, placeholderKey, TextStyle.Body, T.muted, TextAlignmentOptions.Left);
            Stretch(ph.rectTransform);
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            var txt = Raw(area, "", TextStyle.Body, T.ink, TextAlignmentOptions.MidlineLeft);
            Stretch(txt.rectTransform);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = limit;
            input.contentType = type;
            input.pointSize = txt.fontSize;
            return input;
        }

        /// <summary>Vertical scroll area filling the parent; returns the content to fill (top-down stack).</summary>
        public static RectTransform Scroll(RectTransform parent, float spacing, float padH, float padTop, float padBottom)
        {
            var viewport = Rect(parent, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var v = VStack(content, spacing, padH, 0);
            v.padding = new RectOffset((int)D(padH), (int)D(padH), (int)D(padTop), (int)D(padBottom));
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
