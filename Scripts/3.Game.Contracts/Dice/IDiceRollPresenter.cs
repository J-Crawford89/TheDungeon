using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IDiceRollPresenter
{
	Task PresentDieAsync(PhysicalDieRollSpec die, CancellationToken ct = default);

	Task PresentDiceBatchAsync(IReadOnlyList<PhysicalDieRollSpec> dice, CancellationToken ct = default);
}
