using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class DiceRollPresentation
{
	public static void PresentRollFireAndForget(
		IDiceRollPresenter presenter,
		DiceRollResult roll,
		DiceRollRequest request,
		DieRollVisualKind kind)
	{
		var specs = PhysicalDieRollExtractor.FromDiceRollResult(roll, request, kind);
		PresentSpecsFireAndForget(presenter, specs);
	}

	public static void PresentResultFireAndForget(
		IDiceRollPresenter presenter,
		DiceRollResult roll,
		DieRollVisualKind kind) =>
		PresentRollFireAndForget(presenter, roll, new DiceRollRequest(), kind);

	public static void PresentSpecsFireAndForget(
		IDiceRollPresenter presenter,
		IReadOnlyList<PhysicalDieRollSpec> specs)
	{
		if (specs.Count == 0)
			return;
		_ = presenter.PresentDiceBatchAsync(specs);
	}

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
