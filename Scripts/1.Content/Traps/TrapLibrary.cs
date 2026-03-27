using System.Collections.Generic;

public static class TrapLibrary
{
	public static readonly TrapDefinition RustyNeedle = new()
	{
		Id = "rusty_needle",
		Name = "Rusty needle trap",
		DiscoverDc = 12,
		DisarmDc = 14,
		Damage = 3,
		Effect = "A spring-loaded needle jabs out."
	};

	public static IReadOnlyList<TrapDefinition> All => new[] { RustyNeedle };
}
