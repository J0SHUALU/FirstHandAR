using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using static FirstHand.EditorTools.BuilderUtil;
using Debug = UnityEngine.Debug;

namespace FirstHand.EditorTools
{
    public static class AudioFactory
    {
        private const int Rate = 22050;

        public static void BuildSfx()
        {
            EnsureFolders();
            Write("tap", Tone(1400, 0.03f, 0.35f, decay: 60));
            Write("key", Tone(880, 0.06f, 0.4f, decay: 30));
            Write("compress", Mix(Tone(110, 0.08f, 0.9f, decay: 35), Tone(2200, 0.012f, 0.4f, decay: 200)));
            Write("success", Seq(Tone(660, 0.08f, 0.5f), Tone(990, 0.12f, 0.5f, decay: 12)));
            Write("error", Square(196, 0.22f, 0.35f));
            Write("warning", Seq(Tone(880, 0.09f, 0.45f), Silence(0.05f), Tone(880, 0.09f, 0.45f)));
            Write("hazard_found", Seq(Tone(784, 0.07f, 0.45f), Tone(1047, 0.07f, 0.45f), Tone(1319, 0.16f, 0.45f, decay: 10)));
            Write("step_pass", Seq(Tone(523, 0.08f, 0.5f), Tone(659, 0.08f, 0.5f), Tone(784, 0.18f, 0.5f, decay: 8)));
            Write("step_fail", Seq(Tone(392, 0.12f, 0.5f), Tone(311, 0.22f, 0.5f, decay: 8)));
            Write("attempt_start", Sweep(300, 900, 0.35f, 0.45f));
            Write("attempt_end", Seq(Tone(523, 0.1f, 0.5f), Tone(659, 0.1f, 0.5f), Tone(784, 0.1f, 0.5f), Tone(1047, 0.35f, 0.5f, decay: 5)));
            Write("attempt_fail", Seq(Tone(523, 0.14f, 0.5f), Tone(440, 0.14f, 0.5f), Tone(349, 0.3f, 0.5f, decay: 6)));
            Write("place", Mix(Sweep(200, 600, 0.12f, 0.6f), Tone(1200, 0.02f, 0.3f, decay: 100)));
            Write("pause", Seq(Tone(523, 0.1f, 0.35f), Tone(392, 0.16f, 0.35f, decay: 10)));
            AssetDatabase.Refresh();

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Sfx }))
            {
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
                imp.defaultSampleSettings = s;
                imp.forceToMono = true;
                imp.SaveAndReimport();
            }
        }

        public static int BuildNarration(string enVoice = "Samantha", string pcmVoice = "Samantha")
        {
            if (Application.platform != RuntimePlatform.OSXEditor) { Debug.LogWarning("Narration generation needs macOS 'say'."); return 0; }
            EnsureFolders();
            int made = 0;
            var rows = TextContent.Rows;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                string key = rows[i, 0];
                if (!(key.StartsWith("instr.") || key.StartsWith("hint.") || key.StartsWith("fb."))) continue;
                string file = key.Replace('.', '-') + ".wav";
                made += Say(rows[i, 1], enVoice, Path.Combine(VoEn, file)) ? 1 : 0;
                made += Say(rows[i, 2], pcmVoice, Path.Combine(VoPcm, file)) ? 1 : 0;
            }
            AssetDatabase.Refresh();
            foreach (var dir in new[] { VoEn, VoPcm })
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { dir }))
            {
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
                imp.defaultSampleSettings = s;
                imp.forceToMono = true;
                imp.SaveAndReimport();
            }
            return made;
        }

        private static bool Say(string text, string voice, string assetPath)
        {
            string full = Path.GetFullPath(assetPath);
            if (File.Exists(full)) return false;
            var psi = new ProcessStartInfo("/usr/bin/say",
                $"-v {voice} -r 165 -o \"{full}\" --file-format=WAVE --data-format=LEI16@22050 \"{text.Replace("\"", "'")}\"")
            { UseShellExecute = false, CreateNoWindow = true };
            using (var p = Process.Start(psi)) p.WaitForExit(15000);
            return File.Exists(full);
        }

        private static float[] Tone(float hz, float sec, float amp, float decay = 20)
        {
            int n = (int)(sec * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float attack = Mathf.Clamp01(i / (Rate * 0.004f));
                d[i] = Mathf.Sin(2 * Mathf.PI * hz * t) * amp * attack * Mathf.Exp(-decay * t);
            }
            return d;
        }

        private static float[] Square(float hz, float sec, float amp)
        {
            var d = Tone(hz, sec, 1f, 6);
            for (int i = 0; i < d.Length; i++) d[i] = Mathf.Sign(d[i]) * amp * Mathf.Abs(d[i]);
            return d;
        }

        private static float[] Sweep(float from, float to, float sec, float amp)
        {
            int n = (int)(sec * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                phase += 2 * Math.PI * Mathf.Lerp(from, to, k) / Rate;
                d[i] = (float)Math.Sin(phase) * amp * Mathf.Sin(Mathf.PI * k);
            }
            return d;
        }

        private static float[] Silence(float sec) => new float[(int)(sec * Rate)];

        private static float[] Seq(params float[][] parts)
        {
            var list = new List<float>();
            foreach (var p in parts) list.AddRange(p);
            return list.ToArray();
        }

        private static float[] Mix(float[] a, float[] b)
        {
            var d = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < d.Length; i++) d[i] = (i < a.Length ? a[i] : 0) + (i < b.Length ? b[i] : 0);
            return d;
        }

        private static void Write(string name, float[] samples)
        {
            string path = Path.Combine(Sfx, name + ".wav");
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' }); w.Write(16);
                w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2);
                w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
