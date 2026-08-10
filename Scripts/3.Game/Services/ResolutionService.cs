using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class ResolutionService
{
	private readonly IDiceRollRequestExecutor _diceRollService;
	private readonly DiceRollPresenterHost? _dicePresenterHost;

	public ResolutionService(IDiceRollRequestExecutor diceRollService, DiceRollPresenterHost? dicePresenterHost = null)
	{
		_diceRollService = diceRollService;
		_dicePresenterHost = dicePresenterHost;
	}

	public ResolutionResult RollAgainstTarget(DiceRollRequest request, DieRollVisualKind visualKind = DieRollVisualKind.Player)
	{
		_ = visualKind;
		var roll = _diceRollService.Roll(request);
		return BuildResult(roll, request.TargetNumber);
	}

	public async Task<ResolutionResult> RollAgainstTargetAsync(
		DiceRollRequest request,
		DieRollVisualKind visualKind = DieRollVisualKind.Player,
		CancellationToken ct = default)
	{
		var roll = _diceRollService.Roll(request);
		await PresentRollVisualAsync(roll, request, visualKind, ct);
		return BuildResult(roll, request.TargetNumber);
	}

	public Task PresentRollVisualAsync(
		DiceRollResult roll,
		DiceRollRequest request,
		DieRollVisualKind visualKind,
		CancellationToken ct = default) =>
		_dicePresenterHost == null
			? Task.CompletedTask
			: DiceRollPresentation.PresentRollAsync(_dicePresenterHost.Presenter, roll, request, visualKind, ct);

	public Task PresentSpecsAsync(
		IReadOnlyList<PhysicalDieRollSpec> specs,
		CancellationToken ct = default) =>
		_dicePresenterHost == null
			? Task.CompletedTask
			: DiceRollPresentation.PresentSpecsAsync(_dicePresenterHost.Presenter, specs, ct);

	/// <summary>Uses the same rules as <see cref="RollAgainstTarget"/> for an existing roll vs a DC.</summary>
	public ResolutionOutcome ResolveOutcomeAgainstTarget(DiceRollResult roll, int targetNumber) =>
		ParseOutcome(roll, targetNumber);

	private ResolutionResult BuildResult(DiceRollResult roll, int targetNumber) => new()
	{
		Roll = roll,
		TargetNumber = targetNumber,
		Outcome = ParseOutcome(roll, targetNumber),
	};

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
