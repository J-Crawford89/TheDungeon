#nullable enable
using Godot;

[GlobalClass]
public partial class AttackResource : Resource
{
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int AttackModifier { get; set; }
	[Export] public ItemResource? AttackItem { get; set; }
	[Export] public DamageComponentResource? Damage { get; set; }
}
