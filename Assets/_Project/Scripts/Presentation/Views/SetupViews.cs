using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;

namespace FirstHand.Presentation
{
    public class ScanView : View
    {
        private LocText chipText, title, body;
        private Image chipDot, reticle, fill;
        private TextMeshProUGUI sizeText;
        private float pulse;

        protected override void Build()
        {
            chipText = StatusChip(Root, "scan.chip", T.teal, out chipDot).GetComponent<LocText>();
            var close = UIKit.IconButton(Root, "close", () => Act("back"), 40);
            UIKit.Place(close.GetComponent<RectTransform>(), 326, 56, 40, 40);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(1, 1); cr.anchoredPosition = new Vector2(-D(24), -D(56));

            reticle = UIKit.Img(Root, "Reticle", T.circleLine, Color.white);
            reticle.rectTransform.sizeDelta = new Vector2(D(56), D(56));
            var dot = UIKit.Img(reticle.transform, "Dot", T.circle, Color.white); dot.rectTransform.sizeDelta = new Vector2(D(8), D(8));

            var c = UIKit.Sheet(Root, 232, out _);
            UIKit.VStack(c, 6);
            UIKit.Text(c, "scan.kicker", TextStyle.Label);
            title = UIKit.Text(c, "scan.title", TextStyle.H2).GetComponent<LocText>();
            body = UIKit.Text(c, "scan.looking", TextStyle.Body, T.ink2).GetComponent<LocText>();
            UIKit.Space(c, 14);
            var row = UIKit.Rect(c, "Row"); UIKit.HStack(row, 0); UIKit.Size(row, 20);
            var l = UIKit.Text(row, "scan.floor_found", TextStyle.BodyMedium, T.ink, TextAlignmentOptions.MidlineLeft, 13); UIKit.Size(l, -1, -1, 1);
            sizeText = UIKit.Raw(row, "", TextStyle.Mono, T.muted, TextAlignmentOptions.MidlineRight, 11);
            UIKit.Bar(c, 6, out fill, T.teal);
            UIKit.Text(c, "scan.needs", TextStyle.Small, T.muted);
        }

        protected override void OnShow()
        {
            SetState(0, false, false);
            EventBus.Subscribe<PlaneStatusEvent>(OnPlanes);
            EventBus.Subscribe<PlacementRejectedEvent>(OnRejected);
        }

        protected override void OnHide()
        {
            EventBus.Unsubscribe<PlaneStatusEvent>(OnPlanes);
            EventBus.Unsubscribe<PlacementRejectedEvent>(OnRejected);
        }

        private void OnPlanes(PlaneStatusEvent e) => SetState(e.PlaneCount, e.LargestBigEnough, e.ScanTimedOut);

        private void SetState(int planes, bool bigEnough, bool timedOut)
        {
            if (bigEnough) { chipText.Set("scan.chip_ready"); body.Set("scan.found"); chipDot.color = T.teal; UIKit.SetBar(fill, 1f); fill.color = T.teal; }
            else if (planes > 0) { chipText.Set("scan.chip"); body.Set("scan.keep_going"); chipDot.color = T.amber; UIKit.SetBar(fill, 0.6f); fill.color = T.amber; }
            else if (timedOut) { chipText.Set("scan.chip"); body.Set("scan.help"); chipDot.color = T.amber; UIKit.SetBar(fill, 0.1f); }
            else { chipText.Set("scan.chip"); body.Set("scan.looking"); chipDot.color = T.teal; UIKit.SetBar(fill, 0.15f); fill.color = T.teal; }
            sizeText.text = bigEnough ? "OK" : planes > 0 ? "..." : "";
        }

        private void OnRejected(PlacementRejectedEvent e)
        {
            body.Set(e.ReasonKey);
            UI.Toast(e.ReasonKey, FeedbackKind.Warning);
        }

