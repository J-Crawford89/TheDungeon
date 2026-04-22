using Godot;

[GlobalClass]
public partial class NpcResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int DiscoverDc { get; set; }
}
