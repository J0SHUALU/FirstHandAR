using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using static FirstHand.EditorTools.BuilderUtil;

namespace FirstHand.EditorTools
{
    /// <summary>
    /// Puts runtime content into Addressable groups. Grouping follows when things are loaded and released:
    ///  - Scene Core: casualty, phone, tap marker (loaded at placement, released at main menu)
    ///  - Hazards: packed separately so only the 4 props in an attempt are in memory
    ///  - Text: one bundle per language, swapped on language change
    ///  - SFX: one small bundle loaded at boot
    ///  - Narration EN / PCM: one bundle per language, swapped on language change
    /// Everything is local (shipped in the APK) because the app must work offline (NFR-10).
    /// </summary>
    public static class AddressablesSetup
    {
        public static void Configure()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            settings.BuildRemoteCatalog = false;

            Add(settings, "Scene Core", BundledAssetGroupSchema.BundlePackingMode.PackTogether, null,
                $"{Prefabs}/Casualty.prefab", $"{Prefabs}/PhoneProp.prefab", $"{Prefabs}/TapMarker.prefab");

            var hazards = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { HazardPrefabs })) hazards.Add(AssetDatabase.GUIDToAssetPath(guid));
            Add(settings, "Hazards", BundledAssetGroupSchema.BundlePackingMode.PackSeparately, null, hazards.ToArray());

            Add(settings, "Text", BundledAssetGroupSchema.BundlePackingMode.PackSeparately, null, $"{DataDir}/Text_en.asset", $"{DataDir}/Text_pcm.asset");

            Add(settings, "SFX", BundledAssetGroupSchema.BundlePackingMode.PackTogether, "sfx", Clips(Sfx));
            Add(settings, "Narration EN", BundledAssetGroupSchema.BundlePackingMode.PackTogether, "vo_en", Clips(VoEn));
            Add(settings, "Narration PCM", BundledAssetGroupSchema.BundlePackingMode.PackTogether, "vo_pcm", Clips(VoPcm));

            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static string[] Clips(string folder)
        {
            var list = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder })) list.Add(AssetDatabase.GUIDToAssetPath(guid));
            return list.ToArray();
        }

        private static void Add(AddressableAssetSettings settings, string groupName, BundledAssetGroupSchema.BundlePackingMode packing, string label, params string[] paths)
        {
            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = packing;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4; // fast random access on device
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            if (label != null) settings.AddLabel(label);

            foreach (var path in paths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) { Debug.LogWarning($"[Addressables] Missing {path}"); continue; }
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = System.IO.Path.GetFileNameWithoutExtension(path);
                if (label != null) entry.SetLabel(label, true, true, false);
            }
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        }
    }
}
