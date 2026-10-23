using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Presentation
{
    /// <summary>FR-22: paused by the trainee or by lost tracking. Resume needs tracking back.</summary>
    public class PauseView : View
    {
        private LocText reason;

        protected override void Build()
        {
            UIKit.Fill(Root, "Scrim", T.scrim);
            var c = UIKit.Sheet(Root, 300, out _);
            UIKit.VStack(c, 8);
            UIKit.Text(c, "pause.kicker", TextStyle.Label);
            UIKit.Text(c, "pause.title", TextStyle.H2);
            reason = UIKit.Text(c, "pause.manual", TextStyle.Body, T.ink2).GetComponent<LocText>();
            UIKit.Space(c, 10);
            UIKit.Button(c, "pause.resume", ButtonStyle.Primary, () => Act("resume"));
            UIKit.Button(c, "pause.quit", ButtonStyle.Secondary, () =>
                UI.Modal.Confirm("pause.quit_title", "pause.quit_body", () => Act("quit")), 56, false);
        }

        public void SetReason(bool trackingLost) => reason.Set(trackingLost ? "pause.tracking" : "pause.manual");
    }

    /// <summary>
    /// Concept 07 / FR-25: big score, pass state in Assessment, key metrics, step list, one tip per failed step.
    /// </summary>
    public class ResultsView : View
    {
        private RectTransform content;

        protected override void Build()
        {
            UIKit.Fill(Root, "Paper", T.paper);
            var area = UIKit.Rect(Root, "Area");
            UIKit.Stretch(area, 0, 0, 0, 104);
            content = UIKit.Scroll(area, 0, 24, 56, 20);

            var buttons = UIKit.Rect(Root, "Buttons");
            UIKit.Band(buttons, false, 24, 56);
            var h = UIKit.HStack(buttons, 12); h.childForceExpandHeight = true;
            var retry = UIKit.Button(buttons, "result.retry", ButtonStyle.Primary, () => Act("retry"));
            UIKit.Size(retry, 56, -1, 1.7f);
            var done = UIKit.Button(buttons, "result.done", ButtonStyle.Secondary, () => Act("main_menu"), 56, false);
            UIKit.Size(done, 56, -1, 1);
        }

        public void ShowRecord(SessionRecord r, bool saved)
        {
            UIKit.Clear(content);
            bool assessment = r.Mode == TrainingMode.Assessment;

            var top = UIKit.Rect(content, "Top"); UIKit.HStack(top, 0); UIKit.Size(top, 40);
            var date = DateTimeOffset.FromUnixTimeSeconds(r.startedAt).ToLocalTime();
            var kicker = UIKit.Raw(top, LocalizationService.Get(assessment ? "mode.assess" : "mode.practice") + "  ·  " + date.ToString("dd MMM  ·  HH:mm"), TextStyle.Label, T.muted, TextAlignmentOptions.MidlineLeft);
            UIKit.Size(kicker, -1, -1, 1);
            UIKit.IconButton(top, "close", () => Act("main_menu"), 40, T.card, T.ink, true);

            var scoreRow = UIKit.Rect(content, "Score"); UIKit.HStack(scoreRow, 6, 0, 0, TextAnchor.LowerLeft); UIKit.Size(scoreRow, 140);
            var big = UIKit.Raw(scoreRow, r.score.ToString(), TextStyle.Display, T.ink, TextAlignmentOptions.BottomLeft);
            big.textWrappingMode = TextWrappingModes.NoWrap;
            var of = UIKit.Raw(scoreRow, "/100", TextStyle.H2, T.muted, TextAlignmentOptions.BottomLeft, 26);
            UIKit.Size(of, 40, 80);
            of.margin = new Vector4(0, 0, 0, D(26)); // sit on the big number's baseline, not its descender

            Color bg, fg, badge; string key, icon;
            if (!assessment) { bg = T.paper2; fg = T.ink2; badge = T.ink2; key = "result.practice_done"; icon = "dot"; }
            else if (r.passed) { bg = T.tealTint; fg = T.tealDeep; badge = T.teal; key = "result.pass_long"; icon = "check"; }
            else { bg = T.amberTint; fg = T.amberDeep; badge = T.amber; key = "result.not_yet_long"; icon = "retry"; }
            var pillRow = UIKit.Rect(content, "PillRow"); UIKit.HStack(pillRow, 0); UIKit.Size(pillRow, 36);
            var pill = UIKit.Img(pillRow, "Pill", T.pill, bg);
            UIKit.HStack(pill.rectTransform, 8, 10, 0); UIKit.Fit(pill, false, true); UIKit.Size(pill, 36);
            UIKit.Badge(pill.transform, icon, badge, 18);
            UIKit.Text(pill.transform, key, TextStyle.BodyMedium, fg, TextAlignmentOptions.MidlineLeft, 14);

            UIKit.Space(content, 18);
            UIKit.Ecg(content, T.coral, 36);
            UIKit.Space(content, 14);

            int totalMistakes = r.steps.Sum(s => s.mistakes);
            var comp = r.steps.FirstOrDefault(s => s.stepId == "compressions");
            var metrics = UIKit.Rect(content, "Metrics");
            var mh = UIKit.HStack(metrics, 0); mh.childForceExpandWidth = true; mh.childControlWidth = true;
            UIKit.Size(metrics, 56);
            Metric(metrics, FormatTime(r.durationSec), "result.time", false);
            Metric(metrics, totalMistakes.ToString(), "result.mistakes", true);
            if (comp != null) Metric(metrics, Mathf.RoundToInt(comp.averageRate).ToString(), "result.avg_rate", true);
            else Metric(metrics, LocalizationService.Get("branch." + r.branch), "result.branch", true);

            UIKit.Space(content, 28);
            UIKit.Text(content, "result.steps", TextStyle.Label);
            UIKit.Space(content, 6);
            for (int i = 0; i < r.steps.Count; i++)
            {
                var s = r.steps[i];
                var row = UIKit.Rect(content, "Step"); UIKit.HStack(row, 12); UIKit.Size(row, 40);
                bool warn = s.passed && s.mistakes > 0;
                UIKit.Badge(row, s.passed ? (warn ? "dot" : "check") : "close", s.passed ? (warn ? T.amber : T.teal) : T.coral, 22);
                var n = UIKit.Text(row, "step." + s.stepId, TextStyle.Body, T.ink, TextAlignmentOptions.MidlineLeft); UIKit.Size(n, -1, -1, 1);
                string meta = !s.passed ? (s.timedOut ? LocalizationService.Get("result.step_time") : LocalizationService.Get("result.step_fail"))
                            : s.stepId == "compressions" && s.percentInRange > 0 ? LocalizationService.Format("result.in_range_meta", Mathf.RoundToInt(s.percentInRange * 100))
                            : s.mistakes > 0 ? LocalizationService.Format("result.mistakes_meta", s.mistakes)
                            : FormatTime(s.timeTaken);
                UIKit.Raw(row, meta, TextStyle.Label, !s.passed ? T.coralDeep : warn ? T.amberDeep : T.muted, TextAlignmentOptions.MidlineRight, 10);
                if (i < r.steps.Count - 1) { var line = UIKit.Img(content, "Line", null, T.line); UIKit.Size(line, 1); }
            }

            var tips = r.steps.Where(s => !s.passed && !string.IsNullOrEmpty(s.tipKey)).Select(s => s.tipKey).ToList();
            if (tips.Count == 0 && r.steps.Any(s => s.mistakes > 0)) tips.Add(r.steps.First(s => s.mistakes > 0).tipKey);
            if (tips.Count > 0)
            {
                UIKit.Space(content, 20);
                var card = UIKit.Img(content, "Tips", T.button16, T.amberTint);
                UIKit.VStack(card.rectTransform, 6, 16, 14); UIKit.Fit(card);
                UIKit.Text(card.transform, "result.tips", TextStyle.Label, T.amberDeep);
                foreach (var k in tips) UIKit.Text(card.transform, k, TextStyle.BodyMedium, T.ink);
            }
            if (assessment && r.passed) { UIKit.Space(content, 14); UIKit.Text(content, "result.next_course", TextStyle.Small, T.tealDeep); }
            UIKit.Space(content, 10);
            UIKit.Text(content, saved ? "result.saved" : "result.not_saved", TextStyle.Small, T.muted);
            Show();
        }

        private void Metric(Transform parent, string value, string labelKey, bool divider)
        {
            var cell = UIKit.Rect(parent, "Metric");
            UIKit.Size(cell, 56, -1, 1);
            if (divider) { var d = UIKit.Img(cell, "Divider", null, T.line); var r = d.rectTransform; r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.sizeDelta = new Vector2(D(1), 0); r.anchoredPosition = new Vector2(-D(8), 0); }
            var v = UIKit.Raw(cell, value, TextStyle.Number, T.ink, TextAlignmentOptions.TopLeft); UIKit.Place(v.rectTransform, 0, 0, 120, 32);
            var l = UIKit.Text(cell, labelKey, TextStyle.Label, T.muted, TextAlignmentOptions.TopLeft, 10); UIKit.Place(l.rectTransform, 0, 34, 120, 16);
        }

        public static string FormatTime(float seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalMinutes}:{t.Seconds:00}";
        }
    }

    /// <summary>Concept 08: score trend and the five most recent sessions of the signed-in trainee (FR-27).</summary>
    public class ProgressView : View
    {
        private RectTransform content;

        protected override void Build()
        {
            UIKit.Fill(Root, "Paper", T.paper);
            content = UIKit.Scroll(Root, 0, 24, 56, 28);
        }

        protected override void OnShow()
        {
            UIKit.Clear(content);
            var dm = DataManager.Instance;
            var trainee = dm.CurrentTrainee;
            var back = UIKit.Rect(content, "Back"); UIKit.HStack(back, 0); UIKit.Size(back, 40);
            UIKit.IconButton(back, "back", Hide, 40, T.card, T.ink, true);
            UIKit.Space(content, 40);
            UIKit.Text(content, "progress.kicker", TextStyle.Label);
            UIKit.Space(content, 8);
            if (trainee == null) { UIKit.Text(content, "start.no_sessions", TextStyle.Body, T.muted); return; }
            UIKit.Text(content, "progress.title", TextStyle.H1, T.ink, TextAlignmentOptions.Left, 30, trainee.displayName);
            UIKit.Space(content, 18);

            var sessions = dm.GetSessions(trainee.traineeId, 5);
            Chart(sessions.Where(s => s.IsComplete).Reverse().ToList());
            UIKit.Space(content, 26);
            UIKit.Text(content, "start.recent_mine", TextStyle.Label);
            UIKit.Space(content, 8);
            if (sessions.Count == 0) UIKit.Text(content, "start.no_sessions", TextStyle.Body, T.muted);
            foreach (var s in sessions) { SessionRow(content, s); UIKit.Space(content, 8); }
        }

        private void Chart(System.Collections.Generic.List<SessionRecord> list)
        {
            var card = UIKit.Card(content, "Chart", T.card, true);
            UIKit.Size(card, 168);
            var plot = UIKit.Rect(card.transform, "Plot"); UIKit.Stretch(plot, 44, 20, 24, 36);
            Func<float, float> y = v => Mathf.InverseLerp(0, 100, v);
            // pass mark line at 80
            for (int i = 0; i < 26; i++)
            {
                var dash = UIKit.Img(plot, "Pass", null, T.muted);
                var r = dash.rectTransform; r.anchorMin = r.anchorMax = new Vector2(i / 26f, y(80)); r.pivot = new Vector2(0, 0.5f);
                r.sizeDelta = new Vector2(D(5), D(1));
            }
            var lab = UIKit.Raw(card.transform, "80", TextStyle.Mono, T.muted, TextAlignmentOptions.MidlineLeft, 10);
            var lr = lab.rectTransform; lr.anchorMin = lr.anchorMax = new Vector2(0, 0); lr.pivot = new Vector2(0, 0.5f);
            lr.sizeDelta = new Vector2(D(30), D(14)); lr.anchoredPosition = new Vector2(D(12), D(36) + y(80) * (D(168) - D(60)));
            if (list.Count == 0) return;
            Vector2 prev = Vector2.zero;
            for (int i = 0; i < list.Count; i++)
            {
                float px = list.Count == 1 ? 0.5f : (float)i / (list.Count - 1);
                var p = new Vector2(px, y(list[i].score));
                if (i > 0) Segment(plot, prev, p);
                var dot = UIKit.Img(plot, "Point", T.circle, i == list.Count - 1 ? T.coral : T.card);
                var r = dot.rectTransform; r.anchorMin = r.anchorMax = p; r.sizeDelta = new Vector2(D(11), D(11));
                if (i != list.Count - 1) { var o = UIKit.Img(dot.transform, "Ring", T.circleLine, T.coral); UIKit.Stretch(o.rectTransform); }
                var d = UIKit.Raw(plot, DateTimeOffset.FromUnixTimeSeconds(list[i].startedAt).ToLocalTime().ToString("dd"), TextStyle.Mono, T.muted, TextAlignmentOptions.Center, 10);
                var dr = d.rectTransform; dr.anchorMin = dr.anchorMax = new Vector2(px, 0); dr.pivot = new Vector2(0.5f, 1);
                dr.sizeDelta = new Vector2(D(30), D(14)); dr.anchoredPosition = new Vector2(0, -D(10));
                prev = p;
            }
        }

        /// <summary>A line between two normalised points, drawn as a rotated thin image.</summary>
        private void Segment(RectTransform plot, Vector2 a, Vector2 b)
        {
            var seg = UIKit.Img(plot, "Seg", null, T.coral);
            var r = seg.rectTransform;
            r.anchorMin = r.anchorMax = a; r.pivot = new Vector2(0, 0.5f);
            var size = new Vector2(D(390 - 48 - 64), D(168 - 60)); // plot area in the card (concept px)
            var delta = new Vector2((b.x - a.x) * size.x, (b.y - a.y) * size.y);
            r.sizeDelta = new Vector2(delta.magnitude, D(2.5f));
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            r.SetAsFirstSibling();
        }

        public static void SessionRow(Transform parent, SessionRecord s)
        {
            var T = UIKit.Theme;
            var row = UIKit.Card(parent, "Session", T.card);
            UIKit.Size(row, 52);
            UIKit.HStack(row.rectTransform, 10, 14, 0);
            var date = UIKit.Raw(row.transform, DateTimeOffset.FromUnixTimeSeconds(s.startedAt).ToLocalTime().ToString("dd MMM"), TextStyle.Label, T.ink2, TextAlignmentOptions.MidlineLeft, 10);
            UIKit.Size(date, 20, 52);
            bool assess = s.Mode == TrainingMode.Assessment;
            var tag = UIKit.Img(row.transform, "Mode", T.pill, assess ? T.coralTint : T.tealTint);
            UIKit.Size(tag, 22, assess ? 92 : 74);
            var tl = UIKit.Text(tag.transform, assess ? "mode.assess" : "mode.practice", TextStyle.Label, assess ? T.coralDeep : T.tealDeep, TextAlignmentOptions.Center, 9);
            UIKit.Stretch(tl.rectTransform);
            var score = UIKit.Raw(row.transform, s.IsComplete ? s.score.ToString() : "-", TextStyle.Title, T.ink, TextAlignmentOptions.MidlineRight, 18);
            UIKit.Size(score, 24, 40, 1);
            string key; Color c, tc;
            if (!s.IsComplete) { key = "result.incomplete"; c = T.muted; tc = T.muted; }
            else if (!assess) { key = "result.practice"; c = T.muted; tc = T.muted; }
            else if (s.passed) { key = "result.pass"; c = T.teal; tc = T.tealDeep; }
            else { key = "result.not_yet"; c = T.amber; tc = T.amberDeep; }
            var dot = UIKit.Img(row.transform, "Dot", T.circle, c); UIKit.Size(dot, 8, 8);
            var st = UIKit.Text(row.transform, key, TextStyle.Label, tc, TextAlignmentOptions.MidlineLeft, 9);
            UIKit.Size(st, 20, 76);
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
            UIKit.Fill(Root, "Paper", T.paper);
            list = UIKit.Scroll(Root, 10, 24, 56, 28);
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
            var top = UIKit.Rect(list, "Top"); UIKit.HStack(top, 0); UIKit.Size(top, 40);
            UIKit.IconButton(top, "back", Hide, 40, T.card, T.ink, true);
            var sp = UIKit.Rect(top, "Sp"); UIKit.Size(sp, 1, -1, 1);
            var exp = UIKit.Img(top, "Export", T.pill, T.ink, true);
            UIKit.HStack(exp.rectTransform, 8, 14, 0); UIKit.Fit(exp, false, true); UIKit.Size(exp, 40);
            UIKit.Icon(exp.transform, "export", 16, Color.white);
            UIKit.Text(exp.transform, "fac.export", TextStyle.Title, Color.white, TextAlignmentOptions.MidlineLeft, 14);
            exp.gameObject.AddComponent<Button>().onClick.AddListener(Export);

            UIKit.Space(list, 30);
            UIKit.Text(list, "fac.kicker", TextStyle.Label);
            UIKit.Text(list, "fac.title", TextStyle.H1, T.ink, TextAlignmentOptions.Left, 30);
            UIKit.Space(list, 14);

            var rows = DataManager.Instance.GetSummaries();
            if (rows.Count == 0) { UIKit.Text(list, "fac.empty", TextStyle.Body, T.muted); return; }
            foreach (var s in rows)
            {
                var summary = s;
                var card = UIKit.Card(list, "Trainee", T.card, true);
                UIKit.VStack(card.rectTransform, 6, 16, 14); UIKit.Fit(card);
                var head = UIKit.Rect(card.transform, "Head"); UIKit.HStack(head, 8); UIKit.Size(head, 26);
                var name = UIKit.Raw(head, s.Profile.displayName, TextStyle.Title, T.ink, TextAlignmentOptions.MidlineLeft, 17); UIKit.Size(name, -1, -1, 1);
                var badge = UIKit.Img(head, "Badge", T.pill, s.HasPassed ? T.tealTint : T.paper2);
                UIKit.Fit(badge, false, true); UIKit.HStack(badge.rectTransform, 0, 10, 0); UIKit.Size(badge, 24);
                UIKit.Text(badge.transform, s.HasPassed ? "fac.ready" : "fac.not_ready", TextStyle.Label, s.HasPassed ? T.tealDeep : T.muted, TextAlignmentOptions.Center, 9);
                string last = s.LastAttemptAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(s.LastAttemptAt).ToLocalTime().ToString("dd MMM") : "-";
                UIKit.Text(card.transform, "fac.stats", TextStyle.Label, T.muted, TextAlignmentOptions.Left, 10, s.Attempts, s.BestAssessmentScore < 0 ? "-" : s.BestAssessmentScore.ToString(), last);
                var del = UIKit.Rect(card.transform, "Delete"); UIKit.HStack(del, 6); UIKit.Size(del, 28);
                UIKit.Icon(del, "delete", 16, T.coralDeep);
                UIKit.Text(del, "fac.delete", TextStyle.BodyMedium, T.coralDeep, TextAlignmentOptions.MidlineLeft, 14);
                del.gameObject.AddComponent<Image>().color = Color.clear;
                del.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    UI.Modal.Confirm("fac.delete_title", "fac.delete_body", () =>
                    {
                        DataManager.Instance.DeleteTrainee(summary.Profile.traineeId);
                        UI.Toast("fac.deleted", FeedbackKind.Info);
                        Refresh();
                    }, summary.Profile.displayName));
            }
        }

        private void Export()
        {
            string path = DataManager.Instance.ExportCsv();
            GUIUtility.systemCopyBuffer = path;
            UI.Modal.Message("fac.exported_title", "fac.exported", null, System.IO.Path.GetFileName(path));
            NativeShare.ShareText(System.IO.File.ReadAllText(path), System.IO.Path.GetFileName(path));
        }
    }
}
