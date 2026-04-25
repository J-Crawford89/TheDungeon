#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EquipmentResource : ItemResource
{
	[Export] public Array<EquipmentSlot> Slots { get; set; } = [];

	/// <summary>When set, the item occupies every listed slot at once (e.g. main hand + off hand for two-handed weapons).</summary>
	[Export] public Array<EquipmentSlot> OccupiedSlots { get; set; } = [];
}
