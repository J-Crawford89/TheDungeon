#nullable enable
using Godot;

/// <summary>A resolved physical die scene and the catalog-owned appearance applied to its body.</summary>
public sealed class DieVisualSelection
{
	public PackedScene VisualScene { get; set; } = null!;
	public Material? BodyMaterial { get; set; }
}
