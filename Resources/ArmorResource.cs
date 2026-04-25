#nullable enable
using Godot;

[GlobalClass]
public partial class ArmorResource : EquipmentResource
{
	[Export] public int ArmorBonus { get; set; }
	[Export] public int AgilityPenalty { get; set; }
}
