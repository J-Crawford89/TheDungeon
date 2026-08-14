using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class ResolutionService
{
	private readonly IDiceRollRequestExecutor _diceRollService;
	private readonly DiceRollPresenterHost? _dicePresenterHost;
	private readonly ResolvedRollReactionHost? _resolvedRollReactionHost;

	public ResolutionService(
		IDiceRollRequestExecutor diceRollService,
		DiceRollPresenterHost? dicePresenterHost = null,
		ResolvedRollReactionHost? resolvedRollReactionHost = null)
	{
		_diceRollService = diceRollService;
		_dicePresenterHost = dicePresenterHost;
		_resolvedRollReactionHost = resolvedRollReactionHost;
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
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default)
	{
		var roll = _diceRollService.Roll(request);
		await PresentRollVisualAsync(roll, request, visualKind, profile, ct);
		return BuildResult(roll, request.TargetNumber);
	}

	public Task PresentRollVisualAsync(
		DiceRollResult roll,
		DiceRollRequest request,
		DieRollVisualKind visualKind,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		_dicePresenterHost == null
			? Task.CompletedTask
			: DiceRollPresentation.PresentRollAsync(
				_dicePresenterHost.Presenter,
				roll,
				request,
				visualKind,
				profile,
				ct);

	public Task PresentSpecsAsync(
		IReadOnlyList<PhysicalDieRollSpec> specs,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		_dicePresenterHost == null
			? Task.CompletedTask
			: DiceRollPresentation.PresentSpecsAsync(_dicePresenterHost.Presenter, specs, profile, ct);

	public Task NotifyResolvedRollAsync(GameSessionState session, CancellationToken ct = default) =>
		_resolvedRollReactionHost == null
			? Task.CompletedTask
			: _resolvedRollReactionHost.Sink.NotifyAsync(session, ct);

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
