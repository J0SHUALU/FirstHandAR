using System;
using System.Collections.Generic;
using FirstHand.Steps;

namespace FirstHand.Data
{
    public enum Language { English, Pidgin }
    public enum TrainingMode { Practice, Assessment }

    [Serializable]
    public class TraineeProfile
    {
        public string traineeId;
        public string displayName;
        public string pinHash;
        public string pinSalt;
        public Language language;
        public long createdAt;
        public long lastActiveAt;
        public int failedPinCount;
        public long lockedUntil;
    }

    [Serializable]
    public class SessionRecord
    {
        public string sessionId;
        public string traineeId;
        public string mode;
        public string scenarioId;
        public string branch;
        public string status;
        public int score;
        public bool passed;
        public long startedAt;
        public float durationSec;
        public List<string> hazards = new List<string>();
        public List<StepResult> steps = new List<StepResult>();

        public TrainingMode Mode => mode == "Assessment" ? TrainingMode.Assessment : TrainingMode.Practice;
        public bool IsComplete => status == "complete";
    }

    [Serializable]
    public class ProfilesFile
    {
        public int schemaVersion = DataSchema.Version;
        public List<TraineeProfile> profiles = new List<TraineeProfile>();
        public string facilitatorPinHash;
        public string facilitatorPinSalt;
    }

    [Serializable]
    public class SessionsFile
    {
        public int schemaVersion = DataSchema.Version;
        public string traineeId;
        public List<SessionRecord> sessions = new List<SessionRecord>();
    }

    public static class DataSchema
    {
        public const int Version = 1;
    }

    public struct TraineeSummary
    {
        public TraineeProfile Profile;
        public int Attempts;
        public int BestAssessmentScore;
        public bool HasPassed;
        public long LastAttemptAt;
    }
}
