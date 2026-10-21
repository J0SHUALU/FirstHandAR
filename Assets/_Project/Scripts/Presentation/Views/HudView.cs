using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Entities;

namespace FirstHand.Presentation
{
    /// <summary>
    /// In-attempt interface: current task, progress, timer, mistakes, captioned instructions, feedback, and
    /// the controls each step needs (shout, phone keypad, hold-to-check, rate meter). It only reacts to events.
    /// </summary>
    public class HudView : View
    {
        // top bar
        private LocText stepLabel;
        private Image progressFill;
        private TextMeshProUGUI timerLabel, mistakesLabel;
        // instruction
        private LocText instruction;
        private Image instructionIcon;
        private Button hintButton;
        // step widgets
        private GameObject hazardsBox, shoutBox, callBox, holdBox, meterBox;
        private TextMeshProUGUI hazardsLabel;
        private GameObject keypad, infoChooser;
        private TextMeshProUGUI dialDisplay;
        private TextMeshProUGUI rateNumber, countLabel;
        private LocText rateWord;
        private RectTransform needle;
        private Image meterBand;
        private readonly Dictionary<string, Image> infoButtons = new Dictionary<string, Image>();
        private string dialled = "";

        private int mistakes;
        private string currentStep;

        private const float MeterMin = 60f, MeterMax = 160f;

        protected override void Build()
        {
            BuildTopBar();
            BuildInstruction();
            BuildStepWidgets();
        }

        // ---------- Build ----------

        private void BuildTopBar()
        {
            var bar = Card(Root, "TopBar");
            UIKit.Dock(bar.rectTransform, true, 230, T.padding * 0.6f);
            var v = UIKit.VStack(bar.rectTransform, 14, 28);

            var row = UIKit.Rect(bar.transform, "Row");
            var h = UIKit.HStack(row, 20);
            h.childForceExpandWidth = false;
            UIKit.Size(row, 100);
            var step = UIKit.Label(row, "hud.step", T.bodySize, T.text, TextAlignmentOptions.Left, true);
            UIKit.Size(step, -1, -1, 1);
            stepLabel = step.GetComponent<LocText>();
            mistakesLabel = UIKit.Raw(row, "", T.bodySize * 0.85f, T.warning, TextAlignmentOptions.Right);
            UIKit.Size(mistakesLabel, -1, 170);
            timerLabel = UIKit.Raw(row, "", T.bodySize, T.text, TextAlignmentOptions.Right, true);
            UIKit.Size(timerLabel, -1, 150);
            var pause = UIKit.Button(row, "hud.pause", ButtonStyle.Ghost, () => Act("pause"), "pause", 100);
            UIKit.Size(pause, 100, 200);

            UIKit.Bar(bar.transform, 22, out progressFill, T.success);
        }

        private void BuildInstruction()
        {
            var card = Card(Root, "Instruction");
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(-2 * T.padding * 0.6f, 0);
            rt.anchoredPosition = new Vector2(0, T.padding * 0.6f);
            UIKit.VStack(rt, 16, 32);
            UIKit.Fit(card);

            var row = UIKit.Rect(card.transform, "Row");
            var h = UIKit.HStack(row, 24);
            h.childForceExpandWidth = false;
            instructionIcon = UIKit.Icon(row, "info", 110, T.info);
            var text = UIKit.Label(row, "", T.bodySize * 1.05f, T.text, TextAlignmentOptions.Left, true);
            UIKit.Size(text, -1, -1, 1);
            instruction = text.GetComponent<LocText>();
            var fit = row.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = 130;

            hintButton = UIKit.Button(card.transform, "hud.hint", ButtonStyle.Ghost, () => Act("hint"), "hint", 110);
        }

        private RectTransform ControlArea()
        {
            var area = UIKit.Rect(Root, "Controls");
            area.anchorMin = new Vector2(0, 0); area.anchorMax = new Vector2(1, 0);
            area.pivot = new Vector2(0.5f, 0);
            area.sizeDelta = new Vector2(-2 * T.padding, 520);
            area.anchoredPosition = new Vector2(0, 520);
            return area;
        }

