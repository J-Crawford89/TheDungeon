using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CombatService : ICombatService, ICombatTurnReadiness
{
	private readonly DiceRollService _dice;
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly PlayerDownedResolutionService _playerDowned;
	private readonly TreasurePickupService _treasurePickup;
	private readonly PotionEffectApplicationService _potionEffects;
	private readonly TrapService _trapService;
	private readonly CombatAbilityEffectsRegistry _combatAbilities = new();
	private readonly CombatEncounterLifecycle _lifecycle;
	private readonly CombatInitiative _initiative;
	private readonly CombatMonsterTurn _monsterTurn;
	private readonly CombatTurnLoop _turnLoop;

	public CombatService(
		DiceRollService dice,
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		PlayerDownedResolutionService playerDowned,
		TreasurePickupService treasurePickup,
		PotionEffectApplicationService potionEffects,
		TrapService trapService)
	{
		_dice = dice;
		_resolution = resolution;
		_narrative = narrative;
		_vitals = vitals;
		_playerDowned = playerDowned;
		_treasurePickup = treasurePickup;
		_trapService = trapService;
		_lifecycle = new CombatEncounterLifecycle(_narrative);
		_monsterTurn = new CombatMonsterTurn(_resolution, _narrative, _vitals, _playerDowned);
		_initiative = new CombatInitiative(_dice, _narrative);
		_turnLoop = new CombatTurnLoop(this, _lifecycle, _monsterTurn);
		_potionEffects = potionEffects;
		RegisterCombatAbilityHandlers();
	}

	private void RunAfterSuccessfulCombatHealthPotion(GameSessionState session)
	{
		var room = session.Dungeon.CurrentRoom;
		if (room != null && RoomFeatureHelper.GetFeature<MonsterFeature>(room) is { } monsterFeature)
		{
			_turnLoop.PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (_turnLoop.CheckVictory(session, monsterFeature))
				return;
		}

		_turnLoop.AdvanceTurn(session);
		_turnLoop.ProcessAutomaticMonsterTurns(session);
	}

	private void RegisterCombatAbilityHandlers()
	{
		_combatAbilities.Register(new DefendCombatAbilityHandler(_narrative, IsAwaitingPlayerAction));
	}

	public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel)
	{
		if (session.Phase != GamePlayPhase.InProgress)
			return false;
		if (session.Dungeon.DungeonMode == DungeonMode.Combat)
			return false;
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;
		var feature = RoomFeatureHelper.GetFeature<MonsterFeature>(room);
		if (feature == null)
			return false;
		if (!feature.Monsters.Any(m => m.CurrentHp > 0))
			return false;

		session.Combat = new CombatState
		{
			FleeReturnCoord = previousCoord,
			FleeReturnFloorLevel = floorLevel,
			FleeDc = 12,
			TurnOrder = new List<CombatTurnSlot>(),
			CurrentTurnIndex = 0
		};
		session.Dungeon.DungeonMode = DungeonMode.Combat;

		session.AppendGameLog(_narrative.ForCombatStarted(feature.Monsters.Where(m => m.CurrentHp > 0).Select(m => m.Definition.Name).ToList()));

		var order = _initiative.RollInitiativeOrder(session, feature);
		session.Combat.TurnOrder = order;
		session.AppendGameLog(_narrative.ForCombatTurnOrderSummary(_initiative.BuildTurnOrderNames(feature, order)));

		_turnLoop.ProcessAutomaticMonsterTurns(session);
		return true;
	}

	public bool IsAwaitingPlayerAction(GameSessionState session)
	{
		if (session.Phase != GamePlayPhase.InProgress)
			return false;
		if (session.Combat is not { } c || session.Dungeon.DungeonMode != DungeonMode.Combat)
			return false;
		if (c.TurnOrder.Count == 0)
			return false;
		var slot = c.TurnOrder[c.CurrentTurnIndex];
		return slot.IsPlayer;
	}

	public void ExecutePlayerAttack(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		var room = session.Dungeon.CurrentRoom!;
		var feature = RoomFeatureHelper.GetFeature<MonsterFeature>(room)!;
		var targetIndex = CombatState.FirstLivingMonsterIndex(feature);
		if (targetIndex < 0)
		{
			_lifecycle.EndCombatVictory(session);
			return;
		}

		var monster = feature.Monsters[targetIndex];
		var might = session.Player.AbilityScores.Might;
		var req = new DiceRollRequest
		{
			DiceRollLabel = $"Unarmed strike vs {monster.Definition.Name}",
			TargetNumber = monster.Definition.Defense,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true }
			},
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = might, Source = "Might" }
			}
		};
		var result = _resolution.RollAgainstTarget(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForAttackRoll("You", monster.Definition.Name, result.Roll.Total, monster.Definition.Defense, result.Roll.DetailText)
		});

		if (result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess)
		{
			var d6 = _dice.Roll(DieType.d6);
			var dmg = CombatFormulas.UnarmedDamageTotal(d6.RolledValue, might);
			if (result.Outcome == ResolutionOutcome.CriticalSuccess)
				dmg *= 2;
			monster.CurrentHp -= dmg;
			session.AppendGameLog(_narrative.ForDamageDealt(monster.Definition.Name, dmg, monster.CurrentHp, d6.RolledValue));
		}
		else
			session.AppendGameLog(_narrative.ForAttackMiss("You", monster.Definition.Name));

		_turnLoop.PruneDeadMonstersFromTurnOrder(session, feature);
		if (_turnLoop.CheckVictory(session, feature))
			return;

		_turnLoop.AdvanceTurn(session);
		_turnLoop.ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerFlee(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		var combat = session.Combat!;
		var agi = session.Player.AbilityScores.Agility;
		var req = new DiceRollRequest
		{
			DiceRollLabel = "Flee",
			TargetNumber = combat.FleeDc,
			CheckStyle = D20CheckStyle.None,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = false }
			},
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = agi, Source = "Agility" }
			}
		};
		var result = _resolution.RollAgainstTarget(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForFleeRoll(result.Roll.Total, combat.FleeDc, result.Roll.DetailText)
		});

		if (result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess)
		{
			session.AppendGameLog(_narrative.ForFleeSuccess());
			_lifecycle.RestoreExplorationAfterFlee(session);
			return;
		}

		session.AppendGameLog(_narrative.ForFleeFailure());
		_turnLoop.AdvanceTurn(session);
		_turnLoop.ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerTakeTreasure(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		var outcome = _treasurePickup.TakeAllFromCurrentRoom(session);
		if (outcome != TakeTreasureOutcome.TookItems)
			return;
		var room = session.Dungeon.CurrentRoom;
		if (room != null && RoomFeatureHelper.GetFeature<MonsterFeature>(room) is { } monsterFeature)
		{
			_turnLoop.PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (_turnLoop.CheckVictory(session, monsterFeature))
				return;
		}

		_turnLoop.AdvanceTurn(session);
		_turnLoop.ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerUseHealthPotion(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		var outcome = _potionEffects.TryUseHealthPotion(session);
		if (outcome == HealthPotionUseOutcome.Applied)
			RunAfterSuccessfulCombatHealthPotion(session);
	}

	public void ExecutePlayerDisarmTrap(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		var result = _trapService.TryDisarm(session);
		if (!result.ShouldAdvanceCombatTurn)
			return;

		var room = session.Dungeon.CurrentRoom;
		if (room != null && RoomFeatureHelper.GetFeature<MonsterFeature>(room) is { } monsterFeature)
		{
			_turnLoop.PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (_turnLoop.CheckVictory(session, monsterFeature))
				return;
		}

		_turnLoop.AdvanceTurn(session);
		_turnLoop.ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerDefend(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		if (!session.Player.HasAbility(AbilityIds.Defend))
			return;

		void Advance() { _turnLoop.AdvanceTurn(session); _turnLoop.ProcessAutomaticMonsterTurns(session); }

		if (_combatAbilities.TryExecute(AbilityIds.Defend, session, Advance))
			return;

		if (session.Combat is not { } c)
			return;
		if (c.HasDefendStanceActive())
			session.AppendGameLog(_narrative.ForDefendAlreadyDefending());
		else if (c.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend))
			session.AppendGameLog(_narrative.ForDefendOnCooldown());
		else
			session.AppendGameLog(_narrative.ForDefendCannotUse());
	}
}
