using System;
using System.Collections.Generic;
using System.Linq;

internal interface ICombatTurnReadiness
{
	bool IsAwaitingPlayerAction(GameSessionState session);
}

internal sealed class CombatTurnLoop
{
	private readonly ICombatTurnReadiness _readiness;
	private readonly CombatEncounterLifecycle _lifecycle;
	private readonly CombatMonsterTurn _monsterTurn;

	public CombatTurnLoop(
		ICombatTurnReadiness readiness,
		CombatEncounterLifecycle lifecycle,
		CombatMonsterTurn monsterTurn)
	{
		_readiness = readiness;
		_lifecycle = lifecycle;
		_monsterTurn = monsterTurn;
	}

	public void NotifyPlayerTurnStarted(GameSessionState session)
	{
		if (session.Combat is not { } c)
			return;
		if (!_readiness.IsAwaitingPlayerAction(session))
			return;
		c.AbilityCooldowns.OnPlayerTurnStarted();
	}

	public void ProcessAutomaticMonsterTurns(GameSessionState session)
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
				return;
			}

			var feature = RoomFeatureHelper.GetFeature<MonsterFeature>(room);
			if (feature == null)
			{
				_lifecycle.EndCombatVictory(session);
				return;
			}

			_monsterTurn.ExecuteMonsterTurn(session, feature, slot.MonsterIndex);
			if (session.Phase != GamePlayPhase.InProgress)
				return;
			if (session.Dungeon.DungeonMode != DungeonMode.Combat)
				return;

			PruneDeadMonstersFromTurnOrder(session, feature);
			if (CheckVictory(session, feature))
				return;

			AdvanceTurn(session);
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

	public bool CheckVictory(GameSessionState session, MonsterFeature feature)
	{
		if (feature.Monsters.All(m => m.CurrentHp <= 0))
		{
			_lifecycle.EndCombatVictory(session);
			return true;
		}

		return false;
	}

	public void AdvanceTurn(GameSessionState session)
	{
		if (session.Combat is not { } c || c.TurnOrder.Count == 0)
			return;
		c.CurrentTurnIndex = (c.CurrentTurnIndex + 1) % c.TurnOrder.Count;
	}
}
