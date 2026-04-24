#nullable enable
using Godot;

[GlobalClass]
public partial class TrapResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int DiscoverDc { get; set; }
	[Export] public int DisarmDc { get; set; }
	[Export] public int Damage { get; set; }
	[Export] public string Effect { get; set; } = string.Empty;
	[Export] public bool IsRemovedAfterTripped { get; set; } = true;

	[Export] public Texture2D? Icon { get; set; }
}
