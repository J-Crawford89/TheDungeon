using System.Collections.Generic;

public sealed class NpcFeature : RoomFeature
{
    public List<NpcInstance> NPCs { get; set; } = new();
}