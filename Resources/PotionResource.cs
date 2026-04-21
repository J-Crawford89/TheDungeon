using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PotionResource : ItemResource
{
	[Export] public bool ConsumedOnUse { get; set; } = true;

	[Export] public Array<RestoreHealthEffectResource> RestoreHealthEffects { get; set; } = [];
}
