using System;
using FirstHand.Data;

namespace FirstHand.Steps
{
    /// <summary>
    /// Factory: the step list comes from the ScenarioDefinition asset, so nothing else in the code
    /// decides which TrainingStep subclass to build. A new step type is one enum value and one line here;
    /// StepManager never changes.
    /// </summary>
    public static class StepFactory
    {
        public static TrainingStep Create(StepDefinition def)
        {
            switch (def.type)
            {
                case StepType.HazardScan:       return new HazardScanStep(def);
                case StepType.ResponseCheck:    return new ResponseCheckStep(def);
                case StepType.CallHelp:         return new CallHelpStep(def);
                case StepType.BreathingCheck:   return new BreathingCheckStep(def);
                case StepType.Compressions:     return new CompressionStep(def);
                case StepType.RecoveryPosition: return new RecoveryPositionStep(def);
                default: throw new ArgumentOutOfRangeException(nameof(def.type), def.type, "No step class for type");
            }
        }
    }
}
