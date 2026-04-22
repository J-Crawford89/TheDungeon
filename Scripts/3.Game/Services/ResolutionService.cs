public sealed class ResolutionService
{
    private readonly IDiceRollRequestExecutor _diceRollService;

    public ResolutionService(IDiceRollRequestExecutor diceRollService)
    {
        _diceRollService = diceRollService;
    }

    public ResolutionResult RollAgainstTarget(DiceRollRequest request)
    {
        var roll = _diceRollService.Roll(request);

        return new ResolutionResult()
        {
            Roll = roll,
            TargetNumber = request.TargetNumber,
            Outcome = ParseOutcome(roll, request.TargetNumber)
        };
    }

    /// <summary>Uses the same rules as <see cref="RollAgainstTarget"/> for an existing roll vs a DC.</summary>
    public ResolutionOutcome ResolveOutcomeAgainstTarget(DiceRollResult roll, int targetNumber) =>
        ParseOutcome(roll, targetNumber);

    private ResolutionOutcome ParseOutcome(DiceRollResult roll, int targetNumber)
    {
        if (IsCritical(roll, out var outcome))
            return outcome;

        int variance = roll.Total - targetNumber;
        switch (variance)
        {
            case >= 0:
                return ResolutionOutcome.Success;
            default:
                return ResolutionOutcome.Fail;
        }
    }

    private bool IsCritical(DiceRollResult roll, out ResolutionOutcome outcome)
    {
        outcome = ResolutionOutcome.None;
        if (roll.ResolvedD20CheckValue is not { } v)
            return false;
        if (v == 20)
        {
            outcome = ResolutionOutcome.CriticalSuccess;
            return true;
        }

        if (v == 1)
        {
            outcome = ResolutionOutcome.CriticalFail;
            return true;
        }

        return false;
    }
}