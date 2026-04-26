#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WeaponResource : EquipmentResource
{
	[Export] public Array<DamageComponentResource> DamageComponents { get; set; } = [];
	[Export] public WeaponCategoryResource Category { get; set; } = new();
	[Export] public WeaponSubCategoryResource SubCategory { get; set; } = new();
	[Export] public WeaponGroupResource Group { get; set; } = new();
	[Export] public WeaponSubGroupResource SubGroup { get; set; } = new();
	[Export] public ProficiencyRank OwnershipProficiencyRank { get; set; } = ProficiencyRank.Untrained;
}
