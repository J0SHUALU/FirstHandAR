using System;
using System.Collections.Generic;
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
            if (Visible) { OnShow(); return; }
            Root.gameObject.SetActive(true);
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

        protected Image Card(Transform parent, string name, Color? color = null)
            => UIKit.Panel(parent, name, color ?? T.surface);
    }

    /// <summary>Overlay layer for dialogs (PIN pad, confirmations, settings). One at a time.</summary>
    public class ModalLayer
    {
        private readonly RectTransform root;
        private RectTransform card;
        private UITheme T => UIKit.Theme;

        public bool IsOpen => root.gameObject.activeSelf;

        public ModalLayer(RectTransform parent)
        {
            var scrim = UIKit.Panel(parent, "ModalLayer", UIKit.Theme.scrim);
            scrim.sprite = null; scrim.type = Image.Type.Simple;
            root = scrim.rectTransform;
            UIKit.Stretch(root);
            root.gameObject.SetActive(false);
        }

        public RectTransform Open(string titleKey, params object[] args)
        {
            Close();
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var c = UIKit.Panel(root, "Card", T.surface);
            card = c.rectTransform;
            card.anchorMin = new Vector2(0, 0.5f); card.anchorMax = new Vector2(1, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(-2 * T.padding, 0);
            UIKit.VStack(card);
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(titleKey))
            {
                var title = UIKit.Label(card, titleKey, T.titleSize * 0.8f, null, TextAlignmentOptions.Center, true);
                if (args.Length > 0) title.GetComponent<LocText>().Set(titleKey, args);
            }
            return card;
        }

        public void Close()
        {
            if (card != null) UnityEngine.Object.Destroy(card.gameObject);
            card = null;
            root.gameObject.SetActive(false);
        }

        public void Message(string titleKey, string bodyKey, Action onOk = null, params object[] args)
        {
            var c = Open(titleKey);
            var body = UIKit.Label(c, bodyKey, T.bodySize, T.textMuted);
            if (args.Length > 0) body.GetComponent<LocText>().Set(bodyKey, args);
            UIKit.Button(c, "ui.ok", ButtonStyle.Primary, () => { Close(); onOk?.Invoke(); });
        }

        public void Confirm(string titleKey, string bodyKey, Action onYes, params object[] args)
        {
            var c = Open(titleKey);
            var body = UIKit.Label(c, bodyKey, T.bodySize, T.textMuted);
            if (args.Length > 0) body.GetComponent<LocText>().Set(bodyKey, args);
            var row = UIKit.Rect(c, "Row");
            UIKit.HStack(row);
            UIKit.Size(row, T.buttonHeight);
            UIKit.Button(row, "ui.cancel", ButtonStyle.Secondary, Close);
            UIKit.Button(row, "ui.yes", ButtonStyle.Danger, () => { Close(); onYes?.Invoke(); });
        }

        /// <summary>Four-digit PIN pad (FR-01, FR-28). Digits are shown as dots, never as numbers.</summary>
        public void PinPad(string titleKey, Action<string> onDone, string subtitle = null)
        {
            var c = Open(titleKey);
            if (subtitle != null) UIKit.Raw(c, subtitle, T.bodySize, T.textMuted);
            string pin = "";
            var dots = UIKit.Raw(c, "_ _ _ _", T.titleSize, T.text, TextAlignmentOptions.Center, true);
            UIKit.Size(dots, 110);

            void Redraw()
            {
                var s = "";
                for (int i = 0; i < 4; i++) s += (i < pin.Length ? "*" : "_") + (i < 3 ? " " : "");
                dots.text = s;
            }

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "del", "0", "ok" };
            var grid = UIKit.Rect(c, "Grid");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(250, 150);
            g.spacing = new Vector2(24, 24);
            g.childAlignment = TextAnchor.MiddleCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            UIKit.Size(grid, 4 * 150 + 3 * 24);

            foreach (var k in keys)
            {
                string key = k;
                var style = key == "ok" ? ButtonStyle.Success : key == "del" ? ButtonStyle.Ghost : ButtonStyle.Secondary;
                var b = UIKit.Button(grid, key == "ok" ? "ui.ok" : key == "del" ? "ui.delete" : "digit." + key, style, () =>
                {
                    if (key == "del") { if (pin.Length > 0) pin = pin.Substring(0, pin.Length - 1); }
                    else if (key == "ok") { if (pin.Length == 4) { Close(); onDone(pin); } return; }
                    else if (pin.Length < 4) pin += key;
                    Redraw();
                });
                UnityEngine.Object.Destroy(b.GetComponent<LayoutElement>());
            }
            UIKit.Button(c, "ui.cancel", ButtonStyle.Ghost, Close);
        }
    }
}
