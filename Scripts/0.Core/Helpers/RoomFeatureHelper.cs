using System.Collections.Generic;
using System.Linq;

public static class RoomFeatureHelper
{
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

