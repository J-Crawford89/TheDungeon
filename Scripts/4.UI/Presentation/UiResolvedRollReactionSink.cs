using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class UiResolvedRollReactionSink : IResolvedRollReactionSink
{
	private readonly Action<UiRefreshFlags> _refreshHud;

	public UiResolvedRollReactionSink(Action<UiRefreshFlags> refreshHud)
	{
		_refreshHud = refreshHud ?? throw new ArgumentNullException(nameof(refreshHud));
	}

	public Task NotifyAsync(CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		_refreshHud(UiRefreshFlags.All);
		return Task.CompletedTask;
	}
}
