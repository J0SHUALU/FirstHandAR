using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;

namespace FirstHand.Presentation
{
    /// <summary>
    /// One screen. Subclasses build their widgets once and subscribe to the events they display only while
    /// visible, so hidden screens cost nothing and never react to the wrong state.
    /// </summary>
    public abstract class View
    {
        public RectTransform Root { get; private set; }
        protected UITheme T => UIKit.Theme;
        protected UIManager UI { get; private set; }
        public bool Visible => Root != null && Root.gameObject.activeSelf;
        protected static float D(float v) => UIKit.D(v);

        public void Create(RectTransform parent, UIManager ui)
        {
            UI = ui;
            Root = UIKit.Rect(parent, GetType().Name);
            UIKit.Stretch(Root);
            Build();
            Root.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (!Visible) Root.gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            if (!Visible) return;
            OnHide();
            Root.gameObject.SetActive(false);
        }

        protected abstract void Build();
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        /// <summary>UI never calls the simulation directly; it publishes the action and the current state decides.</summary>
        protected static void Act(string action, string value = null)
            => EventBus.Publish(new UiActionEvent { Action = action, Value = value });

        /// <summary>Status chip at the top of AR screens: coloured dot + short text.</summary>
        protected TextMeshProUGUI StatusChip(Transform parent, string key, Color dot, out Image dotImg)
        {
            var chip = UIKit.Img(parent, "Chip", T.pill, T.card);
            var rt = chip.rectTransform;
            UIKit.Place(rt, 24, 56, 10, 40);
            var h = UIKit.HStack(rt, 10, 14, 0);
            UIKit.Fit(chip, false, true);
            dotImg = UIKit.Img(rt, "Dot", T.circle, dot);
            UIKit.Size(dotImg, 10, 10);
            var label = UIKit.Text(rt, key, TextStyle.Title, T.ink, TextAlignmentOptions.MidlineLeft, 14);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        protected static RectTransform Corner(RectTransform parent, float x, float y, float w, float h)
            => UIKit.Place(UIKit.Rect(parent, "Corner"), x, y, w, h);
    }

    /// <summary>Overlay for dialogs (PIN pad, confirmations, settings): a scrim and a bottom sheet. One at a time.</summary>
    public class ModalLayer
    {
        private readonly RectTransform root;
        private RectTransform sheet;
        private UITheme T => UIKit.Theme;
        public bool IsOpen => root.gameObject.activeSelf;

        public ModalLayer(RectTransform parent)
        {
            var scrim = UIKit.Img(parent, "ModalLayer", null, UIKit.Theme.scrim, true);
            root = scrim.rectTransform;
            UIKit.Stretch(root);
            root.gameObject.SetActive(false);
        }

        public RectTransform Open(string titleKey, params object[] args)
        {
            Close();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var s = UIKit.Img(root, "Sheet", T.sheetTop, T.card, true);
            sheet = s.rectTransform;
            sheet.anchorMin = new Vector2(0, 0); sheet.anchorMax = new Vector2(1, 0); sheet.pivot = new Vector2(0.5f, 0);
            sheet.anchoredPosition = Vector2.zero;
            sheet.sizeDelta = Vector2.zero; // full width; height comes from the fitter
            var v = UIKit.VStack(sheet, 16, 24, 0);
            v.padding = new RectOffset((int)UIKit.D(24), (int)UIKit.D(24), (int)UIKit.D(36), (int)UIKit.D(34));
            UIKit.Fit(s);
            var handle = UIKit.Img(sheet, "Handle", T.pill, T.line);
            var hl = handle.gameObject.AddComponent<LayoutElement>(); hl.ignoreLayout = true;
            var h = handle.rectTransform; h.anchorMin = h.anchorMax = new Vector2(0.5f, 1); h.pivot = new Vector2(0.5f, 1);
            h.sizeDelta = new Vector2(UIKit.D(40), UIKit.D(4)); h.anchoredPosition = new Vector2(0, -UIKit.D(10));
            if (!string.IsNullOrEmpty(titleKey)) UIKit.Text(sheet, titleKey, TextStyle.H2, T.ink, TextAlignmentOptions.Left, null, args);
            return sheet;
        }

