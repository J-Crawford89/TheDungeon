using System.Collections.Generic;

/// <summary>Room feature that holds loot rows until the player explicitly takes them.</summary>
public abstract class ContainerFeature : RoomFeature
{
	public List<LootableItemDefinition> Contents { get; set; } = new();

	/// <summary>When true, the feature is removed from the room once <see cref="Contents"/> is empty.</summary>
	public bool RemoveFeatureWhenEmpty { get; set; } = true;
}
