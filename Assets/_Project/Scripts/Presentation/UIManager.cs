using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Simulation;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Shows the screen for the current state and routes events to it. It holds no simulation rules:
    /// buttons publish UiActionEvents and the active SimState decides what they mean.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private Canvas canvas;

        public ModalLayer Modal { get; private set; }

        private StartView startView;
        private ScanView scanView;
        private PlacementView placementView;
        private ModeView modeView;
        private HudView hudView;
        private PauseView pauseView;
        private ResultsView resultsView;
        private FacilitatorView facilitatorView;
        private ProgressView progressView;
        private View[] stateViews;

        private RectTransform toast;
        private TextMeshProUGUI toastText;
        private Image toastBg, toastDot;
        private float toastUntil;
        private bool built;
        private bool trackingLost;
        private float textScale = 1f;

        private async void Start()
        {
            UIKit.Theme = theme;
            textScale = PlayerPrefs.GetFloat("fh.textScale", 1f);
            UIKit.SetTextScale(textScale);

            var lang = (Language)PlayerPrefs.GetInt("fh.language", 0);
            await LocalizationService.Instance.SetLanguageAsync(lang);
            await AudioManager.Instance.InitAsync(lang);

            BuildUI();
            OnStateChanged(new StateChangedEvent { Current = SimulationManager.Instance.CurrentState });
        }

        private void BuildUI()
        {
            var root = (RectTransform)canvas.transform;
            var safe = UIKit.Rect(root, "SafeArea");
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            startView = new StartView();
            scanView = new ScanView();
            placementView = new PlacementView();
            modeView = new ModeView();
            hudView = new HudView();
            pauseView = new PauseView();
            resultsView = new ResultsView();
            facilitatorView = new FacilitatorView();
            progressView = new ProgressView();
            stateViews = new View[] { startView, scanView, placementView, modeView, hudView, pauseView, resultsView };
            foreach (var v in stateViews) v.Create(safe, this);
            facilitatorView.Create(safe, this);
            progressView.Create(safe, this);

            BuildToast(safe);
            Modal = new ModalLayer(safe);
            built = true;
        }

        /// <summary>Feedback toast: dark pill under the step card with a coloured dot (colour + words, NFR-05).</summary>
        private void BuildToast(RectTransform parent)
        {
            toastBg = UIKit.Img(parent, "Toast", theme.chip12, theme.ink);
            toast = toastBg.rectTransform;
            toast.anchorMin = new Vector2(0, 1); toast.anchorMax = new Vector2(1, 1); toast.pivot = new Vector2(0.5f, 1);
            toast.sizeDelta = new Vector2(-UIKit.D(48), 0);
            toast.anchoredPosition = new Vector2(0, -UIKit.D(250));
            UIKit.HStack(toast, 12, 16, 12);
            UIKit.Fit(toastBg);
            toastDot = UIKit.Img(toast, "Dot", theme.circle, theme.teal);
            UIKit.Size(toastDot, 10, 10);
            toastText = UIKit.Text(toast, "", TextStyle.BodyMedium, Color.white, TextAlignmentOptions.MidlineLeft, 14);
            UIKit.Size(toastText, -1, -1, 1);
            toast.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StateChangedEvent>(OnStateChanged);
            EventBus.Subscribe<AttemptFinishedEvent>(OnAttemptFinished);
            EventBus.Subscribe<TrackingChangedEvent>(OnTracking);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StateChangedEvent>(OnStateChanged);
            EventBus.Unsubscribe<AttemptFinishedEvent>(OnAttemptFinished);
            EventBus.Unsubscribe<TrackingChangedEvent>(OnTracking);
        }

        private void Update()
        {
            if (!built) return;
            if (toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false);
            if (scanView.Visible) scanView.Tick(Time.deltaTime);
            // In the attempt the toast sits under the instruction card; elsewhere under the status chip.
            float toastY = hudView.Visible ? hudView.InstructionBottom + UIKit.D(10) : UIKit.D(250);
            toast.anchoredPosition = new Vector2(0, -toastY);
            if (facilitatorView.Visible)
            {
                if (Pointer.current != null && Pointer.current.press.isPressed) facilitatorView.Touch();
                facilitatorView.Tick();
            }
        }

        // ---------- State -> screen ----------

        private void OnStateChanged(StateChangedEvent e)
        {
            if (!built) return;
            Modal.Close();
            View target = null;
            switch (e.Current)
            {
                case SimStateId.Start: target = startView; progressView.Hide(); facilitatorView.Hide(); break;
                case SimStateId.Scanning: target = scanView; break;
                case SimStateId.Placement: target = placementView; break;
                case SimStateId.ModeSelect: target = modeView; break;
                case SimStateId.Running:
                    target = hudView;
                    if (e.Previous != SimStateId.Paused) hudView.ResetAttempt();
                    break;
                case SimStateId.Paused:
                    pauseView.SetReason(trackingLost);
                    pauseView.Show();    // overlay on top of the HUD
                    return;
                case SimStateId.Results: return; // shown by OnAttemptFinished with the record
            }
            foreach (var v in stateViews) if (v != target) v.Hide();
            target?.Show();
        }

        private void OnAttemptFinished(AttemptFinishedEvent e)
        {
            foreach (var v in stateViews) if (v != resultsView) v.Hide();
            resultsView.ShowRecord(e.Record, DataManager.Instance.CurrentTrainee != null);
        }

        private void OnTracking(TrackingChangedEvent e) => trackingLost = !e.IsTracking;

        // ---------- Shared UI services ----------

        public void Toast(string key, FeedbackKind kind, float seconds = 2.2f)
        {
            if (!built || string.IsNullOrEmpty(key)) return;
            toastText.GetComponent<LocText>().Set(key);
            toastDot.color = kind == FeedbackKind.Mistake ? theme.coral
                           : kind == FeedbackKind.Warning ? theme.amber
                           : kind == FeedbackKind.Success ? theme.teal : Color.white;
            toast.gameObject.SetActive(true);
            toast.SetAsLastSibling();
            toastUntil = Time.unscaledTime + seconds;
        }

        public async void SetLanguage(Language lang)
        {
            await LocalizationService.Instance.SetLanguageAsync(lang);
            await AudioManager.Instance.SetLanguageAsync(lang);
            DataManager.Instance.SetLanguage(lang);
        }

        public void ApplyTraineeLanguage()
        {
            var t = DataManager.Instance.CurrentTrainee;
            if (t != null && t.language != LocalizationService.Instance.Current) SetLanguage(t.language);
        }

        public void OpenSettings()
        {
            var c = Modal.Open("settings.title");
            UIKit.Text(c, "settings.volume", TextStyle.Label);
            Segmented(c, new[] { ("settings.vol_off", 0f), ("settings.vol_low", 0.4f), ("settings.vol_mid", 0.7f), ("settings.vol_high", 1f) },
                AudioManager.Instance.Volume, v => { AudioManager.Instance.SetVolume(v); AudioManager.Instance.Play("tap"); OpenSettings(); });
            UIKit.Space(c, 6);
            UIKit.Text(c, "settings.text_size", TextStyle.Label);
            Segmented(c, new[] { ("settings.size_100", 1f), ("settings.size_125", 1.25f), ("settings.size_150", 1.5f) },
                textScale, v => { textScale = v; PlayerPrefs.SetFloat("fh.textScale", v); UIKit.SetTextScale(v); OpenSettings(); });
            UIKit.Space(c, 6);
            UIKit.Button(c, "ui.close", ButtonStyle.Secondary, Modal.Close, 56, false);
        }

        private void Segmented(Transform parent, (string key, float value)[] options, float current, System.Action<float> pick)
        {
            var seg = UIKit.Img(parent, "Segmented", theme.chip12, theme.paper2);
            UIKit.Size(seg, 48);
            var h = UIKit.HStack(seg.rectTransform, 0, 3, 3); h.childForceExpandWidth = true; h.childControlWidth = true; h.childForceExpandHeight = true;
            foreach (var (key, value) in options)
            {
                float v = value;
                bool on = Mathf.Approximately(current, v);
                var tab = UIKit.Img(seg.transform, "Tab", theme.chip12, on ? theme.card : new Color(0, 0, 0, 0), true);
                var label = UIKit.Text(tab.transform, key, TextStyle.Title, on ? theme.ink : theme.muted, TextAlignmentOptions.Center, 14);
                UIKit.Stretch(label.rectTransform);
                tab.gameObject.AddComponent<Button>().onClick.AddListener(() => pick(v));
            }
        }

        /// <summary>FR-28: facilitator PIN first (set it on first use), then the facilitator screen.</summary>
        public void OpenFacilitator()
        {
            var dm = DataManager.Instance;
            if (!dm.HasFacilitatorPin)
            {
                Modal.PinPad("fac.set_pin", pin1 => Modal.PinPad("fac.confirm_pin", pin2 =>
                {
                    if (pin1 != pin2) { Toast("reg.pin_mismatch", FeedbackKind.Mistake); return; }
                    dm.SetFacilitatorPin(pin1);
                    facilitatorView.Show();
                }));
                return;
            }
            Modal.PinPad("fac.enter_pin", pin =>
            {
                if (dm.VerifyFacilitatorPin(pin)) facilitatorView.Show();
                else Toast("pin.wrong_short", FeedbackKind.Mistake);
            });
        }

        public void RefreshStart() { if (startView.Visible) startView.Refresh(); }

        /// <summary>Last five sessions of the signed-in trainee (from the Start screen).</summary>
        public void ShowProgress() => progressView.Show();
    }
}
