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
    /// <summary>
    /// FR-03: text for the selected language. Only one language table is in memory at a time; switching
    /// loads the new table through Addressables and releases the old one.
    /// </summary>
    public class LocalizationService : Singleton<LocalizationService>
    {
        private readonly AssetService assets = new AssetService();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private string loadedKey;

        public Language Current { get; private set; }
        public bool Ready { get; private set; }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var inst = Instance;
            return inst != null && inst.strings.TryGetValue(key, out var v) ? v : key;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        public async Task SetLanguageAsync(Language language)
        {
            string key = language == Language.Pidgin ? "Text_pcm" : "Text_en";
            if (key == loadedKey) return;

            var table = await assets.LoadAsync<TextTable>(key);
            if (table == null) return;
            strings.Clear();
            foreach (var e in table.entries) strings[e.key] = e.text;

            if (loadedKey != null) assets.Release(loadedKey);
            loadedKey = key;
            Current = language;
            Ready = true;
            PlayerPrefs.SetInt("fh.language", (int)language);
            EventBus.Publish(new LanguageChangedEvent { Language = language });
        }

        protected override void OnDestroy()
        {
            assets.ReleaseAll();
            base.OnDestroy();
        }
    }
}
