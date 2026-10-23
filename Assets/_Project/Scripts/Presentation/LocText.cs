using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using FirstHand.Assets;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Presentation
{
    /// <summary>A label that re-reads its text whenever the language changes.</summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocText : MonoBehaviour
    {
        [SerializeField] private string key;
        private object[] args;
        private TextMeshProUGUI label;

        /// <summary>Mono labels in the design are uppercase in both languages.</summary>
        public bool Upper { get; set; }

        public void Set(string newKey, params object[] newArgs)
        {
            key = newKey;
            args = newArgs;
            Refresh();
        }

        private void Awake() => label = GetComponent<TextMeshProUGUI>();
        private void OnEnable() { EventBus.Subscribe<LanguageChangedEvent>(OnLang); Refresh(); }
        private void OnDisable() => EventBus.Unsubscribe<LanguageChangedEvent>(OnLang);
        private void OnLang(LanguageChangedEvent e) => Refresh();

        private void Refresh()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (string.IsNullOrEmpty(key)) return;
            var s = args != null && args.Length > 0 ? LocalizationService.Format(key, args) : LocalizationService.Get(key);
            label.text = Upper ? s.ToUpperInvariant() : s;
        }
    }
}
