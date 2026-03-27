using System.Collections.Generic;

public sealed class TrapFeature : RoomFeature
{
    public List<TrapInstance> Traps { get; set; } = new();
}