using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FirstHand.Core;
using FirstHand.Data;
using FirstHand.Steps;

namespace FirstHand.Tests
{
    public class FakeCasualty : ICasualtyView
    {
        public Vector3 ChestTarget => Vector3.zero;
        public int Stage; public List<string> Snapped = new List<string>(); public bool HandlesActive;
        public void SetRecoveryStage(int completedStages) => Stage = completedStages;
        public void SnapLimbBack(string limbId) => Snapped.Add(limbId);
        public void SetLimbHandlesActive(bool active) => HandlesActive = active;
    }

    public class StepTests
    {
        private FakeCasualty casualty;

        [SetUp] public void SetUp() { EventBus.Clear(); casualty = new FakeCasualty(); }

        private StepContext Ctx(TrainingMode mode = TrainingMode.Practice, bool breathing = false) => new StepContext
        {
            Mode = mode, CasualtyBreathing = breathing, Casualty = casualty,
            HazardIds = new HashSet<string> { "wet_floor", "loose_cable" }
        };

        public static StepDefinition Def(StepType type, string id) => new StepDefinition
        {
            stepId = id, type = type, weight = 20, timeLimitSeconds = type == StepType.Compressions ? 90 : 30, maxMistakes = 2,
            hintKey = "hint." + id,
            targetCount = type == StepType.HazardScan ? 2 : 30,
            minValue = type == StepType.Compressions ? 100 : 5,
            maxValue = type == StepType.Compressions ? 120 : 10,
            radius = 0.06f, requiredInRange = 0.7f, requiredOnTarget = 0.8f, assessmentRounds = 2,
            expectedText = "112",
            orderedIds = type == StepType.ResponseCheck ? new List<string> { "casualty_shoulder_l", "casualty_shoulder_r" }
                       : type == StepType.CallHelp ? new List<string> { "location", "what_happened", "breathing" }
                       : new List<string> { "near_arm", "far_hand", "far_knee", "roll", "head_tilt" }
        };

        // ---------- Hazards ----------

        [Test]
        public void Hazards_PassWhenBothFound_AndDecoyCountsAsMistake()
        {
            var s = new HazardScanStep(Def(StepType.HazardScan, "hazards"));
            s.Begin(Ctx());
            s.HandleInput(new TapInput("decoy_bag", Vector3.zero, true));
            s.HandleInput(new TapInput("wet_floor", Vector3.zero, true));
            s.HandleInput(new TapInput("wet_floor", Vector3.zero, true)); // repeat ignored
            Assert.IsFalse(s.IsComplete);
            s.HandleInput(new TapInput("loose_cable", Vector3.zero, true));
            Assert.IsTrue(s.IsComplete);
            Assert.IsTrue(s.Passed);
            Assert.AreEqual(1, s.Mistakes);
        }

        [Test]
        public void Hazards_TapOnEmptyFloorIsNotAMistake()
        {
            var s = new HazardScanStep(Def(StepType.HazardScan, "hazards"));
            s.Begin(Ctx());
            s.HandleInput(new TapInput(null, Vector3.zero, false));
            Assert.AreEqual(0, s.Mistakes);
        }

        // ---------- Response ----------

        [Test]
        public void Response_ShoutBeforeShouldersIsMistake()
        {
            var def = Def(StepType.ResponseCheck, "response");
            def.maxMistakes = 0;
            var s = new ResponseCheckStep(def);
            s.Begin(Ctx());
            s.HandleInput(new ActionInput("shout"));
            s.HandleInput(new TapInput("casualty_shoulder_l", Vector3.zero, true));
            s.HandleInput(new TapInput("casualty_shoulder_r", Vector3.zero, true));
            s.HandleInput(new ActionInput("shout"));
            Assert.IsTrue(s.IsComplete);
            Assert.IsFalse(s.Passed, "FR-13: only correct if shoulders were tapped first");
        }