        private void BuildStepWidgets()
        {
            var area = ControlArea();

            // Hazards counter
            var hz = Card(area, "Hazards", T.surface);
            UIKit.Dock(hz.rectTransform, false, 130);
            hazardsLabel = UIKit.Raw(hz.transform, "", T.bodySize, T.text, TextAlignmentOptions.Center, true);
            UIKit.Stretch(hazardsLabel.rectTransform);
            hazardsBox = hz.gameObject;

            // Shout
            var shout = UIKit.Rect(area, "Shout");
            UIKit.Dock(shout, false, T.buttonHeight * 1.2f);
            UIKit.HStack(shout);
            UIKit.Button(shout, "hud.shout", ButtonStyle.Primary, () => Act("shout"), "shout", T.buttonHeight * 1.2f);
            shoutBox = shout.gameObject;

            // Call: open phone button, keypad, info chooser
            var call = UIKit.Rect(area, "Call");
            UIKit.Dock(call, false, T.buttonHeight * 1.2f);
            UIKit.HStack(call);
            UIKit.Button(call, "hud.open_phone", ButtonStyle.Success, OpenKeypad, "phone", T.buttonHeight * 1.2f);
            callBox = call.gameObject;
            BuildKeypad();
            BuildInfoChooser();

            // Hold to check breathing
            var hold = UIKit.Rect(area, "Hold");
            hold.anchorMin = new Vector2(0.5f, 0); hold.anchorMax = new Vector2(0.5f, 0);
            hold.pivot = new Vector2(0.5f, 0);
            hold.sizeDelta = new Vector2(420, 420);
            var ringBg = UIKit.Panel(hold, "Ring", T.surface, T.circle);
            UIKit.Stretch(ringBg.rectTransform);
            var fill = UIKit.Panel(hold, "Fill", T.success, T.circle);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360; fill.fillAmount = 0;
            fill.raycastTarget = false;
            UIKit.Stretch(fill.rectTransform);
            var inner = UIKit.Panel(hold, "Inner", T.surfaceRaised, T.circle);
            UIKit.Stretch(inner.rectTransform, 40, 40, 40, 40);
            var innerV = UIKit.VStack(inner.rectTransform, 4, 20, TextAnchor.MiddleCenter);
            UIKit.Label(inner.transform, "hud.hold", T.bodySize * 0.8f, T.text, TextAlignmentOptions.Center, true);
            var counter = UIKit.Raw(inner.transform, "", T.titleSize, T.text, TextAlignmentOptions.Center, true);
            var hb = inner.gameObject.AddComponent<HoldButton>();
            hb.fill = fill; hb.counter = counter;
            hb.goodColor = T.success; hb.earlyColor = T.info; hb.lateColor = T.primary;
            holdBox = hold.gameObject;

            BuildMeter(area);
        }

        private void BuildKeypad()
        {
            var pad = Card(Root, "Keypad", T.surface);
            var rt = pad.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-2 * T.padding, 0);
            UIKit.VStack(rt, 20);
            UIKit.Fit(pad);

            UIKit.Label(pad.transform, "call.title", T.bodySize, T.textMuted);
            dialDisplay = UIKit.Raw(pad.transform, "", T.titleSize * 1.2f, T.text, TextAlignmentOptions.Center, true);
            UIKit.Size(dialDisplay, 130);

