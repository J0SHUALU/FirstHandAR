using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Entities;

namespace FirstHand.Presentation
{
    /// <summary>
    /// In-attempt interface (concepts 05, 06): step card with progress and timer, a dark instruction card,
    /// and the control each step needs (dangers counter, shout, dialer, info chooser, hold ring, rate panel).
    /// It only reacts to events and publishes UI actions.
    /// </summary>
    public class HudView : View
    {
        // step card
        private TextMeshProUGUI stepNo, timerLabel, mistakesLabel;
        private LocText stepTitle;
        private Image timerChip;
        private readonly List<Image> segments = new List<Image>();
        // instruction
        private LocText instruction;
        private RectTransform instructionCard;

        /// <summary>Distance from the top of the screen to the bottom of the instruction card, in canvas units.</summary>
        public float InstructionBottom => instructionCard == null ? UIKit.D(250) : -instructionCard.anchoredPosition.y + instructionCard.rect.height;
        private GameObject hintButton;
        // widgets
        private GameObject hazardsBox, shoutBox, callBox, holdBox, meterBox, keypad, infoChooser;
        private TextMeshProUGUI hazardsLabel, dialDisplay;
        private readonly List<Image> hazardDots = new List<Image>();
        private readonly Dictionary<string, (Image bg, TextMeshProUGUI label)> infoButtons = new Dictionary<string, (Image, TextMeshProUGUI)>();
        // meter
        private TextMeshProUGUI rateNumber, countLabel;
        private Image ringFill, needle, feedbackChip, feedbackBadge;
        private LocText feedbackText;
        private RectTransform gauge;

        private string dialled = "";
        private int mistakes, total = 5;
        private string currentStep;
        private const float MeterMin = 60f, MeterMax = 160f;

        protected override void Build()
        {
            BuildStepCard();
            BuildInstruction();
            BuildHazards();
            BuildShout();
            BuildCall();
            BuildHold();
            BuildMeter();
        }

        // ---------- Top ----------

        private void BuildStepCard()
        {
            var card = UIKit.Card(Root, "StepCard");
            UIKit.Band(card.rectTransform, true, 54, 84);
            stepNo = UIKit.Raw(card.transform, "01/05", TextStyle.Mono, T.coral, TextAlignmentOptions.Left, 13);
            stepNo.font = T.monoMedium;
            UIKit.Place(stepNo.rectTransform, 18, 14, 120, 18);
            stepTitle = UIKit.Text(card.transform, "step.hazards", TextStyle.Title, T.ink, TextAlignmentOptions.Left, 18).GetComponent<LocText>();
            UIKit.Place(stepTitle.GetComponent<RectTransform>(), 18, 32, 220, 26);

            var right = UIKit.Rect(card.transform, "Right");
            right.anchorMin = right.anchorMax = new Vector2(1, 1); right.pivot = new Vector2(1, 1);
            right.anchoredPosition = new Vector2(-D(12), -D(12)); right.sizeDelta = new Vector2(D(140), D(40));
            var h = UIKit.HStack(right, 8, 0, 0, TextAnchor.MiddleRight);
            mistakesLabel = UIKit.Raw(right, "", TextStyle.Mono, T.amberDeep, TextAlignmentOptions.MidlineRight, 11);
            timerChip = UIKit.Img(right, "Timer", T.chip12, T.paper2);
            UIKit.Size(timerChip, 34, 64);
            timerLabel = UIKit.Raw(timerChip.transform, "", TextStyle.Mono, T.ink, TextAlignmentOptions.Center, 14);
            timerLabel.font = T.monoMedium;
            UIKit.Stretch(timerLabel.rectTransform);
            UIKit.IconButton(right, "pause", () => Act("pause"), 36, T.paper2);

            var segRow = UIKit.Rect(card.transform, "Segments");
            segRow.anchorMin = new Vector2(0, 0); segRow.anchorMax = new Vector2(1, 0); segRow.pivot = new Vector2(0.5f, 0);
            segRow.sizeDelta = new Vector2(-D(36), D(4)); segRow.anchoredPosition = new Vector2(0, D(14));
            var sh = UIKit.HStack(segRow, 6); sh.childForceExpandWidth = true; sh.childControlWidth = true; sh.childForceExpandHeight = true;
            for (int i = 0; i < 6; i++) { var s = UIKit.Img(segRow, "Seg", T.pill, T.paper2); UIKit.Size(s, 4, -1, 1); segments.Add(s); }
        }

