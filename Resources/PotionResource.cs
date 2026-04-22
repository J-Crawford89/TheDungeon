using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PotionResource : ConsumableResource
{
	[Export] public Array<RestoreHealthEffectResource> RestoreHealthEffects { get; set; } = [];
}
