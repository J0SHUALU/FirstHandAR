using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Data;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Start screen: title, how it works, who is training (sign in / new trainee), Start, the five most recent
    /// sessions, and language / settings / facilitator entry.
    /// </summary>
    public class StartView : View
    {
        private RectTransform account;
        private RectTransform recentList;
        private TextMeshProUGUI recentTitle;

        protected override void Build()
        {
            var bg = UIKit.Panel(Root, "Background", T.background);
            bg.sprite = null; bg.type = Image.Type.Simple;
            UIKit.Stretch(bg.rectTransform);

            var content = UIKit.Scroll(Root);

            // Header
            var logo = UIKit.Rect(content, "Logo");
            var logoImg = logo.gameObject.AddComponent<Image>();
            logoImg.sprite = T.logo; logoImg.preserveAspect = true; logoImg.enabled = T.logo != null;
            UIKit.Size(logoImg, T.logo != null ? 260 : 0);
            UIKit.Label(content, "app.title", T.titleSize * 1.3f, T.text, TextAlignmentOptions.Center, true, true);
            UIKit.Label(content, "start.tagline", T.bodySize, T.textMuted);

            // How it works (Start screen instructions)
            var how = Card(content, "HowItWorks");
            UIKit.VStack(how.rectTransform, 18);
            UIKit.Fit(how);
            UIKit.Label(how.transform, "start.how_title", T.bodySize * 1.1f, T.text, TextAlignmentOptions.Left, true);
            foreach (var k in new[] { "start.how1", "start.how2", "start.how3", "start.how4" })
                UIKit.Label(how.transform, k, T.bodySize * 0.9f, T.textMuted, TextAlignmentOptions.Left);

            // Account area (rebuilt whenever the screen shows)
            account = UIKit.Rect(content, "Account");
            UIKit.VStack(account, -1, 0);
            UIKit.Fit(account);

            // Recent sessions
            var recent = Card(content, "Recent");
            UIKit.VStack(recent.rectTransform, 12);
            UIKit.Fit(recent);
            recentTitle = UIKit.Label(recent.transform, "start.recent", T.bodySize * 1.1f, T.text, TextAlignmentOptions.Left, true);
            recentList = UIKit.Rect(recent.transform, "List");
            UIKit.VStack(recentList, 10, 0);
            UIKit.Fit(recentList);

            // Footer: language, settings, facilitator
            var footer = UIKit.Rect(content, "Footer");
            UIKit.HStack(footer, 18);
            UIKit.Size(footer, T.buttonHeight * 0.85f);
            UIKit.Button(footer, "start.lang_en", ButtonStyle.Ghost, () => UI.SetLanguage(Language.English), null, T.buttonHeight * 0.85f);
            UIKit.Button(footer, "start.lang_pcm", ButtonStyle.Ghost, () => UI.SetLanguage(Language.Pidgin), null, T.buttonHeight * 0.85f);
            UIKit.Button(footer, "start.settings", ButtonStyle.Ghost, UI.OpenSettings, "settings", T.buttonHeight * 0.85f);
            UIKit.Button(content, "start.facilitator", ButtonStyle.Ghost, UI.OpenFacilitator, "lock", T.buttonHeight * 0.8f);
        }

        protected override void OnShow() => Refresh();

        public void Refresh()
        {
            BuildAccount();
            BuildRecent();
        }

        private void BuildAccount()
        {
            UIKit.Clear(account);
            var dm = DataManager.Instance;
            var trainee = dm.CurrentTrainee;

            if (trainee != null)
            {
                var hello = UIKit.Label(account, "start.hello", T.titleSize * 0.8f, T.text, TextAlignmentOptions.Center, true);
                hello.GetComponent<LocText>().Set("start.hello", trainee.displayName);
                UIKit.Button(account, "start.begin", ButtonStyle.Primary, () => Act("start"), "play", T.buttonHeight * 1.2f);
                UIKit.Button(account, "start.sign_out", ButtonStyle.Ghost, () => { dm.SignOut(); Refresh(); });
                return;
            }

            var card = Card(account, "WhoCard");
            UIKit.VStack(card.rectTransform, 16);
            UIKit.Fit(card);
            UIKit.Label(card.transform, "start.who", T.bodySize * 1.1f, T.text, TextAlignmentOptions.Left, true);
            if (dm.Profiles.Count == 0) UIKit.Label(card.transform, "start.no_profiles", T.bodySize * 0.9f, T.textMuted, TextAlignmentOptions.Left);
            foreach (var p in dm.Profiles)
            {
                var profile = p;
                var b = UIKit.Button(card.transform, null, ButtonStyle.Secondary, () => SignIn(profile), "person");
                b.GetComponentInChildren<TextMeshProUGUI>().text = profile.displayName;
                UnityEngine.Object.DestroyImmediate(b.GetComponentInChildren<LocText>());
            }
            UIKit.Button(account, "start.new_trainee", ButtonStyle.Primary, Register, "add");
        }

        private void BuildRecent()
        {
            UIKit.Clear(recentList);
            var dm = DataManager.Instance;
            var trainee = dm.CurrentTrainee;
            var sessions = trainee != null ? dm.GetSessions(trainee.traineeId, 5) : dm.GetRecentSessionsAllTrainees(5);
            recentTitle.GetComponent<LocText>().Set(trainee != null ? "start.recent_mine" : "start.recent");

            if (sessions.Count == 0)
            {
                UIKit.Label(recentList, "start.no_sessions", T.bodySize * 0.85f, T.textMuted, TextAlignmentOptions.Left);
                return;
            }
            foreach (var s in sessions) SessionRow(recentList, s, trainee == null ? dm.DisplayNameFor(s.traineeId) : null);
        }

        public static void SessionRow(Transform parent, SessionRecord s, string who)
        {
            var T = UIKit.Theme;
            var row = UIKit.Panel(parent, "Row", T.surfaceRaised);
            UIKit.Size(row, 120);
            var h = UIKit.HStack(row.rectTransform, 16, 0);
            h.padding = new RectOffset(28, 28, 8, 8);

            var date = DateTimeOffset.FromUnixTimeSeconds(s.startedAt).ToLocalTime().ToString("dd MMM HH:mm");
            var left = UIKit.Raw(row.transform, (who != null ? who + "  " : "") + date, T.bodySize * 0.75f, T.textMuted, TextAlignmentOptions.Left);
            UIKit.Size(left, -1, -1, 2);
            var mode = UIKit.Label(row.transform, s.Mode == TrainingMode.Assessment ? "mode.assess" : "mode.practice", T.bodySize * 0.75f, T.text, TextAlignmentOptions.Center);
            UIKit.Size(mode, -1, -1, 1.2f);
            var score = UIKit.Raw(row.transform, s.IsComplete ? s.score.ToString() : "-", T.bodySize, T.text, TextAlignmentOptions.Center, true);
            UIKit.Size(score, -1, -1, 0.6f);

            string badgeKey; Color badgeColor;
            if (!s.IsComplete) { badgeKey = "result.incomplete"; badgeColor = T.textMuted; }
            else if (s.Mode == TrainingMode.Practice) { badgeKey = "result.practice"; badgeColor = T.info; }
            else if (s.passed) { badgeKey = "result.pass"; badgeColor = T.success; }
            else { badgeKey = "result.not_yet"; badgeColor = T.warning; }
            var badge = UIKit.Label(row.transform, badgeKey, T.bodySize * 0.7f, badgeColor, TextAlignmentOptions.Right, true);
            UIKit.Size(badge, -1, -1, 1.3f);
        }

        // ---------- Sign in / register ----------

        private void SignIn(TraineeProfile p)
        {
            UI.Modal.PinPad("pin.enter", pin =>
            {
                switch (DataManager.Instance.SignIn(p.traineeId, pin, out int secs))
                {
                    case SignInResult.Ok:
                        UI.ApplyTraineeLanguage();
                        Refresh();
                        break;
                    case SignInResult.WrongPin:
                        UI.Toast("pin.wrong", Core.FeedbackKind.Mistake);
                        break;
                    case SignInResult.Locked:
                        UI.Modal.Message("pin.locked_title", "pin.locked", null, secs);
                        break;
                }
            }, p.displayName);
        }

        private void Register()
        {
            var c = UI.Modal.Open("reg.title");
            UIKit.Label(c, "reg.name_help", T.bodySize * 0.9f, T.textMuted);
            var input = UIKit.Input(c, "reg.name_placeholder", 20, TMP_InputField.ContentType.Standard);
            UIKit.Button(c, "ui.next", ButtonStyle.Primary, () =>
            {
                string name = input.text.Trim();
                if (name.Length < 2) { UI.Toast("reg.name_short", Core.FeedbackKind.Mistake); return; }
                UI.Modal.PinPad("reg.choose_pin", pin1 =>
                    UI.Modal.PinPad("reg.confirm_pin", pin2 =>
                    {
                        if (pin1 != pin2) { UI.Toast("reg.pin_mismatch", Core.FeedbackKind.Mistake); return; }
                        var result = DataManager.Instance.Register(name, pin1, LocalizationService.Instance.Current, out _);
                        if (result == RegisterResult.NameTaken) UI.Toast("reg.name_taken", Core.FeedbackKind.Mistake);
                        else if (result != RegisterResult.Ok) UI.Toast("reg.failed", Core.FeedbackKind.Mistake);
                        else { UI.Toast("reg.done", Core.FeedbackKind.Success); Refresh(); }
                    }, name), name);
            });
            UIKit.Button(c, "ui.cancel", ButtonStyle.Ghost, UI.Modal.Close);
        }
    }
}
