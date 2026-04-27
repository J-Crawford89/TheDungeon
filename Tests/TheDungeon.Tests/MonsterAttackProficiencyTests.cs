using System.Collections.Generic;
using Xunit;

public sealed class MonsterAttackProficiencyTests
{
	[Fact]
	public void Resolve_WithoutOverride_UsesUnarmedResolver()
	{
		var def = new MonsterDefinition
		{
			Proficiencies = new Dictionary<ProficiencyKey, ProficiencyRank>
			{
				[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Expert,
			},
		};
		var attack = new AttackDefinition
		{
			Id = "claw",
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					new DamageTypeDefinition("p", "P", DamageFamily.Physical)),
			],
		};

		var r = MonsterAttackProficiency.Resolve(def, attack);

		Assert.Equal(ProficiencyRank.Expert, r.Rank);
	}

	[Fact]
	public void Resolve_WithOverride_UsesOnlyThatKey()
	{
		var def = new MonsterDefinition
		{
			Proficiencies = new Dictionary<ProficiencyKey, ProficiencyRank>
			{
				[new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All)] = ProficiencyRank.Expert,
				[new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blade")] = ProficiencyRank.Trained,
			},
		};
		var attack = new AttackDefinition
		{
			Id = "slash",
			ProficiencyLookupOverride = new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blade"),
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
					0,
					new DamageTypeDefinition("p", "P", DamageFamily.Physical)),
			],
		};

		var r = MonsterAttackProficiency.Resolve(def, attack);

		Assert.Equal(ProficiencyRank.Trained, r.Rank);
	}
}
