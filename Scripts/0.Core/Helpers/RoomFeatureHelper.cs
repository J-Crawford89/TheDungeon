using System.Collections.Generic;
using System.Linq;

public static class RoomFeatureHelper
{
	/// <summary>First trap feature in list order that has at least one <see cref="TrapInstance.IsRevealed"/> trap.</summary>
	public static TrapFeature? GetFirstTrapFeatureOrdered(DungeonRoom room)
	{
		foreach (var feature in room.Features)
		{
			if (feature is TrapFeature trapFeature && trapFeature.Traps.Any(t => t.IsRevealed))
				return trapFeature;
		}

		return null;
	}

	public static TFeature? GetFeature<TFeature>(DungeonRoom room) where TFeature : RoomFeature
	{
		return room.Features.OfType<TFeature>().FirstOrDefault();
	}

	public static IEnumerable<TFeature> GetFeatures<TFeature>(DungeonRoom room) where TFeature : RoomFeature
	{
		return room.Features.OfType<TFeature>();
	}

	public static bool HasFeature<TFeature>(DungeonRoom room) where TFeature : RoomFeature
	{
		return room.Features.OfType<TFeature>().Any();
	}

	public static int FirstLivingMonsterIndex(MonsterFeature feature)
	{
		for (var i = 0; i < feature.Monsters.Count; i++)
		{
			if (feature.Monsters[i].CurrentHp > 0)
				return i;
		}

		return -1;
	}
}
