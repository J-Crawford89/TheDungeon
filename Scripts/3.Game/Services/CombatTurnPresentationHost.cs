using System.Threading;
using System.Threading.Tasks;

public sealed class CombatTurnPresentationHost
{
	public ICombatTurnPresentationSink Sink { get; set; } = NullCombatTurnPresentationSink.Instance;
}

public sealed class NullCombatTurnPresentationSink : ICombatTurnPresentationSink
{
	public static NullCombatTurnPresentationSink Instance { get; } = new();

	private NullCombatTurnPresentationSink()
	{
	}

	public Task PresentAsync(CombatTurnPresentationKind kind, CancellationToken ct = default)
	{
		_ = kind;
		return Task.CompletedTask;
	}
}
