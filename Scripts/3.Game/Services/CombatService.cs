using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CombatService
{
	private readonly DiceRollService _dice;
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly PlayerDownedResolutionService _playerDowned;
	private readonly TreasurePickupService _treasurePickup;
	private readonly CombatAbilityEffectsRegistry _combatAbilities;

	public CombatService(
		DiceRollService dice,
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		PlayerDownedResolutionService playerDowned,
		TreasurePickupService treasurePickup,
		CombatAbilityEffectsRegistry combatAbilities)
	{
		_dice = dice;
		_resolution = resolution;
		_narrative = narrative;
		_vitals = vitals;
		_playerDowned = playerDowned;
		_treasurePickup = treasurePickup;
		_combatAbilities = combatAbilities;
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

		var order = RollInitiativeOrder(session, feature);
		session.Combat.TurnOrder = order;
		session.AppendGameLog(_narrative.ForCombatTurnOrderSummary(BuildTurnOrderNames(feature, order)));

		ProcessAutomaticMonsterTurns(session);
		return true;
	}

	private List<string> BuildTurnOrderNames(MonsterFeature feature, List<CombatTurnSlot> order)
	{
		var names = new List<string>();
		foreach (var slot in order)
			names.Add(slot.IsPlayer ? "You" : feature.Monsters[slot.MonsterIndex].Definition.Name);
		return names;
	}

	private List<CombatTurnSlot> RollInitiativeOrder(GameSessionState session, MonsterFeature feature)
	{
		var entries = new List<InitiativeEntry>();

		var playerAgi = session.Player.AbilityScores.Agility;
		var pRoll = _dice.RollD20Plus("Initiative (you)", playerAgi, "Agility");
		session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll("You", pRoll) });
		entries.Add(new InitiativeEntry { Total = pRoll.Total, Agility = playerAgi, IsPlayer = true, MonsterIndex = -1 });

		for (var i = 0; i < feature.Monsters.Count; i++)
		{
			if (feature.Monsters[i].CurrentHp <= 0)
				continue;
			var monsterAgi = 0;
			var mRoll = _dice.RollD20Plus($"Initiative ({feature.Monsters[i].Definition.Name})", monsterAgi, "Agility");
			session.AppendLog(new LogEntry { Kind = LogEntryKind.Roll, Text = _narrative.ForCombatInitiativeRoll(feature.Monsters[i].Definition.Name, mRoll) });
			entries.Add(new InitiativeEntry { Total = mRoll.Total, Agility = monsterAgi, IsPlayer = false, MonsterIndex = i });
		}

		entries.Sort(InitiativeHelper.Compare);
		return entries.Select(e => e.IsPlayer
				? new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 }
				: new CombatTurnSlot { IsPlayer = false, MonsterIndex = e.MonsterIndex })
			.ToList();
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
			EndCombatVictory(session);
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

		PruneDeadMonstersFromTurnOrder(session, feature);
		if (CheckVictory(session, feature))
			return;

		AdvanceTurn(session);
		ProcessAutomaticMonsterTurns(session);
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
			RestoreExplorationAfterFlee(session);
			return;
		}

		session.AppendGameLog(_narrative.ForFleeFailure());
		AdvanceTurn(session);
		ProcessAutomaticMonsterTurns(session);
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
			PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (CheckVictory(session, monsterFeature))
				return;
		}

		AdvanceTurn(session);
		ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerUseHealthPotion(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		if (session.Player.HealthPotionCount <= 0)
		{
			session.AppendGameLog(_narrative.ForHealthPotionNoneLeft());
			return;
		}

		if (session.Player.CurrentHp >= session.Player.MaxHp)
		{
			session.AppendGameLog(_narrative.ForHealthPotionAtFullHealth());
			return;
		}

		session.Player.HealthPotionCount--;
		var heal = Math.Min(InventoryConstants.HealthPotionHealAmount, session.Player.MaxHp - session.Player.CurrentHp);
		session.Player.CurrentHp += heal;
		session.AppendGameLog(_narrative.ForUsedHealthPotion(heal, session.Player.CurrentHp));

		var room = session.Dungeon.CurrentRoom;
		if (room != null && RoomFeatureHelper.GetFeature<MonsterFeature>(room) is { } monsterFeature)
		{
			PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (CheckVictory(session, monsterFeature))
				return;
		}

		AdvanceTurn(session);
		ProcessAutomaticMonsterTurns(session);
	}

	public void ExecutePlayerDefend(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		if (!session.Player.HasAbility(AbilityIds.Defend))
			return;

		void Advance() { AdvanceTurn(session); ProcessAutomaticMonsterTurns(session); }

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

	private void RestoreExplorationAfterFlee(GameSessionState session)
	{
		var combat = session.Combat!;
		session.Dungeon.PlayerCoord = combat.FleeReturnCoord;
		var floor = session.Dungeon.Floors.FirstOrDefault(f => f.Level == combat.FleeReturnFloorLevel);
		if (floor != null)
			session.Dungeon.CurrentFloor = floor;
		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
	}

	private void NotifyPlayerTurnStarted(GameSessionState session)
	{
		if (session.Combat is not { } c)
			return;
		if (!IsAwaitingPlayerAction(session))
			return;
		c.AbilityCooldowns.OnPlayerTurnStarted();
	}

	private void ProcessAutomaticMonsterTurns(GameSessionState session)
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
				EndCombatVictory(session);
				return;
			}

			var feature = RoomFeatureHelper.GetFeature<MonsterFeature>(room);
			if (feature == null)
			{
				EndCombatVictory(session);
				return;
			}

			ExecuteMonsterTurn(session, feature, slot.MonsterIndex);
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

	private void ExecuteMonsterTurn(GameSessionState session, MonsterFeature feature, int monsterIndex)
	{
		if (monsterIndex < 0 || monsterIndex >= feature.Monsters.Count)
			return;
		var monster = feature.Monsters[monsterIndex];
		if (monster.CurrentHp <= 0)
			return;

		var name = monster.Definition.Name;
		var atk = monster.Definition.Attack;
		var playerEc = CombatFormulas.PlayerEvasionClass(session.Player.AbilityScores.Agility);
		var req = new DiceRollRequest
		{
			DiceRollLabel = $"{name} attacks",
			TargetNumber = playerEc,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true }
			},
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = atk, Source = "Attack" }
			}
		};
		var result = _resolution.RollAgainstTarget(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForAttackRoll(name, "you", result.Roll.Total, playerEc, result.Roll.DetailText)
		});

		if (result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess)
		{
			if (result.Outcome == ResolutionOutcome.CriticalSuccess)
				atk *= 2;
			var damage = atk;
			if (CombatPlayerIncomingDamage.TryApplyDefendNegate(ref damage, session, _narrative))
				return;

			var source = new PlayerDamageSource
			{
				Type = DamageSourceType.Monster,
				DisplayName = name,
				DefinitionId = monster.Definition.Id,
			};
			var vitals = _vitals.ApplyDamage(session.Player, damage);
			session.AppendGameLog(_narrative.ForMonsterHitPlayer(name, damage, vitals.HpAfterClamped));
			if (vitals.HpAfterClamped <= 0)
			{
				_playerDowned.Resolve(session, new PlayerDownedContext
				{
					Vitals = vitals,
					DamageSource = source,
				});
			}
		}
		else
			session.AppendGameLog(_narrative.ForAttackMiss(name, "you"));
	}

	private void PruneDeadMonstersFromTurnOrder(GameSessionState session, MonsterFeature feature)
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

	private bool CheckVictory(GameSessionState session, MonsterFeature feature)
	{
		if (feature.Monsters.All(m => m.CurrentHp <= 0))
		{
			EndCombatVictory(session);
			return true;
		}

		return false;
	}

	private void EndCombatVictory(GameSessionState session)
	{
		session.AppendGameLog(_narrative.ForCombatVictory());
		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
	}

	private void AdvanceTurn(GameSessionState session)
	{
		if (session.Combat is not { } c || c.TurnOrder.Count == 0)
			return;
		c.CurrentTurnIndex = (c.CurrentTurnIndex + 1) % c.TurnOrder.Count;
	}
}
