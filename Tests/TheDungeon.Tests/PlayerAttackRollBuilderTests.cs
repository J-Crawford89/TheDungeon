using System.Collections.Generic;
using Xunit;

public sealed class PlayerAttackRollBuilderTests
{
	private sealed class AgilityOverlay : IAttackRollAbilityOverlay
	{
		public void Apply(AttackRollResolutionContext context, ref AbilityScore hitAbility, ref bool addAbilityToDamage) =>
			hitAbility = AbilityScore.Agility;
	}

	private static PlayerAttackRollInput RollInput(AttackDefinition attack, WeaponDefinition? weapon, IAttackRollAbilityOverlay? overlay) =>
		new()
		{
			Attack = attack,
			Weapon = weapon,
			AbilityOverlay = overlay,
		};

	[Fact]
	public void BuildToHitRequest_IncludesAbilityProficiencyAndAttackModifierInOrder()
	{
		var physical = new DamageTypeDefinition("p", "Physical", DamageFamily.Physical);
		var session = new GameSessionState();
		session.Player.AbilityScores = new AbilityScores { Might = 3, Agility = 1 };
		session.Player.Proficiencies = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.Weapon, "w1")] = ProficiencyRank.Trained,
		};
		var weapon = new WeaponDefinition { Id = "w1", Name = "Test Blade" };
		var attack = new AttackDefinition
		{
			Name = "Slash",
			AbilityScore = AbilityScore.Might,
			AttackModifier = 2,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					physical),
			],
		};

		var req = PlayerAttackRollBuilder.BuildToHitRequest(RollInput(attack, weapon, null), session.Player, "roll", 12);

		Assert.Equal(12, req.TargetNumber);
		Assert.Equal(3, req.ModifiersWithSources.Count);
		Assert.Equal(3, req.ModifiersWithSources[0].Modifier);
		Assert.Equal("Might", req.ModifiersWithSources[0].Source);
		Assert.Equal(2, req.ModifiersWithSources[1].Modifier);
		Assert.Contains("Test Blade", req.ModifiersWithSources[1].Source, System.StringComparison.Ordinal);
		Assert.Equal(2, req.ModifiersWithSources[2].Modifier);
		Assert.Equal("Attack", req.ModifiersWithSources[2].Source);
	}

	[Fact]
	public void BuildToHitRequest_OverlayCanSwapHitAbility()
	{
		var physical = new DamageTypeDefinition("p", "Physical", DamageFamily.Physical);
		var session = new GameSessionState();
		session.Player.AbilityScores = new AbilityScores { Might = 1, Agility = 5 };
		var weapon = new WeaponDefinition { Id = "w1", Name = "Knife" };
		var attack = new AttackDefinition
		{
			AbilityScore = AbilityScore.Might,
			AttackModifier = 0,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					physical),
			],
		};

		var req = PlayerAttackRollBuilder.BuildToHitRequest(RollInput(attack, weapon, new AgilityOverlay()), session.Player, "roll", 10);

		Assert.Single(req.ModifiersWithSources);
		Assert.Equal(5, req.ModifiersWithSources[0].Modifier);
		Assert.Equal("Agility", req.ModifiersWithSources[0].Source);
	}

	[Fact]
	public void BuildToHitRequest_Unarmed_AllUnarmedGrant_AddsProficiencyModifier()
	{
		var physical = new DamageTypeDefinition("p", "Physical", DamageFamily.Physical);
		var session = new GameSessionState();
		session.Player.Proficiencies = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Trained,
		};
		var attack = new AttackDefinition
		{
			Id = "test_strike",
			AbilityScore = AbilityScore.Might,
			AttackModifier = 0,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					physical),
			],
		};

		var req = PlayerAttackRollBuilder.BuildToHitRequest(RollInput(attack, null, null), session.Player, "roll", 10);

		Assert.Contains(
			req.ModifiersWithSources,
			m => m.Modifier == (int)ProficiencyRank.Trained && m.Source.Contains("Unarmed", System.StringComparison.Ordinal));
	}

	[Fact]
	public void RollDamageTotal_WhenAddAbilityToDamageFalse_OmitsAbilityFromSum()
	{
		var dice = new DiceRollService(new System.Random(42));
		var physical = new DamageTypeDefinition("p", "Physical", DamageFamily.Physical);
		var session = new GameSessionState();
		session.Player.AbilityScores = new AbilityScores { Might = 10 };
		var attack = new AttackDefinition
		{
			AbilityScore = AbilityScore.Might,
			AddAbilityScoreToDamage = false,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					physical),
			],
		};

		var rollInput = RollInput(attack, null, null);
		var total = PlayerAttackRollBuilder.RollDamageTotal(
			new PlayerAttackDamageRollInput
			{
				Dice = dice,
				Roll = rollInput,
				DamageAbility = AbilityScore.Might,
				AddAbilityToDamage = false,
			},
			session.Player,
			out _);

		Assert.InRange(total, 1, 4);
	}
}
