using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
		=> ExecuteMonsterTurnAsync(session, feature, monsterIndex).GetAwaiter().GetResult();

	public async Task ExecuteMonsterTurnAsync(
		GameSessionState session,
		MonsterFeature feature,
		int monsterIndex,
		CancellationToken ct = default)
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

		var profRank = MonsterAttackProficiencyRank.Resolve(monster.Definition, attack);
		if ((int)profRank != 0)
			modifiers.Add(new ModifierWithSource { Modifier = (int)profRank, Source = "Proficiency" });

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
		var result = await _resolution.RollAgainstTargetAsync(
			req,
			DieRollVisualKind.Monster,
			DicePresentationProfile.Standard,
			ct);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForAttackRoll(name, "you", result.Roll.Total, playerEc, result.Roll.DetailText)
		});

		var hit = result.Outcome == ResolutionOutcome.Success || result.Outcome == ResolutionOutcome.CriticalSuccess;
		if (hit)
		{
			await _resolution.NotifyResolvedRollAsync(ct);

			var hitArmorBand = result.Roll.Total >= playerEc && result.Roll.Total < armorThreshold;
			var damageRoll = RollAttackDamageSum(attack);
			await _resolution.PresentSpecsAsync(damageRoll.VisualDice, DicePresentationProfile.Standard, ct);
			var damage = damageRoll.Total;
			if (attack.AddAbilityScoreToDamage)
				damage += monster.Definition.AbilityScores.GetScore(attack.AbilityScore);
			damage = Math.Max(0, damage);
			if (result.Outcome == ResolutionOutcome.CriticalSuccess)
				damage *= 2;
			// Preserve existing defend ordering: it can fully negate damage before any armor/DR math.
			if (CombatPlayerIncomingDamage.TryApplyDefendNegate(ref damage, session, _narrative))
			{
				await _resolution.NotifyResolvedRollAsync(ct);
				return;
			}
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
			await _resolution.NotifyResolvedRollAsync(ct);
		}
		else
		{
			session.AppendGameLog(_narrative.ForAttackMiss(name, "you"));
			await _resolution.NotifyResolvedRollAsync(ct);
		}
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

	private MonsterDamageRollResult RollAttackDamageSum(AttackDefinition attack)
	{
		var sum = 0;
		var visualDice = new List<PhysicalDieRollSpec>();
		foreach (var damage in attack.DamageComponents)
		{
			var rolled = RollAttackDamageOne(damage);
			sum += rolled.Total;
			visualDice.AddRange(rolled.VisualDice);
		}
		return new MonsterDamageRollResult(sum, visualDice);
	}

	private MonsterDamageRollResult RollAttackDamageOne(DamageComponent damage)
	{
		var request = new DiceRollRequest
		{
			DiceRollLabel = "Monster damage",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.None,
			DiceExpressions = new List<DiceExpression> { damage.DamageDice },
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = damage.FlatAmount, Source = "Flat" }
			}
		};
		var damageRoll = _diceRolls.Roll(request);
		var specs = PhysicalDieRollExtractor.FromDiceRollResult(damageRoll, request, DieRollVisualKind.Monster);
		return new MonsterDamageRollResult(Math.Max(0, damageRoll.Total), specs);
	}

	private sealed record MonsterDamageRollResult(int Total, IReadOnlyList<PhysicalDieRollSpec> VisualDice);
}
