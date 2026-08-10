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
		CancellationToken ct = default)
	{
		var specs = PhysicalDieRollExtractor.FromDiceRollResult(roll, request, kind);
		return PresentSpecsAsync(presenter, specs, ct);
	}

	public static Task PresentSpecsAsync(
		IDiceRollPresenter presenter,
		IReadOnlyList<PhysicalDieRollSpec> specs,
		CancellationToken ct = default)
	{
		if (specs.Count == 0)
			return Task.CompletedTask;
		if (specs.Count == 1)
			return presenter.PresentDieAsync(specs[0], ct);
		return presenter.PresentDiceBatchAsync(specs, ct);
	}
}
