using UnityEngine;
using FirstHand.AR;
using FirstHand.Core;
using FirstHand.Data;

namespace FirstHand.Simulation
{
    public interface ISimState
    {
        SimStateId Id { get; }
        void Enter();
        void Tick(float dt);
        void HandleAction(string action, string value);
        void Exit();
    }

    public abstract class SimStateBase : ISimState
    {
        protected readonly SimulationManager Sim;
        protected SimStateBase(SimulationManager sim) { Sim = sim; }
        public abstract SimStateId Id { get; }
        public virtual void Enter() { }
        public virtual void Tick(float dt) { }
        public virtual void HandleAction(string action, string value) { }
        public virtual void Exit() { }
    }

    public class StartState : SimStateBase
    {
        public StartState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Start;

        public override void Enter()
        {
            if (Sim.SceneLoaded) Sim.UnloadScene();
            Sim.ForgetAttempt();
        }

        public override void HandleAction(string action, string value)
        {
            if (action == "start" && DataManager.Instance.CurrentTrainee != null)
                Sim.ChangeState(SimStateId.Scanning);
        }
    }

    public class ScanningState : SimStateBase
    {
        public ScanningState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Scanning;

        public override void Enter()
        {
            ARManager.Instance.BeginScan(Sim.Scenario.minPlaneSize);
            EventBus.Subscribe<ScreenTapEvent>(OnScreenTap);
        }

        public override void Exit() => EventBus.Unsubscribe<ScreenTapEvent>(OnScreenTap);

        private async void OnScreenTap(ScreenTapEvent e)
        {
            if (await Sim.PlaceSceneAsync(e.ScreenPosition) && Sim.CurrentState == SimStateId.Scanning)
                Sim.ChangeState(SimStateId.Placement);
        }

        public override void HandleAction(string action, string value)
        {
            if (action == "back") Sim.ChangeState(SimStateId.Start);
        }
    }

    public class PlacementState : SimStateBase
    {
        public PlacementState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Placement;

        public override void Enter() => EventBus.Subscribe<ScreenTapEvent>(OnScreenTap);
        public override void Exit() => EventBus.Unsubscribe<ScreenTapEvent>(OnScreenTap);

        private async void OnScreenTap(ScreenTapEvent e) => await Sim.PlaceSceneAsync(e.ScreenPosition);

        public override void HandleAction(string action, string value)
        {
            switch (action)
            {
                case "rotate_left": Sim.RotateScene(-15f); break;
                case "rotate_right": Sim.RotateScene(15f); break;
                case "confirm_placement":
                    ARManager.Instance.EndScan();
                    Sim.ChangeState(SimStateId.ModeSelect);
                    break;
                case "back": Sim.ChangeState(SimStateId.Start); break;
            }
        }
    }

    public class ModeSelectState : SimStateBase
    {
        public ModeSelectState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.ModeSelect;

        public override void HandleAction(string action, string value)
        {
            switch (action)
            {
                case "mode_practice": Sim.BeginAttempt(TrainingMode.Practice); break;
                case "mode_assessment": Sim.BeginAttempt(TrainingMode.Assessment); break;
                case "reposition":
                    ARManager.Instance.BeginScan(Sim.Scenario.minPlaneSize);
                    Sim.ChangeState(SimStateId.Placement);
                    break;
                case "main_menu": Sim.ChangeState(SimStateId.Start); break;
            }
        }
    }

    public class RunningState : SimStateBase
    {
        private float lostFor;
        public RunningState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Running;

        public override void Enter()
        {
            lostFor = 0;
            Sim.Steps.Paused = false;
        }

        public override void Tick(float dt)
        {
            lostFor = ARManager.Instance.IsTracking ? 0 : lostFor + dt;
            if (lostFor >= Sim.TrackingLossPauseSeconds)
            {
                Sim.ChangeState(SimStateId.Paused);
                return;
            }
            Sim.Steps.Tick(dt);
        }

        public override void HandleAction(string action, string value)
        {
            if (action == "pause") Sim.ChangeState(SimStateId.Paused);
        }
    }

    public class PausedState : SimStateBase
    {
        public PausedState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Paused;

        public override void Enter() => Sim.Steps.Paused = true;

        public override void HandleAction(string action, string value)
        {
            switch (action)
            {
                case "resume":
                    if (ARManager.Instance.IsTracking) Sim.ChangeState(SimStateId.Running);
                    else EventBus.Publish(new StepFeedbackEvent { MessageKey = "fb.wait_tracking", Kind = FeedbackKind.Warning });
                    break;
                case "quit":
                    Sim.QuitAttempt();
                    Sim.ChangeState(SimStateId.Start);
                    break;
            }
        }
    }

    public class ResultsState : SimStateBase
    {
        public ResultsState(SimulationManager sim) : base(sim) { }
        public override SimStateId Id => SimStateId.Results;

        public override void HandleAction(string action, string value)
        {
            switch (action)
            {
                case "retry": Sim.ChangeState(SimStateId.ModeSelect); break;
                case "main_menu": Sim.ChangeState(SimStateId.Start); break;
            }
        }
    }
}