        private void BuildInstruction()
        {
            var card = UIKit.Img(Root, "Instruction", T.card20, T.ink, true);
            var rt = card.rectTransform;
            instructionCard = rt;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-D(48), 0); rt.anchoredPosition = new Vector2(0, -D(150));
            var h = UIKit.HStack(rt, 14, 14, 14, TextAnchor.UpperLeft);
            UIKit.Fit(card);
            var speaker = UIKit.Img(rt, "Speaker", T.circle, T.coral);
            UIKit.Size(speaker, 32, 32);
            var ic = UIKit.Img(speaker.transform, "Icon", T.Icon("speaker"), Color.white); ic.preserveAspect = true; ic.rectTransform.sizeDelta = new Vector2(D(18), D(18));
            var col = UIKit.Rect(rt, "Col");
            UIKit.VStack(col, 10);
            UIKit.Size(col, -1, -1, 1);
            instruction = UIKit.Text(col, "", TextStyle.BodyMedium, Color.white, TextAlignmentOptions.Left, 14).GetComponent<LocText>();
            var hint = UIKit.Img(col, "Hint", T.pill, new Color(1, 1, 1, 0.14f), true);
            var hh = UIKit.HStack(hint.rectTransform, 6, 12, 0);
            UIKit.Fit(hint, false, true);
            UIKit.Size(hint, 30);
            UIKit.Icon(hint.transform, "hint", 14, Color.white);
            UIKit.Text(hint.transform, "hud.hint", TextStyle.Title, Color.white, TextAlignmentOptions.MidlineLeft, 13);
            hint.gameObject.AddComponent<Button>().onClick.AddListener(() => Act("hint"));
            var hintRow = UIKit.Rect(col, "HintRow"); // keeps the pill left-aligned and its own width
            hint.transform.SetParent(hintRow, false);
            UIKit.HStack(hintRow, 0); UIKit.Size(hintRow, 30);
            hintButton = hintRow.gameObject;
        }

        private RectTransform BottomBand(string name, float height, float offset = 24)
        {
            var rt = UIKit.Rect(Root, name);
            UIKit.Band(rt, false, offset, height);
            return rt;
        }

        // ---------- Step widgets ----------

        private void BuildHazards()
        {
            var card = UIKit.Card(BottomBand("Hazards", 64), "Card");
            UIKit.Stretch(card.rectTransform);
            var h = UIKit.HStack(card.rectTransform, 10, 18, 0);
            hazardsLabel = UIKit.Raw(card.transform, "", TextStyle.Title, T.ink, TextAlignmentOptions.MidlineLeft, 16);
            UIKit.Size(hazardsLabel, -1, -1, 1);
            for (int i = 0; i < 2; i++) { var d = UIKit.Img(card.transform, "Dot", T.circleLine, T.line); UIKit.Size(d, 22, 22); hazardDots.Add(d); }
            hazardsBox = card.transform.parent.gameObject;
        }

        private void BuildShout()
        {
            var band = BottomBand("Shout", 56);
            var b = UIKit.Button(band, "hud.shout", ButtonStyle.Primary, () => Act("shout"));
            UIKit.Stretch(b.GetComponent<RectTransform>());
            shoutBox = band.gameObject;
        }

