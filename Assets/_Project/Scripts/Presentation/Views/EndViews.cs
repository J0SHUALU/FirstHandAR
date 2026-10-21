using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Simulation;

namespace FirstHand.Presentation
{
    /// <summary>FR-22: paused by the trainee or by lost tracking. Resume needs tracking back.</summary>
    public class PauseView : View
    {
        private LocText reason;

        protected override void Build()
        {
            var scrim = UIKit.Panel(Root, "Scrim", T.scrim);
            scrim.sprite = null; scrim.type = Image.Type.Simple;
            UIKit.Stretch(scrim.rectTransform);

            var card = Card(Root, "Card");
            var rt = card.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(-2 * T.padding, 0);
            UIKit.VStack(rt);
            UIKit.Fit(card);
            UIKit.Icon(card.transform, "pause", 140, T.warning);
            UIKit.Label(card.transform, "pause.title", T.titleSize, T.text, TextAlignmentOptions.Center, true);
            reason = UIKit.Label(card.transform, "pause.manual", T.bodySize, T.textMuted).GetComponent<LocText>();
            UIKit.Button(card.transform, "pause.resume", ButtonStyle.Success, () => Act("resume"), "play");
            UIKit.Button(card.transform, "pause.quit", ButtonStyle.Danger, () =>
                UI.Modal.Confirm("pause.quit_title", "pause.quit_body", () => Act("quit")), "home");
        }

        public void SetReason(bool trackingLost) => reason.Set(trackingLost ? "pause.tracking" : "pause.manual");
    }

    /// <summary>FR-25: score, pass/fail in Assessment, step breakdown, one tip per failed step. Retry or menu.</summary>
    public class ResultsView : View
    {
        private RectTransform content;

        protected override void Build()
        {
            var bg = UIKit.Panel(Root, "Background", T.background);
            bg.sprite = null; bg.type = Image.Type.Simple;
            UIKit.Stretch(bg.rectTransform);

            var scrollArea = UIKit.Rect(Root, "ScrollArea");
            UIKit.Stretch(scrollArea, 0, 0, 0, T.buttonHeight + 2 * T.padding);
            content = UIKit.Scroll(scrollArea);

            var buttons = UIKit.Rect(Root, "Buttons");
            UIKit.Dock(buttons, false, T.buttonHeight, T.padding);
            UIKit.HStack(buttons);
            UIKit.Button(buttons, "result.retry", ButtonStyle.Primary, () => Act("retry"), "retry");
            UIKit.Button(buttons, "ui.main_menu", ButtonStyle.Secondary, () => Act("main_menu"), "home");
        }

