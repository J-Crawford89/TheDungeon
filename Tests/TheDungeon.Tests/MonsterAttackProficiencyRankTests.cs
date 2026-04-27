using Xunit;

public sealed class MonsterAttackProficiencyRankTests
{
	[Fact]
	public void Resolve_WhenAttackUntrained_UsesMonsterDefault()
	{
		var def = new MonsterDefinition { DefaultAttackProficiencyRank = ProficiencyRank.Expert };
		var attack = new MonsterAttackDefinition
		{
			AttackProficiencyRank = ProficiencyRank.Untrained,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					new DamageTypeDefinition("p", "P", DamageFamily.Physical)),
			],
		};

		Assert.Equal(ProficiencyRank.Expert, MonsterAttackProficiencyRank.Resolve(def, attack));
	}

	[Fact]
	public void Resolve_WhenAttackHasRank_UsesAttackRank()
	{
		var def = new MonsterDefinition { DefaultAttackProficiencyRank = ProficiencyRank.Expert };
		var attack = new MonsterAttackDefinition
		{
			AttackProficiencyRank = ProficiencyRank.Trained,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					new DamageTypeDefinition("p", "P", DamageFamily.Physical)),
			],
		};

		Assert.Equal(ProficiencyRank.Trained, MonsterAttackProficiencyRank.Resolve(def, attack));
	}

	[Fact]
	public void Resolve_LegacyAttackDefinition_UsesMonsterDefaultOnly()
	{
		var def = new MonsterDefinition { DefaultAttackProficiencyRank = ProficiencyRank.Trained };
		var attack = new AttackDefinition
		{
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					new DamageTypeDefinition("p", "P", DamageFamily.Physical)),
			],
		};

		Assert.Equal(ProficiencyRank.Trained, MonsterAttackProficiencyRank.Resolve(def, attack));
	}
}
