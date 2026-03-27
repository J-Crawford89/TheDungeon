using System.Collections.Generic;

public static class LoreLibrary
{
	public static readonly LoreDefinition CrackedPlaque = new()
	{
		Id = "cracked_plaque",
		Name = "Cracked stone plaque",
		Description = "Weathered runes you can barely read."
	};

	public static IReadOnlyList<LoreDefinition> All => new[] { CrackedPlaque };
}
