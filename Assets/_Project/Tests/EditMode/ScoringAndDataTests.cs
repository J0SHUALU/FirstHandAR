using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Scoring;
using FirstHand.Steps;

namespace FirstHand.Tests
{
    public class ScoringTests
    {
        private static StepResult R(string id, bool passed, int mistakes = 0, bool critical = false, int weight = 20, int hints = 0)
            => new StepResult { stepId = id, passed = passed, mistakes = mistakes, critical = critical, weight = weight, hintsUsed = hints, tipKey = "tip." + id };

        [Test]
        public void Assessment_PerfectPasses()
        {
            var results = new List<StepResult>
            {
                R("hazards", true), R("response", true), R("call", true, critical: true),
                R("breathing", true), R("compressions", true, critical: true)
            };
            var s = new AssessmentScoring(80).Score(results);
            Assert.AreEqual(100, s.Score);
            Assert.IsTrue(s.Passed);
            Assert.IsEmpty(s.TipKeys);
        }

        [Test]
        public void Assessment_FailsWhenCriticalStepFails_EvenWithHighScore()
        {
            var results = new List<StepResult>
            {
                R("hazards", true, weight: 30), R("response", true, weight: 30), R("call", false, critical: true, weight: 10),
                R("breathing", true, weight: 20), R("compressions", true, critical: true, weight: 10)
            };
            var s = new AssessmentScoring(80).Score(results);
            Assert.AreEqual(90, s.Score);
            Assert.IsFalse(s.Passed, "FR-24: call and compression/recovery steps must both pass");
            CollectionAssert.AreEqual(new[] { "tip.call" }, s.TipKeys);
        }

        [Test]
        public void Assessment_BelowPassMarkFails()
        {
            var results = new List<StepResult>
            {
                R("hazards", false), R("response", true, 2), R("call", true, critical: true),
                R("breathing", true), R("compressions", true, critical: true)
            };
            var s = new AssessmentScoring(80).Score(results);
            Assert.Less(s.Score, 80);
            Assert.IsFalse(s.Passed);
        }

        [Test]
        public void Practice_HasNoPassMarkAndGivesPartialCredit()
        {
            var results = new List<StepResult> { R("hazards", false), R("call", true, hints: 1) };
            var s = new PracticeScoring().Score(results);
            Assert.IsFalse(s.HasPassMark);
            Assert.IsFalse(s.Passed);
            Assert.AreEqual(70, s.Score);
        }

        [Test]
        public void RhythmMeter_RateAndRange()
        {
            var m = new RhythmMeter();
            for (int i = 0; i < 10; i++) m.AddTap(i * 0.5f);
            Assert.AreEqual(120f, m.CurrentRate, 0.5f);
            Assert.AreEqual(1f, m.PercentInRange(100, 120), 0.001f);
            m.AddTap(4.5f + 1.0f);
            Assert.AreEqual(0.9f, m.PercentInRange(100, 120), 0.001f);
        }
    }

    public class PoolTests
    {
        [Test]
        public void Pool_ReusesAndResets()
        {
            var parking = new GameObject("parking").transform;
            var pool = new ComponentPool<FirstHand.Entities.TapMarker>(() => new GameObject("p").AddComponent<FirstHand.Entities.TapMarker>(), parking, 2, 4);
            Assert.AreEqual(2, pool.InactiveCount);

            var a = pool.Get(null, Vector3.zero, Quaternion.identity);
            var b = pool.Get(null, Vector3.zero, Quaternion.identity);
            var c = pool.Get(null, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(3, pool.CreatedCount);
            Assert.IsTrue(a.gameObject.activeSelf);

            pool.Return(a);
            Assert.IsFalse(a.gameObject.activeSelf);
            Assert.AreEqual(parking, a.transform.parent, "returned instance parked");
            var again = pool.Get(null, Vector3.zero, Quaternion.identity);
            Assert.AreSame(a, again, "reuses the returned instance");
            Assert.AreEqual(3, pool.CreatedCount, "no new allocation");
            Assert.AreEqual(Vector3.zero, a.transform.localScale, "OnSpawned reset the marker");

            pool.ReturnAll();
            Assert.AreEqual(0, pool.ActiveCount);
            pool.Dispose();
            Object.DestroyImmediate(parking.gameObject);
            foreach (var p in new[] { a, b, c }) if (p != null) Object.DestroyImmediate(p.gameObject);
        }
    }

