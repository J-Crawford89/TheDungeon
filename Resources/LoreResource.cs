using Godot;

[GlobalClass]
public partial class LoreResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public string Description { get; set; } = string.Empty;
}
