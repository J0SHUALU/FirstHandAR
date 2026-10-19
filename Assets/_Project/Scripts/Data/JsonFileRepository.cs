using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FirstHand.Data
{
    /// <summary>
    /// Encrypted JSON storage in persistentDataPath, laid out as in the DMP:
    ///   profiles.json                one file for all profiles
    ///   sessions/&lt;traineeId&gt;.json   one file per trainee, so deleting a trainee never rewrites others
    /// Writes go to a .tmp file first and are then swapped in, so a crash mid-write cannot corrupt data.
    /// </summary>
    public class JsonFileRepository : IRepository
    {
        private readonly string root;
        private readonly string sessionsDir;
        private readonly string exportsDir;
        private readonly AesFileCrypto crypto;

        public JsonFileRepository(string rootPath, string cachePath, AesFileCrypto crypto)
        {
            root = rootPath;
            sessionsDir = Path.Combine(root, "sessions");
            exportsDir = Path.Combine(cachePath, "exports");
            this.crypto = crypto;
            Directory.CreateDirectory(sessionsDir);
            Directory.CreateDirectory(exportsDir);
        }

        private string ProfilesPath => Path.Combine(root, "profiles.json");
        private string SessionPath(string traineeId) => Path.Combine(sessionsDir, Sanitize(traineeId) + ".json");

        public ProfilesFile LoadProfiles() => Read<ProfilesFile>(ProfilesPath) ?? new ProfilesFile();
        public void SaveProfiles(ProfilesFile file) => Write(ProfilesPath, file);

        public SessionsFile LoadSessions(string traineeId)
            => Read<SessionsFile>(SessionPath(traineeId)) ?? new SessionsFile { traineeId = traineeId };

        public void SaveSessions(SessionsFile file) => Write(SessionPath(file.traineeId), file);

        public void DeleteSessions(string traineeId)
        {
            var path = SessionPath(traineeId);
            if (File.Exists(path)) File.Delete(path);
        }

        public IEnumerable<string> AllTraineeIdsWithSessions()
        {
            foreach (var f in Directory.GetFiles(sessionsDir, "*.json"))
                yield return Path.GetFileNameWithoutExtension(f);
        }

        public string WriteExport(string fileName, string contents)
        {
            var path = Path.Combine(exportsDir, fileName);
            File.WriteAllText(path, contents);
            return path;
        }

        public void PurgeOldExports(double maxAgeHours)
        {
            foreach (var f in Directory.GetFiles(exportsDir))
                if ((DateTime.UtcNow - File.GetCreationTimeUtc(f)).TotalHours > maxAgeHours) File.Delete(f);
        }

        // ---------- internals ----------

        private T Read<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try
            {
                var json = crypto.Decrypt(File.ReadAllBytes(path));
                var obj = JsonUtility.FromJson<T>(json);
                return Migrate(obj);
            }
            catch (Exception e)
            {
                // A damaged file is moved aside instead of crashing the app or being silently overwritten.
                Debug.LogWarning($"[Repository] Could not read {Path.GetFileName(path)}: {e.GetType().Name}");
                File.Move(path, path + ".corrupt." + DateTime.UtcNow.Ticks);
                return null;
            }
        }

        private void Write<T>(string path, T obj)
        {
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, crypto.Encrypt(JsonUtility.ToJson(obj)));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>Hook for schemaVersion upgrades. Version 1 is the first, so nothing to do yet.</summary>
        private static T Migrate<T>(T obj) => obj;

        private static string Sanitize(string id)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) id = id.Replace(c, '_');
            return id;
        }
    }
}
