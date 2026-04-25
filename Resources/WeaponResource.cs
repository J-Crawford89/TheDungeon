#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WeaponResource : EquipmentResource
{
	[Export] public Array<DamageComponentResource> DamageComponents { get; set; } = [];
}
