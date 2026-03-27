using System.Collections.Generic;

public sealed class TreasureFeature : RoomFeature
{
	public List<TreasureInstance> TreasureItems { get; set; } = new();
	public bool RemoveFeatureWhenEmpty { get; set; } = true;
}