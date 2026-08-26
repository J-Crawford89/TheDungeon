using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class UiCombatTurnPresentationSink : ICombatTurnPresentationSink
{
	private readonly Func<CombatTurnPresentationKind, CancellationToken, Task> _presentCombatTurn;

	public UiCombatTurnPresentationSink(
		Func<CombatTurnPresentationKind, CancellationToken, Task> presentCombatTurn)
	{
		_presentCombatTurn = presentCombatTurn ?? throw new ArgumentNullException(nameof(presentCombatTurn));
	}

	public Task PresentAsync(CombatTurnPresentationKind kind, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		return _presentCombatTurn(kind, ct);
	}
}
