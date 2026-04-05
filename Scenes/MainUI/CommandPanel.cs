using Godot;
using System;

public partial class CommandPanel : PanelContainer
{
	[Export] private Button _forwardButton;
	[Export] private Button _backwardButton;
	[Export] private Button _leftButton;
	[Export] private Button _rightButton;
	[Export] private Button _inspectButton;
	[Export] private Button _floorUpButton;
	[Export] private Button _floorDownButton;

	[Export] private Button _attackButton;
	[Export] private Button _fleeButton;
	[Export] private Button _takeButton;
	[Export] private Button _potionButton;

	public event Action? ForwardPressed;
	public event Action? BackwardPressed;
	public event Action? LeftPressed;
	public event Action? RightPressed;
	public event Action? InspectPressed;
	public event Action? FloorUpPressed;
	public event Action? FloorDownPressed;
	public event Action? AttackPressed;
	public event Action? FleePressed;
	public event Action? TakePressed;
	public event Action? PotionPressed;

	public override void _Ready()
	{
		_forwardButton.Pressed += () => ForwardPressed?.Invoke();
		_backwardButton.Pressed += () => BackwardPressed?.Invoke();
		_leftButton.Pressed += () => LeftPressed?.Invoke();
		_rightButton.Pressed += () => RightPressed?.Invoke();
		_inspectButton.Pressed += () => InspectPressed?.Invoke();
		_floorUpButton.Pressed += () => FloorUpPressed?.Invoke();
		_floorDownButton.Pressed += () => FloorDownPressed?.Invoke();
		_attackButton.Pressed += () => AttackPressed?.Invoke();
		_fleeButton.Pressed += () => FleePressed?.Invoke();
		_takeButton.Pressed += () => TakePressed?.Invoke();
		_potionButton.Pressed += () => PotionPressed?.Invoke();
	}

	public void ApplyDungeonMode(DungeonMode mode)
	{
		var exploration = mode == DungeonMode.Exploration;
		_forwardButton.Visible = exploration;
		_backwardButton.Visible = exploration;
		_leftButton.Visible = exploration;
		_rightButton.Visible = exploration;
		_inspectButton.Visible = exploration;

		_floorUpButton.Visible = false;
		_floorDownButton.Visible = false;

		var combat = mode == DungeonMode.Combat;

		_attackButton.Visible = combat;
		_fleeButton.Visible = combat;
		_potionButton.Visible = combat;
	}

	public void ApplyTakeButtonVisible(bool visible)
	{
		_takeButton.Visible = visible;
	}

	public void SetAllCommandButtonsDisabled(bool disabled)
	{
		_forwardButton.Disabled = disabled;
		_backwardButton.Disabled = disabled;
		_leftButton.Disabled = disabled;
		_rightButton.Disabled = disabled;
		_inspectButton.Disabled = disabled;
		_floorUpButton.Disabled = disabled;
		_floorDownButton.Disabled = disabled;
		_attackButton.Disabled = disabled;
		_fleeButton.Disabled = disabled;
		_takeButton.Disabled = disabled;
		_potionButton.Disabled = disabled;
	}

	public void HideAllGameplayCommands()
	{
		_forwardButton.Visible = false;
		_backwardButton.Visible = false;
		_leftButton.Visible = false;
		_rightButton.Visible = false;
		_inspectButton.Visible = false;
		_floorUpButton.Visible = false;
		_floorDownButton.Visible = false;
		_attackButton.Visible = false;
		_fleeButton.Visible = false;
		_takeButton.Visible = false;
		_potionButton.Visible = false;
	}

	public void ApplyCombatItemButtons(PlayerState player)
	{
		if (_potionButton == null)
			return;
		_potionButton.Disabled = player.HealthPotionCount <= 0 || player.CurrentHp >= player.MaxHp;
	}

	public void RenderFloorExitButtons(DungeonRoom? currentRoom)
	{
		_floorUpButton.Visible = false;
		_floorDownButton.Visible = false;

		if (currentRoom == null)
			return;

		var floorExitFeature = RoomFeatureHelper.GetFeature<FloorExitFeature>(currentRoom);
		if (floorExitFeature == null)
			return;

		switch (DirectionHelper.GetVerticalExitDirection(currentRoom.Position))
		{
			case VerticalDirection.Up:
				_floorUpButton.Visible = true;
				break;
			case VerticalDirection.Down:
				_floorDownButton.Visible = true;
				break;
		}
	}
}