        private void BuildCall()
        {
            var band = BottomBand("Call", 56);
            var b = UIKit.Button(band, "hud.open_phone", ButtonStyle.Teal, OpenKeypad);
            UIKit.Stretch(b.GetComponent<RectTransform>());
            callBox = band.gameObject;

            // dialer sheet
            var holder = UIKit.Rect(Root, "Keypad"); UIKit.Stretch(holder);
            var c = UIKit.Sheet(holder, 440, out _);
            UIKit.VStack(c, 0, 0, 0, TextAnchor.UpperCenter);
            dialDisplay = UIKit.Raw(c, "", TextStyle.H1, T.ink, TextAlignmentOptions.Center, 44);
            UIKit.Size(dialDisplay, 52);
            UIKit.Text(c, "call.emergency", TextStyle.Label, T.muted, TextAlignmentOptions.Center, 10);
            UIKit.Space(c, 12);
            var grid = UIKit.Rect(c, "Grid");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(D(54), D(54)); g.spacing = new Vector2(D(42), D(8));
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3; g.childAlignment = TextAnchor.UpperCenter;
            UIKit.Size(grid, 4 * 54 + 3 * 8);
            foreach (var k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "", "0", "del" })
            {
                string key = k;
                Image cell;
                if (key == "") { cell = UIKit.Img(grid, "Empty", null, Color.clear); continue; }
                if (key == "del") { cell = UIKit.Img(grid, "Del", null, Color.clear, true); var ic = UIKit.Img(cell.transform, "Icon", T.Icon("backspace"), T.ink); ic.preserveAspect = true; ic.rectTransform.sizeDelta = new Vector2(D(26), D(26)); }
                else { cell = UIKit.Img(grid, "Btn_digit." + key, T.circle, T.paper2, true); var t = UIKit.Raw(cell.transform, key, TextStyle.H2, T.ink, TextAlignmentOptions.Center, 26); UIKit.Stretch(t.rectTransform); }
                var btn = cell.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (key == "del") { dialled = ""; Act("dial_clear"); }
                    else { if (dialled.Length < 6) dialled += key; Act("dial", key); }
                    dialDisplay.text = dialled;
                });
            }
            UIKit.Space(c, 12);
            var call = UIKit.Button(c, "call.call", ButtonStyle.Teal, () => Act("call"), 56, false);
            call.name = "Btn_call.call";
            keypad = holder.gameObject;
            keypad.SetActive(false);

            // info chooser sheet
            var holder2 = UIKit.Rect(Root, "InfoChooser"); UIKit.Stretch(holder2);
            var c2 = UIKit.Sheet(holder2, 540, out _);
            UIKit.VStack(c2, 8);
            UIKit.Text(c2, "call.dispatcher_label", TextStyle.Label, T.tealDeep);
            UIKit.Text(c2, "call.dispatcher", TextStyle.H2);
            UIKit.Text(c2, "call.pick_three", TextStyle.Small, T.ink2);
            UIKit.Space(c2, 4);
            foreach (var id in InfoIds)
            {
                string info = id;
                var bg = UIKit.Img(c2, "Btn_info." + info, T.button16, T.paper2, true);
                UIKit.Size(bg, 44);
                var label = UIKit.Text(bg.transform, "info." + info, TextStyle.BodyMedium, T.ink, TextAlignmentOptions.MidlineLeft, 15);
                UIKit.Stretch(label.rectTransform, 16, 16, 0, 0);
                bg.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    Act("info", info);
                    var (b2, l2) = infoButtons[info];
                    bool on = b2.color != T.ink;
                    b2.color = on ? T.ink : T.paper2;
                    l2.color = on ? Color.white : T.ink;
                });
                infoButtons[info] = (bg, label);
            }
            UIKit.Space(c2, 4);
            var send = UIKit.Button(c2, "call.send", ButtonStyle.Teal, () => Act("send_info"));
            send.name = "Btn_call.send";
            infoChooser = holder2.gameObject;
            infoChooser.SetActive(false);
        }

        private static readonly string[] InfoIds = { "location", "casualty_name", "what_happened", "your_shift", "breathing", "weather" };

        private void BuildHold()
        {
            var holder = UIKit.Rect(Root, "Hold");
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0); holder.pivot = new Vector2(0.5f, 0);
            holder.sizeDelta = new Vector2(D(176), D(176)); holder.anchoredPosition = new Vector2(0, D(40));
            var bg = UIKit.Img(holder, "Card", T.circle, T.card, true); UIKit.Stretch(bg.rectTransform);
            var track = UIKit.Img(holder, "Track", T.ring, T.paper2); UIKit.Stretch(track.rectTransform, 10, 10, 10, 10);
            var fill = UIKit.Img(holder, "Fill", T.ring, T.teal);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillOrigin = (int)Image.Origin360.Top; fill.fillAmount = 0;
            UIKit.Stretch(fill.rectTransform, 10, 10, 10, 10);
            var col = UIKit.Rect(holder, "Col"); UIKit.Stretch(col, 30, 30, 40, 40);
            UIKit.VStack(col, 0, 0, 0, TextAnchor.MiddleCenter);
            var counter = UIKit.Raw(col, "", TextStyle.MonoLarge, T.ink, TextAlignmentOptions.Center, 40);
            UIKit.Text(col, "hud.hold", TextStyle.Label, T.muted, TextAlignmentOptions.Center, 10);
            var hb = bg.gameObject.AddComponent<HoldButton>();
            hb.fill = fill; hb.counter = counter;
            hb.goodColor = T.teal; hb.earlyColor = T.ink2; hb.lateColor = T.coral;
            holdBox = holder.gameObject;
        }

        private void BuildMeter()
        {
            var band = BottomBand("Meter", 280);
            var card = UIKit.Card(band, "Card"); UIKit.Stretch(card.rectTransform);
            var t = card.transform;
            var lbl = UIKit.Text(t, "meter.rate", TextStyle.Label, T.muted, TextAlignmentOptions.Left, 10); UIKit.Place(lbl.rectTransform, 20, 26, 120, 16);
            rateNumber = UIKit.Raw(t, "--", TextStyle.MonoLarge, T.ink, TextAlignmentOptions.BottomLeft, 62);
            UIKit.Place(rateNumber.rectTransform, 18, 40, 130, 72);
            var per = UIKit.Text(t, "meter.per_min", TextStyle.Body, T.muted, TextAlignmentOptions.BottomLeft); UIKit.Place(per.rectTransform, 140, 80, 80, 26);

            var ring = UIKit.Rect(t, "Ring"); ring.anchorMin = ring.anchorMax = new Vector2(1, 1); ring.pivot = new Vector2(1, 1);
            ring.sizeDelta = new Vector2(D(88), D(88)); ring.anchoredPosition = new Vector2(-D(20), -D(24));
            var tr = UIKit.Img(ring, "Track", T.ring, T.paper2); UIKit.Stretch(tr.rectTransform);
            ringFill = UIKit.Img(ring, "Fill", T.ring, T.teal);
            ringFill.type = Image.Type.Filled; ringFill.fillMethod = Image.FillMethod.Radial360; ringFill.fillOrigin = (int)Image.Origin360.Top; ringFill.fillClockwise = true;
            UIKit.Stretch(ringFill.rectTransform);
            countLabel = UIKit.Raw(ring, "0", TextStyle.H2, T.ink, TextAlignmentOptions.Center, 22); UIKit.Stretch(countLabel.rectTransform, 0, 0, 0, 12);
            var of = UIKit.Raw(ring, "/30", TextStyle.Mono, T.muted, TextAlignmentOptions.Center, 10); UIKit.Stretch(of.rectTransform, 0, 0, 44, 0);

            gauge = UIKit.Rect(t, "Gauge");
            gauge.anchorMin = new Vector2(0, 1); gauge.anchorMax = new Vector2(1, 1); gauge.pivot = new Vector2(0.5f, 1);
            gauge.sizeDelta = new Vector2(-D(40), D(10)); gauge.anchoredPosition = new Vector2(0, -D(150));
            var track = UIKit.Img(gauge, "Track", T.pill, T.paper2); UIKit.Stretch(track.rectTransform);
            var zone = UIKit.Img(gauge, "Zone", null, new Color(T.teal.r, T.teal.g, T.teal.b, 0.45f));
            float a = (100 - MeterMin) / (MeterMax - MeterMin), b = (120 - MeterMin) / (MeterMax - MeterMin);
            zone.rectTransform.anchorMin = new Vector2(a, 0); zone.rectTransform.anchorMax = new Vector2(b, 1); zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
            needle = UIKit.Img(gauge, "Needle", T.pill, T.ink);
            needle.rectTransform.anchorMin = new Vector2(0, -0.7f); needle.rectTransform.anchorMax = new Vector2(0, 1.7f); needle.rectTransform.sizeDelta = new Vector2(D(3), 0);
            foreach (var (s, v) in new[] { ("60", 0f), ("100", a), ("120", b), ("160", 1f) })
            {
                var tick = UIKit.Raw(gauge, s, TextStyle.Mono, T.muted, TextAlignmentOptions.Center, 10);
                var r = tick.rectTransform; r.anchorMin = r.anchorMax = new Vector2(v, 0); r.pivot = new Vector2(0.5f, 1);
                r.sizeDelta = new Vector2(D(40), D(16)); r.anchoredPosition = new Vector2(0, -D(12));
            }

            feedbackChip = UIKit.Img(t, "Feedback", T.chip12, T.tealTint);
            var fr = feedbackChip.rectTransform; fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(1, 0); fr.pivot = new Vector2(0.5f, 0);
            fr.sizeDelta = new Vector2(-D(40), D(46)); fr.anchoredPosition = new Vector2(0, D(20));
            UIKit.HStack(fr, 10, 14, 0);
            feedbackBadge = UIKit.Badge(fr, "check", T.teal, 20);
            feedbackText = UIKit.Text(fr, "meter.start", TextStyle.BodyMedium, T.tealDeep, TextAlignmentOptions.MidlineLeft, 14).GetComponent<LocText>();
            meterBox = band.gameObject;
        }

        // ---------- Show / events ----------

        protected override void OnShow()
        {
            EventBus.Subscribe<StepStartedEvent>(OnStepStarted);
            EventBus.Subscribe<StepFeedbackEvent>(OnFeedback);
            EventBus.Subscribe<StepTimerEvent>(OnTimer);
            EventBus.Subscribe<RateUpdatedEvent>(OnRate);
            EventBus.Subscribe<HazardFoundEvent>(OnHazard);
            EventBus.Subscribe<HintShownEvent>(OnHint);
            EventBus.Subscribe<UiActionEvent>(OnAction);
            EventBus.Subscribe<TapInputEvent>(OnTap);
        }

        protected override void OnHide()
        {
            EventBus.Unsubscribe<StepStartedEvent>(OnStepStarted);
            EventBus.Unsubscribe<StepFeedbackEvent>(OnFeedback);
            EventBus.Unsubscribe<StepTimerEvent>(OnTimer);
            EventBus.Unsubscribe<RateUpdatedEvent>(OnRate);
            EventBus.Unsubscribe<HazardFoundEvent>(OnHazard);
            EventBus.Unsubscribe<HintShownEvent>(OnHint);
            EventBus.Unsubscribe<UiActionEvent>(OnAction);
            EventBus.Unsubscribe<TapInputEvent>(OnTap);
        }

        /// <summary>Called when a new attempt begins (not when resuming from pause).</summary>
        public void ResetAttempt()
        {
            mistakes = 0;
            mistakesLabel.text = "";
            timerLabel.text = "";
            timerChip.gameObject.SetActive(false);
        }

        private void OnStepStarted(StepStartedEvent e)
        {
            currentStep = e.StepId;
            total = e.Total;
            stepNo.text = $"{e.Index + 1:00}/{e.Total:00}";
            stepTitle.Set("step." + e.StepId);
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].gameObject.SetActive(i < e.Total);
                segments[i].color = i <= e.Index ? T.coral : T.paper2;
            }
            instruction.Set(e.InstructionKey);
            hintButton.SetActive(e.ShowHint);
            timerLabel.text = "";
            timerChip.gameObject.SetActive(false);

            hazardsBox.SetActive(e.StepId == "hazards");
            shoutBox.SetActive(e.StepId == "response");
            callBox.SetActive(e.StepId == "call");
            holdBox.SetActive(e.StepId == "breathing");
            meterBox.SetActive(e.StepId == "compressions");
            keypad.SetActive(false);
            infoChooser.SetActive(false);

            if (e.StepId == "hazards") SetHazards(0, 2);
            if (e.StepId == "call") { dialled = ""; dialDisplay.text = ""; ResetInfoButtons(); }
            if (e.StepId == "compressions") ResetMeter();
        }

        private void OnFeedback(StepFeedbackEvent e)
        {
            if (e.Kind == FeedbackKind.Mistake)
            {
                mistakes++;
                mistakesLabel.text = LocalizationService.Format("hud.mistakes_short", mistakes);
            }
            switch (e.MessageKey)
            {
                case "fb.call_connected":
                    keypad.SetActive(false); callBox.SetActive(false); infoChooser.SetActive(true);
                    EntityManager.Instance?.Phone?.SetCallActive(true);
                    break;
                case "fb.wrong_number": dialled = ""; dialDisplay.text = ""; break;
                case "fb.help_coming": infoChooser.SetActive(false); EntityManager.Instance?.Phone?.SetCallActive(false); break;
                case "fb.next_round": ResetMeter(); break;
            }
            if (currentStep == "compressions" && (e.MessageKey == "fb.rhythm_off" || e.MessageKey == "fb.hands_off_target"))
                SetFeedback(e.MessageKey, T.amberTint, T.amber, T.amberDeep, null);
            UI.Toast(e.MessageKey, e.Kind);
        }

        private void OnTimer(StepTimerEvent e)
        {
            float r = Mathf.Max(0, e.Remaining);
            timerChip.gameObject.SetActive(true);
            int s = Mathf.CeilToInt(r);
            timerLabel.text = $"{s / 60}:{s % 60:00}";
            timerLabel.color = r < 5 ? T.coral : T.ink;
        }

        private void SetHazards(int found, int needed)
        {
            hazardsLabel.text = LocalizationService.Format("hud.hazards", found, needed);
            for (int i = 0; i < hazardDots.Count; i++)
            {
                bool on = i < found;
                hazardDots[i].sprite = on ? T.circle : T.circleLine;
                hazardDots[i].color = on ? T.teal : T.line;
                hazardDots[i].gameObject.SetActive(i < needed);
            }
        }

        private void OnHazard(HazardFoundEvent e) => SetHazards(e.Found, e.Needed);
        private void OnHint(HintShownEvent e) => UI.Toast(e.HintKey, FeedbackKind.Info, 4f);

        private void OnAction(UiActionEvent e)
        {
            if (e.Action == "open_phone" && currentStep == "call") OpenKeypad();
        }

        /// <summary>Pooled ripple where each compression landed.</summary>
        private void OnTap(TapInputEvent e)
        {
            if (currentStep != "compressions" || !e.HitInteractable || e.TargetId == null || !e.TargetId.StartsWith("casualty")) return;
            var cas = EntityManager.Instance?.Casualty;
            if (cas == null) return;
            bool good = Vector3.Distance(e.WorldPoint, cas.ChestTarget) <= 0.06f;
            EntityManager.Instance.SpawnTapMarker(e.WorldPoint, good);
        }

        private void OnRate(RateUpdatedEvent e)
        {
            countLabel.text = e.Count.ToString();
            ringFill.fillAmount = (float)e.Count / Mathf.Max(1, e.Target);
            if (e.Band == RateBand.None) return;
            rateNumber.text = Mathf.RoundToInt(e.Rate).ToString();
            float t = Mathf.InverseLerp(MeterMin, MeterMax, e.Rate);
            needle.rectTransform.anchorMin = new Vector2(t, -0.7f); needle.rectTransform.anchorMax = new Vector2(t, 1.7f);
            needle.rectTransform.anchoredPosition = Vector2.zero;
            switch (e.Band)   // words, not only colour (NFR-05)
            {
                case RateBand.TooSlow: SetFeedback("meter.slow", T.amberTint, T.amber, T.amberDeep, null); rateNumber.color = T.amberDeep; break;
                case RateBand.TooFast: SetFeedback("meter.fast", T.coralTint, T.coral, T.coralDeep, null); rateNumber.color = T.coralDeep; break;
                default: SetFeedback("meter.good", T.tealTint, T.teal, T.tealDeep, "check"); rateNumber.color = T.ink; break;
            }
        }

        private void SetFeedback(string key, Color bg, Color badge, Color text, string icon)
        {
            feedbackChip.color = bg;
            feedbackBadge.color = badge;
            var ic = feedbackBadge.transform.GetChild(0).GetComponent<Image>();
            ic.sprite = T.Icon(icon ?? "dot");
            ic.rectTransform.sizeDelta = icon == null ? new Vector2(D(8), D(8)) : new Vector2(D(12), D(12));
            feedbackText.Set(key);
            feedbackText.GetComponent<TextMeshProUGUI>().color = text;
        }

        private void ResetMeter()
        {
            rateNumber.text = "--";
            rateNumber.color = T.ink;
            countLabel.text = "0";
            ringFill.fillAmount = 0;
            needle.rectTransform.anchorMin = new Vector2(0, -0.7f); needle.rectTransform.anchorMax = new Vector2(0, 1.7f);
            SetFeedback("meter.start", T.paper2, T.ink2, T.ink2, null);
        }

        private void ResetInfoButtons()
        {
            foreach (var (bg, label) in infoButtons.Values) { bg.color = T.paper2; label.color = T.ink; }
        }

        private void OpenKeypad()
        {
            if (infoChooser.activeSelf) return;
            keypad.SetActive(true);
            keypad.transform.SetAsLastSibling();
        }
    }
}
