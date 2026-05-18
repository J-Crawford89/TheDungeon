using Godot;
using System;

public partial class DiceRollOverlay : Control
{
	[Export] public PackedScene RollingDieScene { get; set; }
	[Export] public NodePath DiceSpawnPath { get; set; }
}