        public void ShowRecord(SessionRecord r, bool saved)
        {
            UIKit.Clear(content);
            bool assessment = r.Mode == TrainingMode.Assessment;

            UIKit.Label(content, assessment ? "result.title_assess" : "result.title_practice", T.titleSize, T.text, TextAlignmentOptions.Center, true);

            // Score + badge
            var hero = Card(content, "Hero");
            UIKit.VStack(hero.rectTransform, 6);
            UIKit.Fit(hero);
            UIKit.Raw(hero.transform, r.score.ToString(), T.displaySize * 1.4f, T.text, TextAlignmentOptions.Center, true, true);
            UIKit.Label(hero.transform, "result.out_of", T.bodySize, T.textMuted);
            string badgeKey; Color badgeColor;
            if (!assessment) { badgeKey = "result.practice_done"; badgeColor = T.info; }
            else if (r.passed) { badgeKey = "result.pass_long"; badgeColor = T.success; }
            else { badgeKey = "result.not_yet_long"; badgeColor = T.warning; }
            var badge = UIKit.Panel(hero.transform, "Badge", badgeColor, T.pill);
            UIKit.Size(badge, 110);
            var bl = UIKit.Label(badge.transform, badgeKey, T.bodySize, Color.white, TextAlignmentOptions.Center, true);
            UIKit.Stretch(bl.rectTransform);

            // Key metrics
            int totalMistakes = r.steps.Sum(s => s.mistakes);
            int totalHints = r.steps.Sum(s => s.hintsUsed);
            var metrics = UIKit.Rect(content, "Metrics");
            UIKit.HStack(metrics, 16);
            UIKit.Size(metrics, 210);
            Metric(metrics, "result.time", FormatTime(r.durationSec));
            Metric(metrics, "result.mistakes", totalMistakes.ToString());
            Metric(metrics, assessment ? "result.branch" : "result.hints",
                   assessment ? LocalizationService.Get("branch." + r.branch) : totalHints.ToString());

            var comp = r.steps.FirstOrDefault(s => s.stepId == "compressions");
            if (comp != null)
            {
                var cm = UIKit.Rect(content, "Compression");
                UIKit.HStack(cm, 16);
                UIKit.Size(cm, 210);
                Metric(cm, "result.avg_rate", Mathf.RoundToInt(comp.averageRate).ToString());
                Metric(cm, "result.in_range", Mathf.RoundToInt(comp.percentInRange * 100) + "%");
                Metric(cm, "result.on_target", Mathf.RoundToInt(comp.percentOnTarget * 100) + "%");
            }

            // Steps
            var steps = Card(content, "Steps");
            UIKit.VStack(steps.rectTransform, 10, 28);
            UIKit.Fit(steps);
            UIKit.Label(steps.transform, "result.steps", T.bodySize * 1.05f, T.text, TextAlignmentOptions.Left, true);
            foreach (var s in r.steps)
            {
                var row = UIKit.Rect(steps.transform, "Row");
                var h = UIKit.HStack(row, 12);
                UIKit.Size(row, 90);
                var mark = UIKit.Label(row, s.passed ? "result.step_ok" : (s.timedOut ? "result.step_time" : "result.step_fail"),
                                       T.bodySize * 0.8f, s.passed ? T.success : T.primary, TextAlignmentOptions.Left, true);
                UIKit.Size(mark, -1, 230);
                var name = UIKit.Label(row, "step." + s.stepId, T.bodySize * 0.85f, T.text, TextAlignmentOptions.Left);
                UIKit.Size(name, -1, -1, 1);
                var detail = UIKit.Raw(row, $"{s.timeTaken:0}s" + (s.mistakes > 0 ? $"  x{s.mistakes}" : ""), T.bodySize * 0.8f, T.textMuted, TextAlignmentOptions.Right);
                UIKit.Size(detail, -1, 200);
            }

            // Tips: one line per failed step
            var tips = r.steps.Where(s => !s.passed && !string.IsNullOrEmpty(s.tipKey)).ToList();
            if (tips.Count > 0)
            {
                var tipCard = Card(content, "Tips", T.surfaceRaised);
                UIKit.VStack(tipCard.rectTransform, 12, 28);
                UIKit.Fit(tipCard);
                UIKit.Label(tipCard.transform, "result.tips", T.bodySize * 1.05f, T.warning, TextAlignmentOptions.Left, true);
                foreach (var s in tips) UIKit.Label(tipCard.transform, s.tipKey, T.bodySize * 0.9f, T.text, TextAlignmentOptions.Left);
            }

            if (assessment && r.passed) UIKit.Label(content, "result.next_course", T.bodySize * 0.9f, T.success);
            UIKit.Label(content, saved ? "result.saved" : "result.not_saved", T.bodySize * 0.75f, T.textMuted);
            Show();
        }

        private void Metric(Transform parent, string labelKey, string value)
        {
            var card = Card(parent, "Metric");
            UIKit.VStack(card.rectTransform, 4, 18, TextAnchor.MiddleCenter);
            UIKit.Raw(card.transform, value, T.titleSize, T.text, TextAlignmentOptions.Center, true, true);
            UIKit.Label(card.transform, labelKey, T.bodySize * 0.7f, T.textMuted);
        }

