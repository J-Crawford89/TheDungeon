public sealed class ResolutionResult
{
    public DiceRollResult Roll {  get; set; }
    public int TargetNumber { get; set; }
    public ResolutionOutcome Outcome { get; set; }
}