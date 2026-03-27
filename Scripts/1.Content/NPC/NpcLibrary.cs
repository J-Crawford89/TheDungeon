using System.Collections.Generic;

public static class NpcLibrary
{
	public static readonly NpcDefinition WoundedTraveler = new()
	{
		Id = "wounded_traveler",
		Name = "A wounded traveler"
	};

	public static IReadOnlyList<NpcDefinition> All => new[] { WoundedTraveler };
}
