using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Application output boundary invoked after a presented roll's narration and state consequences
/// have been committed, and before a later independent roll may begin.
/// </summary>
public interface IResolvedRollReactionSink
{
	Task NotifyAsync(CancellationToken ct = default);
}
