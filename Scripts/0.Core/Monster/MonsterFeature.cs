using System.Collections.Generic;

public sealed class MonsterFeature : RoomFeature
{
    public List<MonsterInstance> Monsters { get; set; } = new();
}