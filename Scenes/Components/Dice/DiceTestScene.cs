using Godot;
using System;

public partial class DiceTestScene : Node3D
{
	[Export] public RollingDie Die { get; set; } = null!;
	
	public override async void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent &&
			keyEvent.Pressed &&
			!keyEvent.Echo &&
			keyEvent.Keycode == Key.Space)
			{
				await Die.RollAsync();
			}
	}
}
