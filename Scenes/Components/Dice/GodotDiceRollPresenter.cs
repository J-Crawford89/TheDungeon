using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class GodotDiceRollPresenter : IDiceRollPresenter
{
	private readonly DiceRollOverlay _overlay;

	public GodotDiceRollPresenter(DiceRollOverlay overlay)
	{
		_overlay = overlay;
	}

	public Task PresentDieAsync(PhysicalDieRollSpec die, CancellationToken ct = default) =>
		_overlay.PresentDieAsync(die, ct);

	public Task PresentDiceBatchAsync(IReadOnlyList<PhysicalDieRollSpec> dice, CancellationToken ct = default) =>
		_overlay.PresentDiceBatchAsync(dice, ct);
}