            var grid = UIKit.Rect(pad.transform, "Grid");
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(260, 140); g.spacing = new Vector2(20, 20);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
            g.childAlignment = TextAnchor.MiddleCenter;
            UIKit.Size(grid, 4 * 140 + 3 * 20);
            foreach (var k in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "clear", "0", "call" })
            {
                string key = k;
                var style = key == "call" ? ButtonStyle.Success : key == "clear" ? ButtonStyle.Ghost : ButtonStyle.Secondary;
                var b = UIKit.Button(grid, key == "call" ? "call.call" : key == "clear" ? "call.clear" : "digit." + key, style, () =>
                {
                    if (key == "call") Act("call");
                    else if (key == "clear") { dialled = ""; Act("dial_clear"); }
                    else { if (dialled.Length < 6) dialled += key; Act("dial", key); }
                    dialDisplay.text = dialled;
                }, key == "call" ? "phone" : null);
                Object.Destroy(b.GetComponent<LayoutElement>());
            }
            UIKit.Button(pad.transform, "ui.close", ButtonStyle.Ghost, () => keypad.SetActive(false), null, 110);
            keypad = pad.gameObject;
            keypad.SetActive(false);
        }

        private static readonly string[] InfoIds =
            { "location", "casualty_name", "what_happened", "your_shift", "breathing", "weather" };

        private void BuildInfoChooser()
        {
            var card = Card(Root, "InfoChooser", T.surface);
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-2 * T.padding, 0);
            UIKit.VStack(rt, 18);
            UIKit.Fit(card);
            UIKit.Label(card.transform, "call.dispatcher", T.bodySize, T.text, TextAlignmentOptions.Center, true);
            UIKit.Label(card.transform, "call.pick_three", T.bodySize * 0.85f, T.textMuted);
            foreach (var id in InfoIds)
            {
                string info = id;
                var b = UIKit.Button(card.transform, "info." + info, ButtonStyle.Secondary, () =>
                {
                    Act("info", info);
                    var img = infoButtons[info];
                    img.color = img.color == UIKit.StyleColor(ButtonStyle.Secondary) ? T.info : UIKit.StyleColor(ButtonStyle.Secondary);
                }, null, 120);
                infoButtons[info] = b.GetComponent<Image>();
            }
            UIKit.Button(card.transform, "call.send", ButtonStyle.Success, () => Act("send_info"), "check");
            infoChooser = card.gameObject;
            infoChooser.SetActive(false);
        }

        private void BuildMeter(RectTransform area)
        {
            var card = Card(area, "Meter", T.surface);
            UIKit.Dock(card.rectTransform, false, 420);
            UIKit.VStack(card.rectTransform, 12, 28);

            var row = UIKit.Rect(card.transform, "Numbers");
            UIKit.HStack(row, 10);
            UIKit.Size(row, 170);
            rateNumber = UIKit.Raw(row, "--", T.displaySize, T.text, TextAlignmentOptions.Left, true, true);
            var right = UIKit.Rect(row, "Right");
            UIKit.VStack(right, 4, 0, TextAnchor.MiddleRight);
            UIKit.Label(right, "meter.per_min", T.bodySize * 0.75f, T.textMuted, TextAlignmentOptions.Right);
            rateWord = UIKit.Label(right, "meter.start", T.bodySize * 1.1f, T.text, TextAlignmentOptions.Right, true).GetComponent<LocText>();

            // Bar 60..160 per minute with the 100..120 target zone in green and a needle for the current rate.
            var barBg = UIKit.Panel(card.transform, "Scale", new Color(1, 1, 1, 0.1f), T.pill);
            UIKit.Size(barBg, 60);
            var zone = UIKit.Panel(barBg.transform, "Zone", T.success, T.pill);
            zone.raycastTarget = false;
            float a = (100 - MeterMin) / (MeterMax - MeterMin), b = (120 - MeterMin) / (MeterMax - MeterMin);
            zone.rectTransform.anchorMin = new Vector2(a, 0); zone.rectTransform.anchorMax = new Vector2(b, 1);
            zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
            meterBand = zone;
            var n = UIKit.Panel(barBg.transform, "Needle", Color.white);
            n.sprite = null; n.type = Image.Type.Simple; n.raycastTarget = false;
            needle = n.rectTransform;
            needle.anchorMin = new Vector2(0, -0.3f); needle.anchorMax = new Vector2(0, 1.3f);
            needle.sizeDelta = new Vector2(10, 0);

            countLabel = UIKit.Raw(card.transform, "", T.bodySize, T.text, TextAlignmentOptions.Center, true);
            meterBox = card.gameObject;
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
        }

        private void OnStepStarted(StepStartedEvent e)
        {
            currentStep = e.StepId;
            stepLabel.Set("hud.step", e.Index + 1, e.Total);
            UIKit.SetBar(progressFill, (float)e.Index / e.Total);
            instruction.Set(e.InstructionKey);
            instructionIcon.sprite = T.Icon("step_" + e.StepId);
            instructionIcon.enabled = instructionIcon.sprite != null;
            hintButton.gameObject.SetActive(e.ShowHint);
            timerLabel.text = "";

            hazardsBox.SetActive(e.StepId == "hazards");
            shoutBox.SetActive(e.StepId == "response");
            callBox.SetActive(e.StepId == "call");
            holdBox.SetActive(e.StepId == "breathing");
            meterBox.SetActive(e.StepId == "compressions");
            keypad.SetActive(false);
            infoChooser.SetActive(false);

            if (e.StepId == "hazards") hazardsLabel.text = LocalizationService.Format("hud.hazards", 0, 2);
            if (e.StepId == "call") { dialled = ""; dialDisplay.text = ""; ResetInfoButtons(); }
            if (e.StepId == "compressions") ResetMeter();
        }

        private void OnFeedback(StepFeedbackEvent e)
        {
            if (e.Kind == FeedbackKind.Mistake)
            {
                mistakes++;
                mistakesLabel.text = LocalizationService.Format("hud.mistakes", mistakes);
            }
            if (e.MessageKey == "fb.call_connected")
            {
                keypad.SetActive(false);
                callBox.SetActive(false);
                infoChooser.SetActive(true);
                EntityManager.Instance?.Phone?.SetCallActive(true);
            }
            if (e.MessageKey == "fb.wrong_number") { dialled = ""; dialDisplay.text = ""; }
            if (e.MessageKey == "fb.help_coming") { infoChooser.SetActive(false); EntityManager.Instance?.Phone?.SetCallActive(false); }
            if (e.MessageKey == "fb.next_round") ResetMeter();
            UI.Toast(e.MessageKey, e.Kind);
        }

        private void OnTimer(StepTimerEvent e)
        {
            float r = Mathf.Max(0, e.Remaining);
            timerLabel.text = Mathf.CeilToInt(r) + "s";
            timerLabel.color = r < 5 ? T.primary : T.text;
        }

        private void OnHazard(HazardFoundEvent e) => hazardsLabel.text = LocalizationService.Format("hud.hazards", e.Found, e.Needed);
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
            countLabel.text = LocalizationService.Format("meter.count", e.Count, e.Target);
            if (e.Band == RateBand.None) return;
            rateNumber.text = Mathf.RoundToInt(e.Rate).ToString();
            string key; Color c;
            switch (e.Band)
            {
                case RateBand.TooSlow: key = "meter.slow"; c = T.warning; break;
                case RateBand.TooFast: key = "meter.fast"; c = T.primary; break;
                default: key = "meter.good"; c = T.success; break;
            }
            rateWord.Set(key);                         // words, not only colour (NFR-05)
            rateWord.GetComponent<TextMeshProUGUI>().color = c;
            rateNumber.color = c;
            float t = Mathf.InverseLerp(MeterMin, MeterMax, e.Rate);
            needle.anchorMin = new Vector2(t, -0.3f); needle.anchorMax = new Vector2(t, 1.3f);
            needle.anchoredPosition = Vector2.zero;
        }

        private void ResetMeter()
        {
            rateNumber.text = "--";
            rateNumber.color = T.text;
            rateWord.Set("meter.start");
            rateWord.GetComponent<TextMeshProUGUI>().color = T.text;
            countLabel.text = LocalizationService.Format("meter.count", 0, 30);
            needle.anchorMin = new Vector2(0, -0.3f); needle.anchorMax = new Vector2(0, 1.3f);
        }

        private void ResetInfoButtons()
        {
            foreach (var img in infoButtons.Values) img.color = UIKit.StyleColor(ButtonStyle.Secondary);
        }

        private void OpenKeypad()
        {
            if (infoChooser.activeSelf) return;
            keypad.SetActive(true);
            keypad.transform.SetAsLastSibling();
        }
    }
}