        [Test]
        public void Response_CorrectOrderPasses()
        {
            var s = new ResponseCheckStep(Def(StepType.ResponseCheck, "response"));
            s.Begin(Ctx());
            s.HandleInput(new TapInput("casualty_shoulder_r", Vector3.zero, true));
            s.HandleInput(new TapInput("casualty_shoulder_l", Vector3.zero, true));
            s.HandleInput(new ActionInput("shout"));
            Assert.IsTrue(s.Passed);
        }

        // ---------- Call ----------

        [Test]
        public void Call_WrongNumberThenCorrectAndInfo()
        {
            var s = new CallHelpStep(Def(StepType.CallHelp, "call"));
            s.Begin(Ctx());
            foreach (var d in "199") s.HandleInput(new ActionInput("dial", d.ToString()));
            s.HandleInput(new ActionInput("call"));
            Assert.AreEqual(1, s.Mistakes);
            Assert.IsFalse(s.IsConnected);

            foreach (var d in "112") s.HandleInput(new ActionInput("dial", d.ToString()));
            s.HandleInput(new ActionInput("call"));
            Assert.IsTrue(s.IsConnected);

            s.HandleInput(new ActionInput("info", "location"));
            s.HandleInput(new ActionInput("info", "weather"));
            s.HandleInput(new ActionInput("send_info"));
            Assert.AreEqual(2, s.Mistakes);
            s.HandleInput(new ActionInput("info", "weather"));       // toggle off
            s.HandleInput(new ActionInput("info", "what_happened"));
            s.HandleInput(new ActionInput("info", "breathing"));
            s.HandleInput(new ActionInput("send_info"));
            Assert.IsTrue(s.IsComplete);
            Assert.IsTrue(s.Passed);
        }

        // ---------- Breathing ----------

        [TestCase(3f, false)]
        [TestCase(7f, true)]
        [TestCase(12f, false)]
        public void Breathing_HoldWindow(float seconds, bool completes)
        {
            var s = new BreathingCheckStep(Def(StepType.BreathingCheck, "breathing"));
            s.Begin(Ctx());
            s.HandleInput(new HoldInput("check_breathing", seconds));
            Assert.AreEqual(completes, s.IsComplete);
            Assert.AreEqual(completes ? 0 : 1, s.Mistakes);
        }

        // ---------- Compressions ----------

        private static void Compress(CompressionStep s, int taps, float interval, Vector3 point)
        {
            for (int i = 0; i < taps; i++)
            {
                s.Tick(interval);
                s.HandleInput(new TapInput("casualty_chest", point, true));
            }
        }

        [Test]
        public void Compressions_GoodRhythmPasses()
        {
            var s = new CompressionStep(Def(StepType.Compressions, "compressions"));
            s.Begin(Ctx());
            Compress(s, 30, 0.55f, Vector3.zero); // ~109 per minute
            Assert.IsTrue(s.IsComplete);
            Assert.IsTrue(s.Passed);
            Assert.That(s.Evaluate().averageRate, Is.InRange(105f, 112f));
        }

        [Test]
        public void Compressions_TooSlowFails()
        {
            var s = new CompressionStep(Def(StepType.Compressions, "compressions"));
            s.Begin(Ctx());
            Compress(s, 30, 0.8f, Vector3.zero); // 75 per minute
            Assert.IsTrue(s.IsComplete);
            Assert.IsFalse(s.Passed);
        }

        [Test]
        public void Compressions_OffTargetFails()
        {
            var s = new CompressionStep(Def(StepType.Compressions, "compressions"));
            s.Begin(Ctx());
            Compress(s, 30, 0.55f, new Vector3(0.2f, 0, 0));
            Assert.IsFalse(s.Passed);
            Assert.AreEqual(0f, s.Evaluate().percentOnTarget, 0.001f);
        }

        [Test]
        public void Compressions_AssessmentNeedsTwoRounds()
        {
            var s = new CompressionStep(Def(StepType.Compressions, "compressions"));
            s.Begin(Ctx(TrainingMode.Assessment));
            Compress(s, 30, 0.55f, Vector3.zero);
            Assert.IsFalse(s.IsComplete);
            Compress(s, 30, 0.55f, Vector3.zero);
            Assert.IsTrue(s.IsComplete);
            Assert.IsTrue(s.Passed);
        }

