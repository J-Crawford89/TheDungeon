using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class UiResolvedRollReactionSink : IResolvedRollReactionSink
{
	private readonly Action _refreshCoordinator;

	public UiResolvedRollReactionSink(Action refreshCoordinator)
	{
		_refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
	}

	public Task NotifyAsync(CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		_refreshCoordinator();
		return Task.CompletedTask;
	}
}
