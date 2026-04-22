public sealed class TrapDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DiscoverDc { get; set; }
    public int DisarmDc { get; set; }
    public int Damage { get; set; }
    public string Effect { get; set; }

    /// <summary>If true, after the trap fires (failed disarm or leaving room), the trap instance is removed from the room.</summary>
    public bool IsRemovedAfterTripped { get; set; } = true;
}