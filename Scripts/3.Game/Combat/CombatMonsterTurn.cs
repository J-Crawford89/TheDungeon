using System.Collections.Generic;

public sealed class CombatMonsterTurn
{
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly PlayerDownedResolutionService _playerDowned;

	public CombatMonsterTurn(
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		PlayerDownedResolutionService playerDowned)
	{
		_resolution = resolution;
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
}
