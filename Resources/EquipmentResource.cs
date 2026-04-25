#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EquipmentResource : ItemResource
{
	[Export] public Array<EquipmentSlot> Slots { get; set; } = [];
}
