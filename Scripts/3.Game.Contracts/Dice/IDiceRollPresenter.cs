using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDiceRollPresenter
{
	Task PresentDieAsync(
		PhysicalDieRollSpec die,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default);

	Task PresentDiceBatchAsync(
		IReadOnlyList<PhysicalDieRollSpec> dice,
		DicePresentationProfile profile = DicePresentationProfile.Standard,
		CancellationToken ct = default);
}
