using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Awaitable UI beat after combat turn order is committed, after each later turn advance
/// before the next combatant may act, and when combat ends (victory, flee, or game over).
/// Order and current combatant live on session combat state while combat is active.
/// </summary>
public interface ICombatTurnPresentationSink
{
	Task PresentAsync(CombatTurnPresentationKind kind, CancellationToken ct = default);
}
