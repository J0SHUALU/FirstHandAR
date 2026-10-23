using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Presentation
{
    public class StartView : View
    {
        private RectTransform trainees;
        private Button startButton;
        private Image enTab, pcmTab;
        private TextMeshProUGUI enLabel, pcmLabel;

        protected override void Build()
        {
            UIKit.Fill(Root, "Paper", T.paper);
            var content = UIKit.Scroll(Root, 0, 24, 56, 28);

            var brand = UIKit.Rect(content, "Brand");
            UIKit.HStack(brand, 10);
            UIKit.Size(brand, 40);
            var mark = UIKit.Img(brand, "Mark", T.mark, Color.white); mark.preserveAspect = true; UIKit.Size(mark, 30, 30);
            var name = UIKit.Raw(brand, "FirstHand", TextStyle.Title, T.ink, TextAlignmentOptions.MidlineLeft, 19);
            name.font = T.headBold;
            UIKit.Size(name, -1, -1, 1);
            UIKit.IconButton(brand, "gear", UI.OpenSettings, 40, T.card, T.ink, true);

            UIKit.Space(content, 42);
            UIKit.Text(content, "start.kicker", TextStyle.Label);
            UIKit.Space(content, 14);
            UIKit.Text(content, "start.headline", TextStyle.H1, T.ink);
            UIKit.Text(content, "start.headline_accent", TextStyle.H1, T.coral);
            UIKit.Space(content, 18);
            UIKit.Ecg(content, T.coral, 36);
            UIKit.Space(content, 26);
            UIKit.Text(content, "start.who", TextStyle.Label);
            UIKit.Space(content, 12);

            trainees = UIKit.Rect(content, "Trainees");
            UIKit.VStack(trainees, 10);
            UIKit.Fit(trainees);

            UIKit.Space(content, 14);
            var add = UIKit.Rect(content, "Add");
            UIKit.HStack(add, 10);
            UIKit.Size(add, 30);
            UIKit.Icon(add, "plus", 20, T.coral);
            UIKit.Text(add, "start.new_trainee", TextStyle.Title, T.coral, TextAlignmentOptions.MidlineLeft, 15);
            var addBtn = add.gameObject.AddComponent<Button>();
            add.gameObject.AddComponent<Image>().color = Color.clear;
            addBtn.onClick.AddListener(Register);

            UIKit.Space(content, 44);
            startButton = UIKit.Button(content, "start.begin", ButtonStyle.Primary, () => Act("start"));
            UIKit.Space(content, 18);

            var foot = UIKit.Rect(content, "Footer");
            UIKit.HStack(foot, 0);
            UIKit.Size(foot, 40);
            var seg = UIKit.Img(foot, "Lang", T.chip12, T.paper2);
            UIKit.HardShadow(seg, T.ink, 2.5f);
            UIKit.Size(seg, 40, 150);
            var segH = UIKit.HStack(seg.rectTransform, 0, 3, 3); segH.childForceExpandWidth = true; segH.childControlWidth = true; segH.childForceExpandHeight = true;
            (enTab, enLabel) = Tab(seg.rectTransform, "start.lang_en", () => UI.SetLanguage(Language.English));
            (pcmTab, pcmLabel) = Tab(seg.rectTransform, "start.lang_pcm", () => UI.SetLanguage(Language.Pidgin));
            var spacer = UIKit.Rect(foot, "Spacer"); UIKit.Size(spacer, 1, -1, 1);
            var fac = UIKit.Rect(foot, "Facilitator");
            UIKit.HStack(fac, 6);
            UIKit.Icon(fac, "lock", 16, T.muted);
            UIKit.Text(fac, "start.facilitator", TextStyle.Small, T.muted, TextAlignmentOptions.MidlineLeft);
            fac.gameObject.AddComponent<Image>().color = Color.clear;
            fac.gameObject.AddComponent<Button>().onClick.AddListener(UI.OpenFacilitator);
        }

        private (Image, TextMeshProUGUI) Tab(RectTransform parent, string key, System.Action onClick)
        {
            var tab = UIKit.Img(parent, "Tab", T.chip12, T.card, true);
            var label = UIKit.Text(tab.transform, key, TextStyle.Title, T.ink, TextAlignmentOptions.Center, 14);
            UIKit.Stretch(label.rectTransform);
            tab.gameObject.AddComponent<Button>().onClick.AddListener(() => onClick());
            return (tab, label);
        }

        protected override void OnShow()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLang);
            Refresh();
        }

        protected override void OnHide() => EventBus.Unsubscribe<LanguageChangedEvent>(OnLang);
        private void OnLang(LanguageChangedEvent e) => RefreshLanguage();

        public void Refresh()
        {
            BuildTrainees();
            RefreshLanguage();
            startButton.gameObject.SetActive(DataManager.Instance.CurrentTrainee != null);
        }

        private void RefreshLanguage()
        {
            bool en = LocalizationService.Instance.Current == Language.English;
            enTab.color = en ? T.card : new Color(0, 0, 0, 0);
            pcmTab.color = en ? new Color(0, 0, 0, 0) : T.card;
            enLabel.color = en ? T.ink : T.muted;
            pcmLabel.color = en ? T.muted : T.ink;
        }

        private void BuildTrainees()
        {
            UIKit.Clear(trainees);
            var dm = DataManager.Instance;
            if (dm.Profiles.Count == 0)
            {
                UIKit.Text(trainees, "start.no_profiles", TextStyle.Body, T.muted);
                return;
            }
            var tints = new[] { T.tealTint, T.amberTint, T.coralTint, T.paper2 };
            int i = 0;
            foreach (var p in dm.Profiles)
            {
                var profile = p;
                bool current = dm.CurrentTrainee != null && dm.CurrentTrainee.traineeId == p.traineeId;
                var sessions = dm.GetSessions(p.traineeId);
                int best = -1; bool passed = false;
                foreach (var s in sessions) if (s.IsComplete && s.Mode == TrainingMode.Assessment) { if (s.score > best) best = s.score; passed |= s.passed; }
                string meta = passed ? LocalizationService.Format("start.meta_ready", best)
                            : best >= 0 ? LocalizationService.Format("start.meta_best", sessions.Count, best)
                            : LocalizationService.Format("start.meta_attempts", sessions.Count);
                TraineeRow(trainees, profile.displayName, meta, tints[i++ % tints.Length], current,
                    current ? (System.Action)(() => UI.ShowProgress()) : () => SignIn(profile));
            }
        }

        private void TraineeRow(Transform parent, string name, string meta, Color tint, bool selected, System.Action onTap)
        {
            var row = UIKit.Card(parent, "Trainee", T.card, !selected);
            UIKit.Size(row, 72);
            if (selected) { var o = UIKit.Img(row.transform, "Selected", T.card20Line, T.ink); UIKit.Stretch(o.rectTransform); UIKit.Overlay(o); }
            var h = UIKit.HStack(row.rectTransform, 14, 16, 0);
            var avatar = UIKit.Img(row.transform, "Avatar", T.circle, tint);
            UIKit.Size(avatar, 40, 40);
            var initials = UIKit.Raw(avatar.transform, Initials(name), TextStyle.Title, T.ink, TextAlignmentOptions.Center, 13);
            UIKit.Stretch(initials.rectTransform);
            var col = UIKit.Rect(row.transform, "Text");
            UIKit.VStack(col, 2, 0, 0, TextAnchor.MiddleLeft);
            UIKit.Size(col, -1, -1, 1);
            UIKit.Raw(col, name, TextStyle.Title, T.ink, TextAlignmentOptions.Left, 16);
            UIKit.Raw(col, meta, TextStyle.Label, T.muted, TextAlignmentOptions.Left, 10);
            if (selected) UIKit.Badge(row.transform, "check", T.ink, 22);
            else UIKit.Icon(row.transform, "chevron", 18, T.muted);
            row.gameObject.AddComponent<Button>().onClick.AddListener(() => onTap());
        }

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            string s = parts[0].Substring(0, 1);
            if (parts.Length > 1 && parts[1].Length > 0) s += parts[1].Substring(0, 1);
            else if (parts[0].Length > 1) s += parts[0].Substring(1, 1);
            return s.ToUpperInvariant();
        }

        private void SignIn(TraineeProfile p)
        {
            UI.Modal.PinPad("pin.enter", pin =>
            {
                switch (DataManager.Instance.SignIn(p.traineeId, pin, out int secs))
                {
                    case SignInResult.Ok: UI.ApplyTraineeLanguage(); Refresh(); break;
                    case SignInResult.WrongPin: UI.Toast("pin.wrong", FeedbackKind.Mistake); break;
                    case SignInResult.Locked: UI.Modal.Message("pin.locked_title", "pin.locked", null, secs); break;
                }
            }, p.displayName);
        }

        private void Register()
        {
            var c = UI.Modal.Open("reg.title");
            UIKit.Text(c, "reg.name_help", TextStyle.Body, T.ink2);
            var input = UIKit.Input(c, "reg.name_placeholder", 20, TMP_InputField.ContentType.Standard);
            UIKit.Button(c, "ui.next", ButtonStyle.Primary, () =>
            {
                string name = input.text.Trim();
                if (name.Length < 2) { UI.Toast("reg.name_short", FeedbackKind.Mistake); return; }
                UI.Modal.PinPad("reg.choose_pin", pin1 =>
                    UI.Modal.PinPad("reg.confirm_pin", pin2 =>
                    {
                        if (pin1 != pin2) { UI.Toast("reg.pin_mismatch", FeedbackKind.Mistake); return; }
                        var result = DataManager.Instance.Register(name, pin1, LocalizationService.Instance.Current, out _);
                        if (result == RegisterResult.NameTaken) UI.Toast("reg.name_taken", FeedbackKind.Mistake);
                        else if (result != RegisterResult.Ok) UI.Toast("reg.failed", FeedbackKind.Mistake);
                        else { UI.Toast("reg.done", FeedbackKind.Success); Refresh(); }
                    }, name), name);
            });
            UIKit.Button(c, "ui.cancel", ButtonStyle.Ghost, UI.Modal.Close, 48, false);
        }
    }
}