    public class MemoryRepository : IRepository
    {
        public ProfilesFile Profiles = new ProfilesFile();
        public Dictionary<string, SessionsFile> Sessions = new Dictionary<string, SessionsFile>();
        public ProfilesFile LoadProfiles() => Profiles;
        public void SaveProfiles(ProfilesFile file) => Profiles = file;
        public SessionsFile LoadSessions(string id) => Sessions.TryGetValue(id, out var f) ? f : new SessionsFile { traineeId = id };
        public void SaveSessions(SessionsFile file) => Sessions[file.traineeId] = file;
        public void DeleteSessions(string id) => Sessions.Remove(id);
        public IEnumerable<string> AllTraineeIdsWithSessions() => Sessions.Keys.ToList();
        public string WriteExport(string fileName, string contents) { LastExport = contents; return fileName; }
        public void PurgeOldExports(double h) { }
        public string LastExport;
    }

    public class DataTests
    {
        private DataManager dm;
        private MemoryRepository repo;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            dm = new GameObject("dm").AddComponent<DataManager>();
            repo = new MemoryRepository();
            dm.Init(repo);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(dm.gameObject);

        [Test]
        public void Register_ValidatesNameAndPin()
        {
            Assert.AreEqual(RegisterResult.BadName, dm.Register("A", "1234", Language.English, out _));
            Assert.AreEqual(RegisterResult.BadPin, dm.Register("Ada", "12a4", Language.English, out _));
            Assert.AreEqual(RegisterResult.Ok, dm.Register("Ada", "1234", Language.English, out var p));
            Assert.AreNotEqual("1234", p.pinHash, "PIN is never stored in plain text");
            Assert.AreEqual(RegisterResult.NameTaken, dm.Register("ada", "9999", Language.English, out _));
        }

        [Test]
        public void SignIn_LocksAfterFiveWrongPins()
        {
            dm.Register("Ada", "1234", Language.English, out var p);
            dm.SignOut();
            for (int i = 0; i < 4; i++) Assert.AreEqual(SignInResult.WrongPin, dm.SignIn(p.traineeId, "0000", out _));
            Assert.AreEqual(SignInResult.Locked, dm.SignIn(p.traineeId, "0000", out int secs));
            Assert.AreEqual(60, secs);
            Assert.AreEqual(SignInResult.Locked, dm.SignIn(p.traineeId, "1234", out _), "even the right PIN waits");
        }

        [Test]
        public void Sessions_SaveUpdateAndListMostRecentFirst()
        {
            dm.Register("Ada", "1234", Language.English, out var p);
            for (int i = 0; i < 7; i++)
                dm.SaveSession(new SessionRecord { sessionId = "s" + i, traineeId = p.traineeId, startedAt = 1000 + i, status = "complete", mode = "Practice" });
            dm.SaveSession(new SessionRecord { sessionId = "s3", traineeId = p.traineeId, startedAt = 1003, status = "complete", score = 77, mode = "Practice" });

            var recent = dm.GetSessions(p.traineeId, 5);
            Assert.AreEqual(5, recent.Count);
            Assert.AreEqual("s6", recent[0].sessionId);
            Assert.AreEqual(7, repo.Sessions[p.traineeId].sessions.Count, "update replaced, did not duplicate");
            Assert.AreEqual(77, dm.GetSessions(p.traineeId).First(s => s.sessionId == "s3").score);
        }

        [Test]
        public void Facilitator_DeleteRemovesProfileAndRecords()
        {
            dm.Register("Ada", "1234", Language.English, out var p);
            dm.SaveSession(new SessionRecord { sessionId = "s1", traineeId = p.traineeId, status = "complete", mode = "Assessment", score = 85, passed = true });
            var sum = dm.GetSummaries().Single();
            Assert.AreEqual(85, sum.BestAssessmentScore);
            Assert.IsTrue(sum.HasPassed);
            dm.ExportCsv();
            StringAssert.Contains("Ada", repo.LastExport);

            dm.DeleteTrainee(p.traineeId);
            Assert.IsEmpty(dm.Profiles);
            Assert.IsFalse(repo.Sessions.ContainsKey(p.traineeId));
        }

        [Test]
        public void Crypto_RoundTripAndTamperDetection()
        {
            var key = new byte[32];
            for (int i = 0; i < 32; i++) key[i] = (byte)i;
            var crypto = new AesFileCrypto(key);
            var blob = crypto.Encrypt("{\"hello\":1}");
            Assert.AreEqual("{\"hello\":1}", crypto.Decrypt(blob));
            blob[20] ^= 0xFF;
            Assert.Throws<CryptographicException>(() => crypto.Decrypt(blob));
        }

        [Test]
        public void PinHasher_SaltMakesHashesDiffer()
        {
            string s1 = PinHasher.NewSalt(), s2 = PinHasher.NewSalt();
            Assert.AreNotEqual(PinHasher.Hash("1234", s1), PinHasher.Hash("1234", s2));
            Assert.IsTrue(PinHasher.Verify("1234", s1, PinHasher.Hash("1234", s1)));
            Assert.IsFalse(PinHasher.Verify("1235", s1, PinHasher.Hash("1234", s1)));
        }
    }
}