        private static string FormatTime(float seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalMinutes}:{t.Seconds:00}";
        }
    }

    /// <summary>
    /// FR-28 to FR-31: PIN-gated progress list, CSV export and trainee deletion. Closes itself after five
    /// minutes without a touch.
    /// </summary>
    public class FacilitatorView : View
    {
        public const float IdleSeconds = 300f;
        private RectTransform list;
        private float lastTouch;

        protected override void Build()
        {
            var bg = UIKit.Panel(Root, "Background", T.background);
            bg.sprite = null; bg.type = Image.Type.Simple;
            UIKit.Stretch(bg.rectTransform);

            var header = UIKit.Rect(Root, "Header");
            UIKit.Dock(header, true, 330, T.padding);
            UIKit.VStack(header, 18, 0);
            UIKit.Label(header, "fac.title", T.titleSize, T.text, TextAlignmentOptions.Left, true);
            var row = UIKit.Rect(header, "Actions");
            UIKit.HStack(row);
            UIKit.Size(row, T.buttonHeight);
            UIKit.Button(row, "fac.export", ButtonStyle.Success, Export, "export");
            UIKit.Button(row, "ui.close", ButtonStyle.Ghost, Hide, "close");

            var area = UIKit.Rect(Root, "ListArea");
            UIKit.Stretch(area, 0, 0, 330 + T.padding, 0);
            list = UIKit.Scroll(area, 14);
        }

        protected override void OnShow()
        {
            lastTouch = Time.unscaledTime;
            Refresh();
        }

        public void Touch() => lastTouch = Time.unscaledTime;

        public void Tick()
        {
            if (Visible && Time.unscaledTime - lastTouch > IdleSeconds)
            {
                Hide();
                UI.Toast("fac.timeout", FeedbackKind.Info);
            }
        }

        private void Refresh()
        {
            UIKit.Clear(list);
            var rows = DataManager.Instance.GetSummaries();
            if (rows.Count == 0) { UIKit.Label(list, "fac.empty", T.bodySize, T.textMuted); return; }

            foreach (var s in rows)
            {
                var summary = s;
                var card = Card(list, "Trainee");
                UIKit.VStack(card.rectTransform, 8, 28);
                UIKit.Fit(card);

                var top = UIKit.Rect(card.transform, "Top");
                UIKit.HStack(top, 12);
                UIKit.Size(top, 80);
                var name = UIKit.Raw(top, s.Profile.displayName, T.bodySize * 1.1f, T.text, TextAlignmentOptions.Left, true);
                UIKit.Size(name, -1, -1, 2);
                var badge = UIKit.Label(top, s.HasPassed ? "fac.ready" : "fac.not_ready", T.bodySize * 0.8f,
                                        s.HasPassed ? T.success : T.textMuted, TextAlignmentOptions.Right, true);
                UIKit.Size(badge, -1, -1, 1);

                string last = s.LastAttemptAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(s.LastAttemptAt).ToLocalTime().ToString("dd MMM yyyy") : "-";
                var stats = UIKit.Label(card.transform, "fac.stats", T.bodySize * 0.8f, T.textMuted, TextAlignmentOptions.Left);
                stats.GetComponent<LocText>().Set("fac.stats", s.Attempts, s.BestAssessmentScore < 0 ? "-" : s.BestAssessmentScore.ToString(), last);

                UIKit.Button(card.transform, "fac.delete", ButtonStyle.Danger, () =>
                    UI.Modal.Confirm("fac.delete_title", "fac.delete_body", () =>
                    {
                        DataManager.Instance.DeleteTrainee(summary.Profile.traineeId);
                        UI.Toast("fac.deleted", FeedbackKind.Info);
                        Refresh();
                    }, summary.Profile.displayName), "delete", 110);
            }
        }

        private void Export()
        {
            string path = DataManager.Instance.ExportCsv();
            GUIUtility.systemCopyBuffer = path;
            UI.Modal.Message("fac.exported_title", "fac.exported", null, path);
            NativeShare.ShareText(System.IO.File.ReadAllText(path), System.IO.Path.GetFileName(path));
        }
    }
}
