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

	public Task PresentDieAsync(
		PhysicalDieRollSpec die,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		_overlay.PresentDieAsync(die, profile, ct);

	public Task PresentDiceBatchAsync(
		IReadOnlyList<PhysicalDieRollSpec> dice,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		_overlay.PresentDiceBatchAsync(dice, profile, ct);
}
