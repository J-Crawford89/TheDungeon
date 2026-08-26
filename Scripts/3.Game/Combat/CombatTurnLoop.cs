using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal interface ICombatTurnReadiness
{
	bool IsPlayerTurn(GameSessionState session);
}

internal sealed class CombatTurnLoop
{
	private readonly ICombatTurnReadiness _readiness;
	private readonly CombatEncounterLifecycle _lifecycle;
	private readonly CombatMonsterTurn _monsterTurn;
	private readonly CombatTurnPresentationHost? _turnPresentation;

	public CombatTurnLoop(
		ICombatTurnReadiness readiness,
		CombatEncounterLifecycle lifecycle,
		CombatMonsterTurn monsterTurn,
		CombatTurnPresentationHost? turnPresentation = null)
	{
		_readiness = readiness;
		_lifecycle = lifecycle;
		_monsterTurn = monsterTurn;
		_turnPresentation = turnPresentation;
	}

	public void NotifyPlayerTurnStarted(GameSessionState session)
	{
		if (session.Combat is not { } c)
			return;
		if (!_readiness.IsPlayerTurn(session))
			return;
		c.AbilityCooldowns.OnPlayerTurnStarted();
	}

	public async Task ProcessAutomaticMonsterTurnsAsync(
		GameSessionState session,
		CancellationToken ct = default)
	{
		while (session.Phase == GamePlayPhase.InProgress && session.Dungeon.DungeonMode == DungeonMode.Combat && session.Combat is { } c && c.TurnOrder.Count > 0)
		{
			var slot = c.TurnOrder[c.CurrentTurnIndex];
			if (slot.IsPlayer)
			{
				NotifyPlayerTurnStarted(session);
				return;
			}
			var room = session.Dungeon.CurrentRoom;
			if (room == null)
			{
				_lifecycle.EndCombatVictory(session);
				await PresentCombatEndedAsync(session, ct);
				return;
			}

			var feature = RoomFeatureHelper.GetFeature<MonsterFeature>(room);
			if (feature == null)
			{
				_lifecycle.EndCombatVictory(session);
				await PresentCombatEndedAsync(session, ct);
				return;
			}

			await _monsterTurn.ExecuteMonsterTurnAsync(session, feature, slot.MonsterIndex, ct);
			if (!IsCombatActive(session))
			{
				await PresentCombatEndedAsync(session, ct);
				return;
			}

			PruneDeadMonstersFromTurnOrder(session, feature);
			if (await EndIfVictoryAsync(session, feature, ct))
				return;

			await AdvanceTurnAndPresentAsync(session, ct);
		}
	}

	public void PruneDeadMonstersFromTurnOrder(GameSessionState session, MonsterFeature feature)
	{
		if (session.Combat is not { } c)
			return;
		if (c.TurnOrder.Count == 0)
			return;
		var beforeIndex = c.CurrentTurnIndex;
		var currentSlot = c.TurnOrder[beforeIndex];
		c.TurnOrder.RemoveAll(s => !s.IsPlayer && (s.MonsterIndex < 0 || s.MonsterIndex >= feature.Monsters.Count || feature.Monsters[s.MonsterIndex].CurrentHp <= 0));
		if (c.TurnOrder.Count == 0)
			return;
		var idx = c.TurnOrder.FindIndex(s => TurnSlotsEqual(s, currentSlot));
		if (idx >= 0)
			c.CurrentTurnIndex = idx;
		else
			c.CurrentTurnIndex = Math.Min(beforeIndex, c.TurnOrder.Count - 1);
	}

	private static bool TurnSlotsEqual(CombatTurnSlot a, CombatTurnSlot b) =>
		a.IsPlayer == b.IsPlayer && a.MonsterIndex == b.MonsterIndex;

	public bool IsCombatActive(GameSessionState session) =>
		session.Phase == GamePlayPhase.InProgress && session.Dungeon.DungeonMode == DungeonMode.Combat;

	public bool CheckVictory(GameSessionState session, MonsterFeature feature)
	{
		if (feature.Monsters.All(m => m.CurrentHp <= 0))
		{
			_lifecycle.EndCombatVictory(session);
			return true;
		}

		return false;
	}

	public async Task<bool> EndIfVictoryAsync(
		GameSessionState session,
		MonsterFeature feature,
		CancellationToken ct = default)
	{
		if (!CheckVictory(session, feature))
			return false;
		await PresentCombatEndedAsync(session, ct);
		return true;
	}

	public void AdvanceTurn(GameSessionState session)
	{
		if (session.Combat is not { } c || c.TurnOrder.Count == 0)
			return;
		c.CurrentTurnIndex = (c.CurrentTurnIndex + 1) % c.TurnOrder.Count;
	}

	public async Task AdvanceTurnAndPresentAsync(GameSessionState session, CancellationToken ct = default)
	{
		AdvanceTurn(session);
		await PresentActiveTurnAsync(session, ct);
	}

	public Task PresentOrderRevealedAsync(GameSessionState session, CancellationToken ct = default) =>
		PresentWhileCombatActiveAsync(session, CombatTurnPresentationKind.OrderRevealed, ct);

	public Task PresentActiveTurnAsync(GameSessionState session, CancellationToken ct = default) =>
		PresentWhileCombatActiveAsync(session, CombatTurnPresentationKind.ActiveTurnChanged, ct);

	public Task PresentCombatEndedAsync(GameSessionState session, CancellationToken ct = default)
	{
		_ = session;
		if (_turnPresentation == null)
			return Task.CompletedTask;
		return _turnPresentation.Sink.PresentAsync(CombatTurnPresentationKind.CombatEnded, ct);
	}

	private Task PresentWhileCombatActiveAsync(
		GameSessionState session,
		CombatTurnPresentationKind kind,
		CancellationToken ct)
	{
		if (_turnPresentation == null)
			return Task.CompletedTask;
		if (!IsCombatActive(session))
			return Task.CompletedTask;
		if (session.Combat is not { } combat || combat.TurnOrder.Count == 0)
			return Task.CompletedTask;
		return _turnPresentation.Sink.PresentAsync(kind, ct);
	}
}
