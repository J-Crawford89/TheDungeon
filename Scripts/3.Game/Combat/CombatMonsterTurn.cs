using System;
using System.Collections.Generic;

public sealed class CombatMonsterTurn
{
	private readonly ResolutionService _resolution;
	private readonly IDiceRollRequestExecutor _diceRolls;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly PlayerDownedResolutionService _playerDowned;

	public CombatMonsterTurn(
		ResolutionService resolution,
		IDiceRollRequestExecutor diceRolls,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		PlayerDownedResolutionService playerDowned)
	{
		_resolution = resolution;
		_diceRolls = diceRolls;
		_narrative = narrative;
		_vitals = vitals;
		_playerDowned = playerDowned;
	}

	public void ExecuteMonsterTurn(GameSessionState session, MonsterFeature feature, int monsterIndex)
	{
		if (monsterIndex < 0 || monsterIndex >= feature.Monsters.Count)
			return;
		var monster = feature.Monsters[monsterIndex];
		if (monster.CurrentHp <= 0)
			return;
		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(session.Player);

		var name = monster.Definition.Name;
		var attack = SelectAttack(session, monster.Definition, name);
		if (attack == null || attack.DamageComponents == null || attack.DamageComponents.Count == 0)
			return;

		var abilityMod = monster.Definition.AbilityScores.GetScore(attack.AbilityScore);
		var modifiers = new List<ModifierWithSource>();
		if (abilityMod != 0)
			modifiers.Add(new ModifierWithSource { Modifier = abilityMod, Source = attack.AbilityScore.ToString() });
		if (attack.AttackModifier != 0)
			modifiers.Add(new ModifierWithSource { Modifier = attack.AttackModifier, Source = "Attack" });

		var effectiveAgility = CombatFormulas.PlayerEffectiveAgility(
			session.Player.AbilityScores.Agility,
			session.Player.TotalAgilityPenalty);
		var playerEc = CombatFormulas.PlayerEvasionClass(effectiveAgility);
		var armorThreshold = CombatFormulas.PlayerArmorThreshold(playerEc, session.Player.TotalArmorBonus);
		var req = new DiceRollRequest
		{
			DiceRollLabel = $"{name}: {attack.Name}",
			TargetNumber = playerEc,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true }
			},
			ModifiersWithSources = modifiers,
		};
		var result = _resolution.RollAgainstTarget(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForAttackRoll(name, "you", result.Roll.Total, playerEc, result.Roll.DetailText)
		});

		if (result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess)
		{
			var hitArmorBand = result.Roll.Total >= playerEc && result.Roll.Total < armorThreshold;
			var damage = RollAttackDamageSum(attack);
			if (attack.AddAbilityScoreToDamage)
				damage += monster.Definition.AbilityScores.GetScore(attack.AbilityScore);
			damage = Math.Max(0, damage);
			if (result.Outcome == ResolutionOutcome.CriticalSuccess)
				damage *= 2;
			// Preserve existing defend ordering: it can fully negate damage before any armor/DR math.
			if (CombatPlayerIncomingDamage.TryApplyDefendNegate(ref damage, session, _narrative))
				return;
			var rolledDamage = damage;
			var reducedByArmor = 0;
			if (hitArmorBand)
			{
				var primaryType = attack.DamageComponents[0].DamageType;
				var totalReduction = PlayerDamageReductionHelper.TotalDamageReductionFor(session.Player, primaryType);
				if (totalReduction != 0)
				{
					var reduced = Math.Max(0, damage - totalReduction);
					reducedByArmor = damage - reduced;
					damage = reduced;
				}
			}

			var source = new PlayerDamageSource
			{
				Type = DamageSourceType.Monster,
				DisplayName = name,
				DefinitionId = monster.Definition.Id,
			};
			var vitals = _vitals.ApplyDamage(session.Player, damage);
			if (hitArmorBand)
				session.AppendGameLog(_narrative.ForMonsterHitArmor(name, rolledDamage, reducedByArmor, damage, vitals.HpAfterClamped));
			else
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

	private AttackDefinition? SelectAttack(GameSessionState session, MonsterDefinition definition, string monsterName)
	{
		for (var i = 0; i < definition.Attacks.Count; i++)
		{
			var attack = definition.Attacks[i];
			if (attack?.DamageComponents == null || attack.DamageComponents.Count == 0)
				continue;
			return attack;
		}

		var monsterId = string.IsNullOrWhiteSpace(definition.Id) ? "<unknown>" : definition.Id;
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Debug,
			Text = $"CombatMonsterTurn: monster '{monsterId}' has no valid attacks configured."
		});
		session.AppendGameLog($"{monsterName} hesitates and does not attack.");
		return null;
	}

	private int RollAttackDamageSum(AttackDefinition attack)
	{
		var sum = 0;
		foreach (var damage in attack.DamageComponents)
			sum += RollAttackDamageOne(damage);
		return sum;
	}

	private int RollAttackDamageOne(DamageComponent damage)
	{
		var damageRoll = _diceRolls.Roll(new DiceRollRequest
		{
			DiceRollLabel = "Monster damage",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.None,
			DiceExpressions = new List<DiceExpression> { damage.DamageDice },
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = damage.FlatAmount, Source = "Flat" }
			}
		});
		return Math.Max(0, damageRoll.Total);
	}
}
