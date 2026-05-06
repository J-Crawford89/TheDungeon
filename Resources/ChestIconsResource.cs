#nullable enable
using Godot;

/// <summary>Main-view textures for chest and salvage container slots (corpse art is per-monster on <see cref="MonsterResource"/>).</summary>
[GlobalClass]
public partial class ChestIconsResource : Resource
{
	[Export] public Texture2D? ChestIcon { get; set; }

	[Export] public Texture2D? ChestOpenedIcon { get; set; }

	[Export] public Texture2D? ChestLockedIcon { get; set; }

	[Export] public Texture2D? SalvageIcon { get; set; }

	[Export] public Texture2D? SalvageOpenedIcon { get; set; }

	[Export] public Texture2D? OtherContainerIcon { get; set; }
}
