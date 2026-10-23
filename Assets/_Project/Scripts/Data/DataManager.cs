using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Data
{
    public enum SignInResult { Ok, WrongPin, Locked, NotFound }
    public enum RegisterResult { Ok, BadName, BadPin, NameTaken }

    public class DataManager : Singleton<DataManager>
    {
        public const int MaxWrongPins = 5;
        public const int LockSeconds = 60;
        public const int RetentionDays = 365;
        public const double ExportMaxAgeHours = 24;

        private IRepository repo;
        private ProfilesFile profiles;

        public TraineeProfile CurrentTrainee { get; private set; }
        public bool HasFacilitatorPin => !string.IsNullOrEmpty(profiles.facilitatorPinHash);
        public IReadOnlyList<TraineeProfile> Profiles => profiles.profiles;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            var key = DeviceKeyProvider.GetOrCreate(Application.persistentDataPath);
            Init(new JsonFileRepository(Application.persistentDataPath, Application.temporaryCachePath, new AesFileCrypto(key)));
        }

        public void Init(IRepository repository)
        {
            repo = repository;
            profiles = repo.LoadProfiles();
            ApplyRetention();
            repo.PurgeOldExports(ExportMaxAgeHours);
        }

        public RegisterResult Register(string displayName, string pin, Language language, out TraineeProfile profile)
        {
            profile = null;
            displayName = displayName?.Trim() ?? "";
            if (displayName.Length < 2 || displayName.Length > 20) return RegisterResult.BadName;
            if (!IsValidPin(pin)) return RegisterResult.BadPin;
            if (profiles.profiles.Any(p => string.Equals(p.displayName, displayName, StringComparison.OrdinalIgnoreCase)))
                return RegisterResult.NameTaken;

            var salt = PinHasher.NewSalt();
            long now = Now();
            profile = new TraineeProfile
            {
                traineeId = "t-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                displayName = displayName,
                pinSalt = salt,
                pinHash = PinHasher.Hash(pin, salt),
                language = language,
                createdAt = now,
                lastActiveAt = now
            };
            profiles.profiles.Add(profile);
            repo.SaveProfiles(profiles);
            CurrentTrainee = profile;
            return RegisterResult.Ok;
        }

        public SignInResult SignIn(string traineeId, string pin, out int secondsLeft)
        {
            secondsLeft = 0;
            var p = profiles.profiles.FirstOrDefault(x => x.traineeId == traineeId);
            if (p == null) return SignInResult.NotFound;

            long now = Now();
            if (p.lockedUntil > now)
            {
                secondsLeft = (int)(p.lockedUntil - now);
                return SignInResult.Locked;
            }

            if (!PinHasher.Verify(pin, p.pinSalt, p.pinHash))
            {
                p.failedPinCount++;
                if (p.failedPinCount >= MaxWrongPins)
                {
                    p.failedPinCount = 0;
                    p.lockedUntil = now + LockSeconds;
                    secondsLeft = LockSeconds;
                    repo.SaveProfiles(profiles);
                    return SignInResult.Locked;
                }
                repo.SaveProfiles(profiles);
                return SignInResult.WrongPin;
            }

            p.failedPinCount = 0;
            p.lastActiveAt = now;
            repo.SaveProfiles(profiles);
            CurrentTrainee = p;
            return SignInResult.Ok;
        }

        public void SignOut() => CurrentTrainee = null;

        public void SetLanguage(Language language)
        {
            if (CurrentTrainee == null) return;
            CurrentTrainee.language = language;
            repo.SaveProfiles(profiles);
        }

        public void SaveSession(SessionRecord record)
        {
            var file = repo.LoadSessions(record.traineeId);
            int i = file.sessions.FindIndex(s => s.sessionId == record.sessionId);
            if (i >= 0) file.sessions[i] = record; else file.sessions.Add(record);
            repo.SaveSessions(file);

            var p = profiles.profiles.FirstOrDefault(x => x.traineeId == record.traineeId);
            if (p != null) { p.lastActiveAt = Now(); repo.SaveProfiles(profiles); }
            EventBus.Publish(new SessionSavedEvent { SessionId = record.sessionId });
        }

        public List<SessionRecord> GetSessions(string traineeId, int max = int.MaxValue)
            => repo.LoadSessions(traineeId).sessions.OrderByDescending(s => s.startedAt).Take(max).ToList();

        public List<SessionRecord> GetRecentSessionsAllTrainees(int max)
        {
            var all = new List<SessionRecord>();
            foreach (var id in repo.AllTraineeIdsWithSessions()) all.AddRange(repo.LoadSessions(id).sessions);
            return all.OrderByDescending(s => s.startedAt).Take(max).ToList();
        }

        public string DisplayNameFor(string traineeId)
            => profiles.profiles.FirstOrDefault(p => p.traineeId == traineeId)?.displayName ?? "?";

        public bool SetFacilitatorPin(string pin)
        {
            if (!IsValidPin(pin)) return false;
            profiles.facilitatorPinSalt = PinHasher.NewSalt();
            profiles.facilitatorPinHash = PinHasher.Hash(pin, profiles.facilitatorPinSalt);
            repo.SaveProfiles(profiles);
            return true;
        }

        public bool VerifyFacilitatorPin(string pin)
            => PinHasher.Verify(pin, profiles.facilitatorPinSalt, profiles.facilitatorPinHash);

        public List<TraineeSummary> GetSummaries()
        {
            var list = new List<TraineeSummary>();
            foreach (var p in profiles.profiles)
            {
                var sessions = repo.LoadSessions(p.traineeId).sessions;
                var assessed = sessions.Where(s => s.IsComplete && s.Mode == TrainingMode.Assessment).ToList();
                list.Add(new TraineeSummary
                {
                    Profile = p,
                    Attempts = sessions.Count,
                    BestAssessmentScore = assessed.Count > 0 ? assessed.Max(s => s.score) : -1,
                    HasPassed = assessed.Any(s => s.passed),
                    LastAttemptAt = sessions.Count > 0 ? sessions.Max(s => s.startedAt) : 0
                });
            }
            return list.OrderByDescending(s => s.HasPassed).ThenByDescending(s => s.BestAssessmentScore).ToList();
        }

        public string ExportCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("trainee,session,date,mode,branch,status,score,passed,duration_sec,failed_steps");
            foreach (var p in profiles.profiles)
            foreach (var s in repo.LoadSessions(p.traineeId).sessions.OrderBy(x => x.startedAt))
            {
                var failed = string.Join("|", s.steps.Where(x => !x.passed).Select(x => x.stepId));
                var date = DateTimeOffset.FromUnixTimeSeconds(s.startedAt).ToString("yyyy-MM-dd HH:mm");
                sb.AppendLine($"{Csv(p.displayName)},{s.sessionId},{date},{s.mode},{s.branch},{s.status},{s.score},{s.passed},{s.durationSec:0},{failed}");
            }
            return repo.WriteExport($"firsthand_export_{DateTime.Now:yyyyMMdd_HHmm}.csv", sb.ToString());
        }

        public void DeleteTrainee(string traineeId)
        {
            repo.DeleteSessions(traineeId);
            profiles.profiles.RemoveAll(p => p.traineeId == traineeId);
            repo.SaveProfiles(profiles);
            if (CurrentTrainee?.traineeId == traineeId) CurrentTrainee = null;
        }

        private void ApplyRetention()
        {
            long cutoff = Now() - RetentionDays * 86400L;
            var stale = profiles.profiles.Where(p => p.lastActiveAt < cutoff).Select(p => p.traineeId).ToList();
            foreach (var id in stale) repo.DeleteSessions(id);
            if (stale.Count > 0)
            {
                profiles.profiles.RemoveAll(p => stale.Contains(p.traineeId));
                repo.SaveProfiles(profiles);
            }
        }

        public static bool IsValidPin(string pin) => pin != null && pin.Length == 4 && pin.All(char.IsDigit);
        public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static string Csv(string v) => v.Contains(",") || v.Contains("\"") ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
