using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;

namespace FirstHand.Presentation
{
    /// <summary>AR setup, part 1: plane detection feedback (FR-05, FR-06) and placement instructions.</summary>
    public class ScanView : View
    {
        private LocText status;
        private Image statusIcon;
        private Image dot;
        private float pulse;

        protected override void Build()
        {
            var top = Card(Root, "Status");
            UIKit.Dock(top.rectTransform, true, 300, T.padding);
            var h = UIKit.HStack(top.rectTransform, 28, 36);
            h.childForceExpandWidth = false;
            statusIcon = UIKit.Icon(top.transform, "scan", 120, T.info);
            var label = UIKit.Label(top.transform, "scan.looking", T.bodySize, T.text, TextAlignmentOptions.Left);
            UIKit.Size(label, -1, -1, 1);
            status = label.GetComponent<LocText>();

            // Centre reticle so the trainee knows where the tap will land.
            dot = UIKit.Panel(Root, "Reticle", new Color(1, 1, 1, 0.8f), T.circle);
            dot.raycastTarget = false;
            dot.rectTransform.sizeDelta = new Vector2(70, 70);

            var bottom = UIKit.Rect(Root, "Bottom");
            UIKit.Dock(bottom, false, T.buttonHeight, T.padding);
            UIKit.HStack(bottom);
            UIKit.Button(bottom, "ui.back", ButtonStyle.Ghost, () => Act("back"), "back");
        }

        protected override void OnShow()
        {
            status.Set("scan.looking");
            statusIcon.color = T.info;
            EventBus.Subscribe<PlaneStatusEvent>(OnPlanes);
            EventBus.Subscribe<PlacementRejectedEvent>(OnRejected);
        }

        protected override void OnHide()
        {
            EventBus.Unsubscribe<PlaneStatusEvent>(OnPlanes);
            EventBus.Unsubscribe<PlacementRejectedEvent>(OnRejected);
        }

        private void OnPlanes(PlaneStatusEvent e)
        {
            if (e.LargestBigEnough) { status.Set("scan.found"); statusIcon.color = T.success; }
            else if (e.PlaneCount > 0) { status.Set("scan.keep_going"); statusIcon.color = T.warning; }
            else if (e.ScanTimedOut) { status.Set("scan.help"); statusIcon.color = T.warning; }
            else { status.Set("scan.looking"); statusIcon.color = T.info; }
        }

        private void OnRejected(PlacementRejectedEvent e)
        {
            status.Set(e.ReasonKey);
            statusIcon.color = T.primary;
            UI.Toast(e.ReasonKey, FeedbackKind.Warning);
        }

        public void Tick(float dt)
        {
            pulse += dt * 3f;
            dot.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(pulse));
        }
    }

    /// <summary>AR setup, part 2: confirm placement, rotate in 15 degree steps, tap to move (FR-07, FR-09).</summary>
    public class PlacementView : View
    {
        protected override void Build()
        {
            var top = Card(Root, "Info");
            UIKit.Dock(top.rectTransform, true, 300, T.padding);
            UIKit.VStack(top.rectTransform, 10, 36);
            UIKit.Label(top.transform, "place.placed", T.bodySize * 1.1f, T.success, TextAlignmentOptions.Left, true);
            UIKit.Label(top.transform, "place.adjust", T.bodySize * 0.9f, T.textMuted, TextAlignmentOptions.Left);

            var bottom = UIKit.Rect(Root, "Controls");
            UIKit.Dock(bottom, false, T.buttonHeight * 2 + T.spacing, T.padding);
            UIKit.VStack(bottom, -1, 0);

            var row = UIKit.Rect(bottom, "Rotate");
            UIKit.HStack(row);
            UIKit.Size(row, T.buttonHeight);
            UIKit.Button(row, "place.rotate_left", ButtonStyle.Secondary, () => Act("rotate_left"), "rotate_left");
            UIKit.Button(row, "place.rotate_right", ButtonStyle.Secondary, () => Act("rotate_right"), "rotate_right");
            UIKit.Button(bottom, "place.confirm", ButtonStyle.Success, () => Act("confirm_placement"), "check");
        }
    }

    /// <summary>FR-10: Practice or Assessment, before every attempt.</summary>
    public class ModeView : View
    {
        protected override void Build()
        {
            var sheet = Card(Root, "Sheet");
            var rt = sheet.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(-2 * T.padding, 0);
            rt.anchoredPosition = new Vector2(0, T.padding);
            UIKit.VStack(rt);
            UIKit.Fit(sheet);

            UIKit.Label(sheet.transform, "mode.title", T.titleSize * 0.8f, T.text, TextAlignmentOptions.Center, true);
            ModeCard(sheet.transform, "mode.practice", "mode.practice_desc", ButtonStyle.Secondary, "mode_practice", "practice");
            ModeCard(sheet.transform, "mode.assess", "mode.assess_desc", ButtonStyle.Primary, "mode_assessment", "timer");

            var row = UIKit.Rect(sheet.transform, "Row");
            UIKit.HStack(row);
            UIKit.Size(row, T.buttonHeight * 0.85f);
            UIKit.Button(row, "mode.reposition", ButtonStyle.Ghost, () => Act("reposition"), "move", T.buttonHeight * 0.85f);
            UIKit.Button(row, "ui.main_menu", ButtonStyle.Ghost, () => Act("main_menu"), "home", T.buttonHeight * 0.85f);
        }

        private void ModeCard(Transform parent, string titleKey, string descKey, ButtonStyle style, string action, string icon)
        {
            var b = UIKit.Button(parent, titleKey, style, () => Act(action), icon, T.buttonHeight * 1.15f);
            UIKit.Label(parent, descKey, T.bodySize * 0.8f, T.textMuted);
        }
    }
}
