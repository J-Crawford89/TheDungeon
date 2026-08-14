using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class UiCombatTurnPresentationSink : ICombatTurnPresentationSink
{
	private readonly Action<UiRefreshFlags> _refreshHud;
	private readonly Func<CombatTurnPresentationKind, CancellationToken, Task>? _presentCombatTurn;

	public UiCombatTurnPresentationSink(
		Action<UiRefreshFlags> refreshHud,
		Func<CombatTurnPresentationKind, CancellationToken, Task>? presentCombatTurn = null)
	{
		_refreshHud = refreshHud ?? throw new ArgumentNullException(nameof(refreshHud));
		_presentCombatTurn = presentCombatTurn;
	}

	public Task PresentAsync(CombatTurnPresentationKind kind, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		_refreshHud(UiRefreshFlags.MainView | UiRefreshFlags.Command);
		return _presentCombatTurn?.Invoke(kind, ct) ?? Task.CompletedTask;
	}
}
