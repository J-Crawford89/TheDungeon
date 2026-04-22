using System.Collections.Generic;
using System.Linq;

public static class RoomFeatureHelper
{
	/// <summary>First trap feature in <paramref name="room"/>'s <see cref="DungeonRoom.Features"/> list order.</summary>
	public static TrapFeature? GetFirstTrapFeatureOrdered(DungeonRoom room)
	{
		foreach (var feature in room.Features)
		{
			if (feature is TrapFeature trapFeature)
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
}