        // ---------- Recovery ----------

        [Test]
        public void Recovery_OutOfOrderSnapsBackAndCounts()
        {
            var s = new RecoveryPositionStep(Def(StepType.RecoveryPosition, "recovery"));
            s.Begin(Ctx(breathing: true));
            Assert.IsTrue(casualty.HandlesActive);
            s.HandleInput(new DragInput("far_knee", true));
            Assert.AreEqual(1, s.Mistakes);
            CollectionAssert.Contains(casualty.Snapped, "far_knee");
            foreach (var id in new[] { "near_arm", "far_hand", "far_knee", "roll", "head_tilt" })
                s.HandleInput(new DragInput(id, true));
            Assert.IsTrue(s.Passed);
            Assert.AreEqual(5, casualty.Stage);
            Assert.IsFalse(casualty.HandlesActive);
        }

        [Test]
        public void Recovery_ShortDragIsNotAMistake()
        {
            var s = new RecoveryPositionStep(Def(StepType.RecoveryPosition, "recovery"));
            s.Begin(Ctx(breathing: true));
            s.HandleInput(new DragInput("near_arm", false));
            Assert.AreEqual(0, s.Mistakes);
            Assert.AreEqual(0, casualty.Stage);
        }

        // ---------- Base class rules ----------

        [Test]
        public void Assessment_TimeLimitFailsStep()
        {
            var s = new BreathingCheckStep(Def(StepType.BreathingCheck, "breathing"));
            s.Begin(Ctx(TrainingMode.Assessment));
            s.Tick(31f);
            Assert.IsTrue(s.IsComplete);
            Assert.IsFalse(s.Passed);
            Assert.IsTrue(s.TimedOut);
        }

        [Test]
        public void Practice_HasNoTimeLimitAndAllowsHints()
        {
            var s = new BreathingCheckStep(Def(StepType.BreathingCheck, "breathing"));
            s.Begin(Ctx(TrainingMode.Practice));
            s.Tick(100f);
            Assert.IsFalse(s.IsComplete);
            Assert.IsNotNull(s.RequestHint());
            Assert.AreEqual(1, s.HintsUsed);
        }

        [Test]
        public void Assessment_HasNoHints()
        {
            var s = new BreathingCheckStep(Def(StepType.BreathingCheck, "breathing"));
            s.Begin(Ctx(TrainingMode.Assessment));
            Assert.IsNull(s.RequestHint());
        }

        [Test]
        public void Factory_BuildsEveryType()
        {
            foreach (StepType t in System.Enum.GetValues(typeof(StepType)))
                Assert.IsNotNull(StepFactory.Create(Def(t, t.ToString())));
        }

        [Test]
        public void StepManager_PicksBranchAndRunsInOrder()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.steps.Add(Def(StepType.BreathingCheck, "breathing"));
            var comp = Def(StepType.Compressions, "compressions"); comp.branch = StepBranch.NotBreathing;
            var rec = Def(StepType.RecoveryPosition, "recovery"); rec.branch = StepBranch.Breathing;
            scenario.steps.Add(comp); scenario.steps.Add(rec);

            var mgr = new StepManager();
            bool finished = false;
            mgr.AllStepsFinished += () => finished = true;
            mgr.Load(scenario, Ctx(breathing: true));
            Assert.AreEqual(2, mgr.Count);
            mgr.Start();
            Assert.IsInstanceOf<BreathingCheckStep>(mgr.Current);
            mgr.HandleInput(new HoldInput("check_breathing", 6));
            Assert.IsInstanceOf<RecoveryPositionStep>(mgr.Current);
            foreach (var id in new[] { "near_arm", "far_hand", "far_knee", "roll", "head_tilt" })
                mgr.HandleInput(new DragInput(id, true));
            Assert.IsTrue(finished);
            Assert.AreEqual(2, mgr.Results.Count);
            mgr.Stop();
            Object.DestroyImmediate(scenario);
        }
    }
}
