using System.Collections.Generic;

public sealed class LoreFeature : RoomFeature
{
    public List<LoreInstance> Lore { get; set; } = new();
}