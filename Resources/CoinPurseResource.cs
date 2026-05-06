#nullable enable
using Godot;

/// <summary>Authoring sub-resource for <see cref="TreasureResource"/> and character backgrounds.</summary>
[GlobalClass]
public partial class CoinPurseResource : Resource
{
	[Export] public int Copper { get; set; }
	[Export] public int Silver { get; set; }
	[Export] public int Gold { get; set; }
	[Export] public int Platinum { get; set; }
}
