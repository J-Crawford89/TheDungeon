using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class NullDiceRollPresenter : IDiceRollPresenter
{
	public static NullDiceRollPresenter Instance { get; } = new();

	private NullDiceRollPresenter() { }

	public Task PresentDieAsync(
		PhysicalDieRollSpec die,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		Task.CompletedTask;

	public Task PresentDiceBatchAsync(
		IReadOnlyList<PhysicalDieRollSpec> dice,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default) =>
		Task.CompletedTask;
}
