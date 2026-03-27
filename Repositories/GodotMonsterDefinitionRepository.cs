#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotMonsterDefinitionRepository : IMonsterDefinitionRepository
{
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
				Attack = 1,
				Defense = 12,
				RandomizerWeight = 50
			},
			new MonsterDefinition
			{
				Id = "giant_rat",
				Name = "Giant Rat",
				MaxHp = 20,
				Attack = 4,
				Defense = 10,
				RandomizerWeight = 20
			},
			new MonsterDefinition
			{
				Id = "rat_king",
				Name = "Rat King",
				MaxHp = 32,
				Attack = 6,
				Defense = 13,
				IsBoss = true,
				RandomizerWeight = 5
			}
		};
}
