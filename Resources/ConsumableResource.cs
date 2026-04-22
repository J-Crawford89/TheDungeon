using Godot;

[GlobalClass]
public partial class ConsumableResource : ItemResource
{
    [Export] public bool ConsumedOnUse { get; set; } = true;
}