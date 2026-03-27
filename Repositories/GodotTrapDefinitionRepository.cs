#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotTrapDefinitionRepository : ITrapDefinitionRepository
{
	private readonly IReadOnlyList<TrapDefinition> _all;

	public GodotTrapDefinitionRepository(TrapResourceDatabase? database)
	{
		if (database?.Traps != null && database.Traps.Count > 0)
			_all = database.Traps.Select(TrapMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<TrapDefinition> All => _all;

	private static IReadOnlyList<TrapDefinition> DefaultAll() =>
		new[]
		{
			new TrapDefinition
			{
				Id = "rusty_needle",
				Name = "Rusty needle trap",
				DiscoverDc = 12,
				DisarmDc = 14,
				Damage = 3,
				Effect = "A spring-loaded needle jabs out."
			}
		};
}
