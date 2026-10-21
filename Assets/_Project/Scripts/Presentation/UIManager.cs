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
        private View[] stateViews;

        private RectTransform toast;
        private TextMeshProUGUI toastText;
        private Image toastBg;
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
            stateViews = new View[] { startView, scanView, placementView, modeView, hudView, pauseView, resultsView };
            foreach (var v in stateViews) v.Create(safe, this);
            facilitatorView.Create(safe, this);

            BuildToast(safe);
            Modal = new ModalLayer(safe);
            built = true;
        }

        private void BuildToast(RectTransform parent)
        {
            toastBg = UIKit.Panel(parent, "Toast", theme.surfaceRaised, theme.pill);
            toastBg.raycastTarget = false;
            toast = toastBg.rectTransform;
            toast.anchorMin = new Vector2(0, 1); toast.anchorMax = new Vector2(1, 1);
            toast.pivot = new Vector2(0.5f, 1);
            toast.sizeDelta = new Vector2(-2 * theme.padding, 150);
            toast.anchoredPosition = new Vector2(0, -320);
            toastText = UIKit.Label(toast, "", theme.bodySize * 0.95f, Color.white, TextAlignmentOptions.Center, true);
            UIKit.Stretch(toastText.rectTransform, 30, 30, 10, 10);
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
                case SimStateId.Start: target = startView; break;
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
            toastBg.color = kind == FeedbackKind.Mistake ? theme.primary
                          : kind == FeedbackKind.Warning ? theme.warning
                          : kind == FeedbackKind.Success ? theme.success : theme.surfaceRaised;
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
            UIKit.Label(c, "settings.volume", theme.bodySize, theme.text, TextAlignmentOptions.Left, true);
            var sliderRow = UIKit.Rect(c, "Volume");
            UIKit.HStack(sliderRow);
            UIKit.Size(sliderRow, theme.buttonHeight);
            foreach (var (key, v) in new[] { ("settings.vol_off", 0f), ("settings.vol_low", 0.4f), ("settings.vol_mid", 0.7f), ("settings.vol_high", 1f) })
            {
                float vol = v;
                var style = Mathf.Approximately(AudioManager.Instance.Volume, vol) ? ButtonStyle.Success : ButtonStyle.Secondary;
                UIKit.Button(sliderRow, key, style, () => { AudioManager.Instance.SetVolume(vol); AudioManager.Instance.Play("tap"); OpenSettings(); });
            }

            UIKit.Label(c, "settings.text_size", theme.bodySize, theme.text, TextAlignmentOptions.Left, true);
            var sizeRow = UIKit.Rect(c, "Size");
            UIKit.HStack(sizeRow);
            UIKit.Size(sizeRow, theme.buttonHeight);
            foreach (var s in new[] { 1f, 1.25f, 1.5f })
            {
                float scale = s;
                var b = UIKit.Button(sizeRow, "settings.size_" + Mathf.RoundToInt(s * 100), Mathf.Approximately(textScale, s) ? ButtonStyle.Success : ButtonStyle.Secondary,
                    () => { textScale = scale; PlayerPrefs.SetFloat("fh.textScale", scale); UIKit.SetTextScale(scale); OpenSettings(); });
            }
            UIKit.Button(c, "ui.close", ButtonStyle.Ghost, Modal.Close);
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
    }
}
