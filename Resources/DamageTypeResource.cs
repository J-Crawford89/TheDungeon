#nullable enable
using Godot;

[GlobalClass]
public partial class DamageTypeResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public DamageFamily Family { get; set; } = DamageFamily.Physical;
}