        public void Tick(float dt)
        {
            pulse += dt * 3f;
            reticle.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(pulse));
        }
    }

    public class PlacementView : View
    {
        protected override void Build()
        {
            StatusChip(Root, "place.chip", T.teal, out _);
            var close = UIKit.IconButton(Root, "close", () => Act("back"), 40);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = cr.anchorMax = new Vector2(1, 1); cr.pivot = new Vector2(1, 1); cr.anchoredPosition = new Vector2(-D(24), -D(56));

            var c = UIKit.Sheet(Root, 192, out _);
            UIKit.VStack(c, 6);
            UIKit.Text(c, "place.kicker", TextStyle.Label, T.tealDeep);
            UIKit.Text(c, "place.placed", TextStyle.H2);
            UIKit.Text(c, "place.adjust", TextStyle.Small, T.ink2, TextAlignmentOptions.Left, 14);
            UIKit.Space(c, 10);
            var row = UIKit.Rect(c, "Row");
            var h = UIKit.HStack(row, 8); UIKit.Size(row, 56);
            UIKit.IconButton(row, "rotate_left", () => Act("rotate_left"), 52, T.paper2);
            UIKit.IconButton(row, "rotate_right", () => Act("rotate_right"), 52, T.paper2);
            var place = UIKit.Button(row, "place.confirm", ButtonStyle.Primary, () => Act("confirm_placement"));
            UIKit.Size(place, 56, -1, 1);
        }
    }

    public class ModeView : View
    {
        private bool assessment = true;
        private Image practiceCard, assessCard, practiceSel, assessSel;
        private Image practiceRadio, assessRadio;
        private LocText beginLabel;

        protected override void Build()
        {
            UIKit.Fill(Root, "Scrim", T.scrim);
            var c = UIKit.Sheet(Root, 528, out _);
            UIKit.VStack(c, 6);
            UIKit.Text(c, "mode.kicker", TextStyle.Label);
            UIKit.Text(c, "mode.title", TextStyle.H2);
            UIKit.Space(c, 16);
            (practiceCard, practiceSel, practiceRadio) = Option(c, "mode.practice", "mode.practice_desc", "mode.practice_meta", T.tealTint, "hint", T.tealDeep, false);
            UIKit.Space(c, 6);
            (assessCard, assessSel, assessRadio) = Option(c, "mode.assess", "mode.assess_desc", "mode.assess_meta", T.coralTint, "timer", T.coralDeep, true);
            UIKit.Space(c, 14);
            var begin = UIKit.Button(c, "mode.begin_assess", ButtonStyle.Primary, () => Act(assessment ? "mode_assessment" : "mode_practice"));
            beginLabel = begin.GetComponentInChildren<LocText>();
            var row = UIKit.Rect(c, "Row"); var h = UIKit.HStack(row, 0, 0, 0, TextAnchor.MiddleCenter); h.childForceExpandWidth = true; UIKit.Size(row, 44);
            UIKit.Button(row, "mode.reposition", ButtonStyle.Ghost, () => Act("reposition"), 44, false).GetComponentInChildren<TextMeshProUGUI>().color = T.muted;
            UIKit.Button(row, "ui.main_menu", ButtonStyle.Ghost, () => Act("main_menu"), 44, false).GetComponentInChildren<TextMeshProUGUI>().color = T.muted;
            Select(true);
        }

        private (Image card, Image selOutline, Image radio) Option(Transform parent, string title, string desc, string meta, Color tint, string icon, Color iconColor, bool isAssess)
        {
            var card = UIKit.Card(parent, "Option", T.card, true);
            UIKit.Size(card, 112);
            var sel = UIKit.Img(card.transform, "Selected", T.card20Line, T.coral); UIKit.Stretch(sel.rectTransform);
            var bubble = UIKit.Img(card.transform, "Bubble", T.circle, tint);
            UIKit.Place(bubble.rectTransform, 16, 18, 40, 40);
            var ic = UIKit.Img(bubble.transform, "Icon", T.Icon(icon), iconColor); ic.preserveAspect = true; ic.rectTransform.sizeDelta = new Vector2(D(20), D(20));
            var t = UIKit.Text(card.transform, title, TextStyle.Title, T.ink, TextAlignmentOptions.Left, 17); UIKit.Place(t.rectTransform, 68, 18, 220, 24);
            var d = UIKit.Text(card.transform, desc, TextStyle.Small, T.ink2); UIKit.Place(d.rectTransform, 68, 42, 250, 22);
            var m = UIKit.Text(card.transform, meta, TextStyle.Label, T.muted, TextAlignmentOptions.Left, 10); UIKit.Place(m.rectTransform, 68, 78, 250, 18);
            var radio = UIKit.Img(card.transform, "Radio", T.circleLine, T.line);
            var rr = radio.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(1, 1); rr.pivot = new Vector2(1, 1); rr.sizeDelta = new Vector2(D(22), D(22)); rr.anchoredPosition = new Vector2(-D(16), -D(19));
            var check = UIKit.Img(radio.transform, "Check", T.Icon("check"), Color.white); check.rectTransform.sizeDelta = new Vector2(D(14), D(14));
            card.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(isAssess));
            return (card, sel, radio);
        }

        private void Select(bool assess)
        {
            assessment = assess;
            assessSel.enabled = assess; practiceSel.enabled = !assess;
            assessCard.color = assess ? UITheme.Hex("FFF7F7") : T.card;
            practiceCard.color = !assess ? UITheme.Hex("F4FBFA") : T.card;
            practiceSel.color = T.teal;
            Radio(assessRadio, assess, T.coral);
            Radio(practiceRadio, !assess, T.teal);
            beginLabel?.Set(assess ? "mode.begin_assess" : "mode.begin_practice");
        }

        private void Radio(Image r, bool on, Color c)
        {
            r.sprite = on ? T.circle : T.circleLine;
            r.color = on ? c : T.line;
            r.transform.GetChild(0).gameObject.SetActive(on);
        }

        protected override void OnShow() => Select(assessment);
    }
}
