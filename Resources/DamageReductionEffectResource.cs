#nullable enable
using Godot;

[GlobalClass]
public partial class DamageReductionEffectResource : EquippedItemEffectResource
{
	[Export] public DamageTypeResource? DamageType { get; set; }
	[Export] public DamageFamily DamageFamily { get; set; } = DamageFamily.None;
	[Export] public int ReductionAmount { get; set; }
}
