using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class DiceRollPresentation
{
	public static Task PresentRollAsync(
		IDiceRollPresenter presenter,
		DiceRollResult roll,
		DiceRollRequest request,
		DieRollVisualKind kind,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default)
	{
		var specs = PhysicalDieRollExtractor.FromDiceRollResult(roll, request, kind);
		return PresentSpecsAsync(presenter, specs, profile, ct);
	}

	public static Task PresentSpecsAsync(
		IDiceRollPresenter presenter,
		IReadOnlyList<PhysicalDieRollSpec> specs,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default)
	{
		if (specs.Count == 0)
			return Task.CompletedTask;
		if (specs.Count == 1)
			return presenter.PresentDieAsync(specs[0], profile, ct);
		return presenter.PresentDiceBatchAsync(specs, profile, ct);
	}
}
