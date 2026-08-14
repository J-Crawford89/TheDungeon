using System.Threading;
using System.Threading.Tasks;

public sealed class ResolvedRollReactionHost
{
	public IResolvedRollReactionSink Sink { get; set; } = NullResolvedRollReactionSink.Instance;
}

public sealed class NullResolvedRollReactionSink : IResolvedRollReactionSink
{
	public static NullResolvedRollReactionSink Instance { get; } = new();

	private NullResolvedRollReactionSink()
	{
	}

	public Task NotifyAsync(GameSessionState session, CancellationToken ct = default) =>
		Task.CompletedTask;
}