        public void Close()
        {
            if (sheet != null) UnityEngine.Object.Destroy(sheet.gameObject);
            sheet = null;
            root.gameObject.SetActive(false);
        }

        public void Message(string titleKey, string bodyKey, Action onOk = null, params object[] args)
        {
            var c = Open(titleKey);
            UIKit.Text(c, bodyKey, TextStyle.Body, T.ink2, TextAlignmentOptions.Left, null, args);
            UIKit.Space(c, 4);
            UIKit.Button(c, "ui.ok", ButtonStyle.Primary, () => { Close(); onOk?.Invoke(); }, 56, false);
        }

        public void Confirm(string titleKey, string bodyKey, Action onYes, params object[] args)
        {
            var c = Open(titleKey, args);
            UIKit.Text(c, bodyKey, TextStyle.Body, T.ink2, TextAlignmentOptions.Left, null, args);
            UIKit.Space(c, 4);
            var row = UIKit.Rect(c, "Row");
            var h = UIKit.HStack(row, 12); h.childForceExpandWidth = true; h.childControlWidth = true;
            UIKit.Size(row, 56);
            UIKit.Button(row, "ui.cancel", ButtonStyle.Secondary, Close, 56, false);
            UIKit.Button(row, "ui.yes", ButtonStyle.Danger, () => { Close(); onYes?.Invoke(); }, 56, false);
        }

        /// <summary>Four-digit PIN pad (FR-01, FR-28). Digits are shown as filled dots, never as numbers.</summary>
        public void PinPad(string titleKey, Action<string> onDone, string subtitle = null)
        {
            var c = Open(titleKey);
            if (subtitle != null) UIKit.Raw(c, subtitle, TextStyle.Label);
            string pin = "";
            var dotsRow = UIKit.Rect(c, "Dots");
            UIKit.HStack(dotsRow, 18, 0, 0, TextAnchor.MiddleCenter);
            UIKit.Size(dotsRow, 44);
            var dots = new Image[4];
            for (int i = 0; i < 4; i++) { dots[i] = UIKit.Img(dotsRow, "Dot", T.circleLine, T.ink); UIKit.Size(dots[i], 16, 16); }
            void Redraw() { for (int i = 0; i < 4; i++) { dots[i].sprite = i < pin.Length ? T.circle : T.circleLine; } }

            var grid = UIKit.Rect(c, "Keys");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(UIKit.D(64), UIKit.D(64));
            g.spacing = new Vector2(UIKit.D(32), UIKit.D(12));
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
            UIKit.Size(grid, 4 * 64 + 3 * 12);
            foreach (var k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "del", "0", "ok" })
            {
                string key = k;
                Image cell;
                if (key == "del") { cell = UIKit.Img(grid, "Del", null, Color.clear, true); var ic = UIKit.Img(cell.transform, "Icon", T.Icon("backspace"), T.ink); ic.preserveAspect = true; ic.rectTransform.sizeDelta = new Vector2(UIKit.D(26), UIKit.D(26)); }
                else if (key == "ok") { cell = UIKit.Img(grid, "Ok", T.circle, T.ink, true); var ic = UIKit.Img(cell.transform, "Icon", T.Icon("arrow"), Color.white); ic.preserveAspect = true; ic.rectTransform.sizeDelta = new Vector2(UIKit.D(24), UIKit.D(24)); }
                else { cell = UIKit.Img(grid, "Key" + key, T.circle, T.paper2, true); var t = UIKit.Raw(cell.transform, key, TextStyle.H2, T.ink, TextAlignmentOptions.Center, 26); UIKit.Stretch(t.rectTransform); }
                var b = cell.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() =>
                {
                    if (key == "del") { if (pin.Length > 0) pin = pin.Substring(0, pin.Length - 1); }
                    else if (key == "ok") { if (pin.Length == 4) { Close(); onDone(pin); } return; }
                    else if (pin.Length < 4) pin += key;
                    Redraw();
                });
            }
            UIKit.Button(c, "ui.cancel", ButtonStyle.Ghost, Close, 48, false);
        }
    }
}
