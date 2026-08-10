using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class CombatService : ICombatService, ICombatTurnReadiness
{
	private readonly DiceRollService _dice;
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly IItemDefinitionRepository _items;
	private readonly PlayerVitalsService _vitals;
	private readonly PlayerDownedResolutionService _playerDowned;
	private readonly TreasurePickupService _treasurePickup;
	private readonly PotionEffectApplicationService _potionEffects;
	private readonly TrapService _trapService;
	private readonly PlayerExperienceService? _experience;
	private readonly CombatAbilityEffectsRegistry _combatAbilities = new();
	private readonly CombatEncounterLifecycle _lifecycle;
	private readonly CombatInitiative _initiative;
	private readonly CombatMonsterTurn _monsterTurn;
	private readonly CombatTurnLoop _turnLoop;
	private readonly IAttackRollAbilityOverlay? _attackRollAbilityOverlay;
	private bool _isResolvingAction;

	public CombatService(
		DiceRollService dice,
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		PlayerDownedResolutionService playerDowned,
		TreasurePickupService treasurePickup,
		PotionEffectApplicationService potionEffects,
		TrapService trapService,
		IItemDefinitionRepository itemDefinitions,
		DiceRollPresenterHost? dicePresenterHost = null,
		PlayerExperienceService? experience = null,
		IAttackRollAbilityOverlay? attackRollAbilityOverlay = null)
	{
		_dice = dice;
		_resolution = resolution;
		_narrative = narrative;
		_items = itemDefinitions;
		_vitals = vitals;
		_playerDowned = playerDowned;
		_treasurePickup = treasurePickup;
		_trapService = trapService;
		_experience = experience;
		_attackRollAbilityOverlay = attackRollAbilityOverlay;
		_lifecycle = new CombatEncounterLifecycle(_narrative);
		_monsterTurn = new CombatMonsterTurn(_resolution, _dice, _narrative, _vitals, _playerDowned);
		_initiative = new CombatInitiative(_dice, _narrative, dicePresenterHost);
		_turnLoop = new CombatTurnLoop(this, _lifecycle, _monsterTurn);
		_potionEffects = potionEffects;
		RegisterCombatAbilityHandlers();
	}

	private async Task RunAfterSuccessfulCombatHealthPotionAsync(GameSessionState session)
	{
		var room = session.Dungeon.CurrentRoom;
		if (room != null && RoomFeatureHelper.GetFeature<MonsterFeature>(room) is { } monsterFeature)
		{
			_turnLoop.PruneDeadMonstersFromTurnOrder(session, monsterFeature);
			if (_turnLoop.CheckVictory(session, monsterFeature))
				return;
		}

		_turnLoop.AdvanceTurn(session);
		await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
	}

	private void RegisterCombatAbilityHandlers()
	{
		_combatAbilities.Register(new DefendCombatAbilityHandler(_narrative, IsPlayerTurn));
	}

	public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
		TryBeginCombatIfHostileAsync(session, previousCoord, floorLevel).GetAwaiter().GetResult();

	public async Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel)
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
		if (_isResolvingAction)
			return false;

		_isResolvingAction = true;
		try
		{

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

			var order = await _initiative.RollInitiativeOrderAsync(session, feature);
			session.Combat.TurnOrder = order;
			session.AppendGameLog(_narrative.ForCombatTurnOrderSummary(_initiative.BuildTurnOrderNames(feature, order)));

			await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
			return true;
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public bool IsAwaitingPlayerAction(GameSessionState session)
		=> !_isResolvingAction && IsPlayerTurn(session);

	bool ICombatTurnReadiness.IsAwaitingPlayerAction(GameSessionState session) => IsPlayerTurn(session);

	private static bool IsPlayerTurn(GameSessionState session)
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

	public void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) =>
		ExecutePlayerAttackAsync(session, livingMonsterOrdinal, attackChoice).GetAwaiter().GetResult();

	public async Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		_isResolvingAction = true;
		try
		{
		var room = session.Dungeon.CurrentRoom!;
		if (!MainViewRoomSlots.TryGetLivingMonsterByOrdinal(room, livingMonsterOrdinal, out var feature, out var targetIndex) ||
		    feature == null || targetIndex < 0)
		{
			session.AppendGameLog("There is nothing you can attack.");
			return;
		}

		var monster = feature.Monsters[targetIndex];
		if (monster.CurrentHp <= 0)
			return;
		var monsterWasAlive = monster.CurrentHp > 0;

		var resolvedChoice = attackChoice;
		if (!resolvedChoice.IsUnarmed)
		{
			var inv = session.Player.InventoryState;
			if (!inv.EquippedBySlot.TryGetValue(resolvedChoice.WeaponSlotIfAny!.Value, out var inst) ||
			    inst == null || inst.Definition is not WeaponDefinition)
			{
				session.AppendGameLog("You have nothing to attack with in that hand; you strike unarmed instead.");
				resolvedChoice = PlayerAttackChoice.Unarmed;
			}
		}

		AttackDefinition attack;
		WeaponDefinition? weapon = null;
		if (resolvedChoice.IsUnarmed)
			attack = DefaultUnarmedAttackDefinition.Create();
		else
		{
			weapon = (WeaponDefinition)session.Player.InventoryState.EquippedBySlot[resolvedChoice.WeaponSlotIfAny!.Value]!.Definition;
			if (weapon.Attacks.Count == 0)
			{
				session.AppendGameLog("That weapon has no attacks configured; you cannot strike with it.");
				return;
			}

			attack = weapon.Attacks[0];
		}

		var attackLabel = resolvedChoice.IsUnarmed
			? $"{attack.Name} vs {monster.Definition.Name}"
			: $"{weapon!.Name} vs {monster.Definition.Name}";

		var rollInput = new PlayerAttackRollInput
		{
			Session = session,
			Attack = attack,
			Weapon = weapon,
			AbilityOverlay = _attackRollAbilityOverlay,
		};
		var req = PlayerAttackRollBuilder.BuildToHitRequest(rollInput, attackLabel, monster.Definition.Defense);
		var result = await _resolution.RollAgainstTargetAsync(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForAttackRoll("You", monster.Definition.Name, result.Roll.Total, monster.Definition.Defense, result.Roll.DetailText)
		});

		if (result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess)
		{
			var (damageAbility, addAbilityToDamage) = PlayerAttackRollBuilder.ResolveDamageAbility(rollInput);
			var damageRoll = PlayerAttackRollBuilder.RollDamage(
				new PlayerAttackDamageRollInput
				{
					Dice = _dice,
					Roll = rollInput,
					DamageAbility = damageAbility,
					AddAbilityToDamage = addAbilityToDamage,
				});
			await _resolution.PresentSpecsAsync(damageRoll.VisualDice);
			var dmg = damageRoll.Total;

			if (result.Outcome == ResolutionOutcome.CriticalSuccess)
				dmg *= 2;
			monster.CurrentHp -= dmg;
			session.AppendGameLog(_narrative.ForDamageDealt(monster.Definition.Name, dmg, monster.CurrentHp, damageRoll.Detail));
			if (monsterWasAlive && monster.CurrentHp <= 0)
			{
				_experience?.GrantExperience(session, monster.Definition.ExperienceReward);
				CombatCorpseHelper.SpawnCorpseOnMonsterDeath(session, room, monster.Definition, _items, _narrative);
			}
		}
		else
			session.AppendGameLog(_narrative.ForAttackMiss("You", monster.Definition.Name));

		_turnLoop.PruneDeadMonstersFromTurnOrder(session, feature);
		if (_turnLoop.CheckVictory(session, feature))
			return;

		_turnLoop.AdvanceTurn(session);
		await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void ExecutePlayerFlee(GameSessionState session) =>
		ExecutePlayerFleeAsync(session).GetAwaiter().GetResult();

	public async Task ExecutePlayerFleeAsync(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		_isResolvingAction = true;
		try
		{
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
		var result = await _resolution.RollAgainstTargetAsync(req);
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
		await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload) =>
		ExecutePlayerTakeTreasureAsync(session, payload).GetAwaiter().GetResult();

	public async Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		_isResolvingAction = true;
		try
		{
		var outcome = payload.Kind switch
		{
			TargetPayloadKind.TakeTreasureItem => _treasurePickup.TakeTreasureInstanceAtSlot(session,
				payload.TreasureFeatureOrdinal, payload.TreasureItemIndexInFeature),
			TargetPayloadKind.TakeAllEligibleTreasure => _treasurePickup.TakeAllEligibleFromCurrentRoom(session),
			_ => TakeTreasureOutcome.NothingToTake
		};
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
		await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void ExecutePlayerUseHealthPotion(GameSessionState session) =>
		ExecutePlayerUseHealthPotionAsync(session).GetAwaiter().GetResult();

	public async Task ExecutePlayerUseHealthPotionAsync(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		_isResolvingAction = true;
		try
		{

		var outcome = await _potionEffects.TryUseHealthPotionAsync(session);
		if (outcome == HealthPotionUseOutcome.Applied)
			await RunAfterSuccessfulCombatHealthPotionAsync(session);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload) =>
		ExecutePlayerDisarmTrapAsync(session, payload).GetAwaiter().GetResult();

	public async Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload)
	{
		if (!IsAwaitingPlayerAction(session))
			return;

		_isResolvingAction = true;
		try
		{

		var result = payload.Kind != TargetPayloadKind.DisarmTrapInstance
			? new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoTrapPresent }
			: await _trapService.TryDisarmAtSlotAsync(session, payload.TrapFeatureOrdinal, payload.TrapIndexInFeature);
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
		await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
		}
		finally
		{
			_isResolvingAction = false;
		}
	}

	public void ExecutePlayerDefend(GameSessionState session) =>
		ExecutePlayerDefendAsync(session).GetAwaiter().GetResult();

	public async Task ExecutePlayerDefendAsync(GameSessionState session)
	{
		if (!IsAwaitingPlayerAction(session))
			return;
		if (!session.Player.HasAbility(AbilityIds.Defend))
			return;

		_isResolvingAction = true;
		try
		{

		void Advance() => _turnLoop.AdvanceTurn(session);

		if (_combatAbilities.TryExecute(AbilityIds.Defend, session, Advance))
		{
			await _turnLoop.ProcessAutomaticMonsterTurnsAsync(session);
			return;
		}

		if (session.Combat is not { } c)
			return;
		if (c.HasDefendStanceActive())
			session.AppendGameLog(_narrative.ForDefendAlreadyDefending());
		else if (c.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend))
			session.AppendGameLog(_narrative.ForDefendOnCooldown());
		else
			session.AppendGameLog(_narrative.ForDefendCannotUse());
		}
		finally
		{
			_isResolvingAction = false;
		}
	}
}
