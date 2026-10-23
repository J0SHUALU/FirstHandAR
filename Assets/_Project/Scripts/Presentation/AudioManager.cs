using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using FirstHand.Assets;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Simulation;

namespace FirstHand.Presentation
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource metronomeSource;

        private readonly AssetService assets = new AssetService();
        private readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> voice = new Dictionary<string, AudioClip>();
        private string voiceLabel;
        private float volume = 1f;

        public float Volume => volume;

        protected override void Awake()
        {
            base.Awake();
            volume = PlayerPrefs.GetFloat("fh.volume", 1f);
            ApplyVolume();
        }

        public async Task InitAsync(Language language)
        {
            var clips = await assets.LoadLabelAsync<AudioClip>("sfx");
            foreach (var c in clips) sfx[c.name] = c;
            await SetLanguageAsync(language);
        }

        public async Task SetLanguageAsync(Language language)
        {
            string label = language == Language.Pidgin ? "vo_pcm" : "vo_en";
            if (label == voiceLabel) return;
            voiceSource.Stop();
            voice.Clear();
            if (voiceLabel != null) assets.ReleaseLabel(voiceLabel);
            voiceLabel = label;
            var clips = await assets.LoadLabelAsync<AudioClip>(label);
            foreach (var c in clips) voice[c.name.Replace('-', '.')] = c;
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("fh.volume", volume);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (sfxSource) sfxSource.volume = volume;
            if (voiceSource) voiceSource.volume = volume;
            if (metronomeSource) metronomeSource.volume = volume * 0.6f;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<StepStartedEvent>(OnStepStarted);
            EventBus.Subscribe<StepFeedbackEvent>(OnFeedback);
            EventBus.Subscribe<RateUpdatedEvent>(OnRate);
            EventBus.Subscribe<HazardFoundEvent>(OnHazard);
            EventBus.Subscribe<StepCompletedEvent>(OnStepDone);
            EventBus.Subscribe<AttemptFinishedEvent>(OnAttemptDone);
            EventBus.Subscribe<StateChangedEvent>(OnState);
            EventBus.Subscribe<HintShownEvent>(OnHint);
            EventBus.Subscribe<ScenePlacedEvent>(OnPlaced);
            EventBus.Subscribe<UiActionEvent>(OnUiAction);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StepStartedEvent>(OnStepStarted);
            EventBus.Unsubscribe<StepFeedbackEvent>(OnFeedback);
            EventBus.Unsubscribe<RateUpdatedEvent>(OnRate);
            EventBus.Unsubscribe<HazardFoundEvent>(OnHazard);
            EventBus.Unsubscribe<StepCompletedEvent>(OnStepDone);
            EventBus.Unsubscribe<AttemptFinishedEvent>(OnAttemptDone);
            EventBus.Unsubscribe<StateChangedEvent>(OnState);
            EventBus.Unsubscribe<HintShownEvent>(OnHint);
            EventBus.Unsubscribe<ScenePlacedEvent>(OnPlaced);
            EventBus.Unsubscribe<UiActionEvent>(OnUiAction);
        }

        private void OnStepStarted(StepStartedEvent e) => Narrate(e.InstructionKey);
        private void OnHint(HintShownEvent e) => Narrate(e.HintKey);
        private void OnPlaced(ScenePlacedEvent e) => Play("place");
        private void OnHazard(HazardFoundEvent e) => Play("hazard_found");

        private void OnFeedback(StepFeedbackEvent e)
        {
            switch (e.Kind)
            {
                case FeedbackKind.Mistake: Play("error"); Vibrate(); break;
                case FeedbackKind.Warning: Play("warning"); Vibrate(); break;
                case FeedbackKind.Success: Play("success"); break;
            }
            Narrate(e.MessageKey);
        }

        private void OnRate(RateUpdatedEvent e)
        {
            if (!sfx.TryGetValue("compress", out var clip)) return;
            metronomeSource.pitch = e.Band == RateBand.TooFast ? 1.25f : e.Band == RateBand.TooSlow ? 0.8f : 1f;
            metronomeSource.PlayOneShot(clip);
            if (!e.OnTarget || e.Band == RateBand.TooSlow || e.Band == RateBand.TooFast) Vibrate();
        }

        private void OnStepDone(StepCompletedEvent e) => Play(e.Result.passed ? "step_pass" : "step_fail");
        private void OnAttemptDone(AttemptFinishedEvent e) => Play(e.Record.passed || e.Record.Mode == TrainingMode.Practice ? "attempt_end" : "attempt_fail");

        private void OnState(StateChangedEvent e)
        {
            if (e.Current == SimStateId.Running && e.Previous == SimStateId.ModeSelect) Play("attempt_start");
            if (e.Current == SimStateId.Paused) { voiceSource.Pause(); Play("pause"); }
            if (e.Previous == SimStateId.Paused && e.Current == SimStateId.Running) voiceSource.UnPause();
        }

        private void OnUiAction(UiActionEvent e)
        {
            if (e.Action == "dial") Play("key");
            else Play("tap");
        }

        public void Play(string name)
        {
            if (sfx.TryGetValue(name, out var clip)) sfxSource.PlayOneShot(clip);
        }

        public void Narrate(string key)
        {
            if (string.IsNullOrEmpty(key) || !voice.TryGetValue(key, out var clip)) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (PlayerPrefs.GetInt("fh.vibrate", 1) == 1) Handheld.Vibrate();
#endif
        }

        protected override void OnDestroy()
        {
            assets.ReleaseAll();
            base.OnDestroy();
        }
    }
}
