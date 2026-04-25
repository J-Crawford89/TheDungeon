#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotMonsterDefinitionRepository : IMonsterDefinitionRepository
{
	private static readonly DamageTypeDefinition DefaultPhysicalDamageType =
		new("monster.default.physical", "Physical", DamageFamily.Physical);

	private readonly IReadOnlyList<MonsterDefinition> _all;

	public GodotMonsterDefinitionRepository(MonsterResourceDatabase? database)
	{
		if (database?.Monsters != null && database.Monsters.Count > 0)
			_all = database.Monsters.Select(MonsterMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<MonsterDefinition> All => _all;

	private static IReadOnlyList<MonsterDefinition> DefaultAll() =>
		new[]
		{
			new MonsterDefinition
			{
				Id = "rat",
				Name = "Rat",
				MaxHp = 2,
				Attacks = [CreateDefaultAttack("Bite", 1)],
				Defense = 12,
				ExperienceReward = 0,
				RandomizerWeight = 50
			},
			new MonsterDefinition
			{
				Id = "giant_rat",
				Name = "Giant Rat",
				MaxHp = 20,
				Attacks = [CreateDefaultAttack("Bite", 4)],
				Defense = 10,
				ExperienceReward = 0,
				RandomizerWeight = 20
			},
			new MonsterDefinition
			{
				Id = "rat_king",
				Name = "Rat King",
				MaxHp = 32,
				Attacks = [CreateDefaultAttack("Royal Bite", 6)],
				Defense = 13,
				ExperienceReward = 0,
				IsBoss = true,
				RandomizerWeight = 5
			}
		};

	private static AttackDefinition CreateDefaultAttack(string name, int amount) =>
		new()
		{
			Name = name,
			AttackModifier = amount,
			AttackItem = null,
			Damage = new DamageComponent(
				new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false },
				amount,
				DefaultPhysicalDamageType)
		};
}
