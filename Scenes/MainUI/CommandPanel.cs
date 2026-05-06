#nullable enable
using Godot;
using System;
using System.Collections.Generic;

public partial class CommandPanel : PanelContainer
{
	[Export] private Button _forwardButton = null!;
	[Export] private Button _backwardButton = null!;
	[Export] private Button _leftButton = null!;
	[Export] private Button _rightButton = null!;
	[Export] private Button _inspectButton = null!;
	[Export] private Button _floorUpButton = null!;
	[Export] private Button _floorDownButton = null!;

	[Export] private Button _attackButton = null!;
	[Export] private Button _fleeButton = null!;
	[Export] private Button _takeButton = null!;
	[Export] private Button? _openButton;
	[Export] private Button _potionButton = null!;
	[Export] private Button? _defendButton;
	[Export] private Button? _disarmButton;

	private GridContainer? _gameplayButtonGrid;
	private Control? _targetingButtonHost;

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
	public event Action? OpenPressed;
	public event Action? PotionPressed;
	public event Action? DefendPressed;
	public event Action? DisarmPressed;

	public event Action? TargetSelectCancelPressed;
	public event Action<int>? TargetSelectPicked;
	public event Action<int?>? TargetSelectHoverChanged;

	public override void _Ready()
	{
		_gameplayButtonGrid = _forwardButton.GetParent() as GridContainer;

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
		if (_openButton != null)
			_openButton.Pressed += () => OpenPressed?.Invoke();
		_potionButton.Pressed += () => PotionPressed?.Invoke();
		if (_defendButton != null)
			_defendButton.Pressed += () => DefendPressed?.Invoke();
		if (_disarmButton != null)
			_disarmButton.Pressed += () => DisarmPressed?.Invoke();
	}

	public bool IsInTargetSelectionMode => _targetingButtonHost != null;

	public void EnterTargetSelection(IReadOnlyList<TargetDescriptor> descriptors)
	{
		ExitTargetSelection();
		if (_gameplayButtonGrid == null)
			return;
		var parent = _gameplayButtonGrid.GetParent() as Control;
		if (parent == null)
			return;

		_gameplayButtonGrid.Visible = false;

		var host = new FlowContainer();
		host.Name = "TargetingButtonHost";
		host.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		host.AddThemeConstantOverride("h_separation", 8);
		host.AddThemeConstantOverride("v_separation", 8);

		for (var i = 0; i < descriptors.Count; i++)
		{
			var index = i;
			var d = descriptors[i];
			var b = new Button { Text = d.Label };
			b.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			b.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			b.Pressed += () => TargetSelectPicked?.Invoke(index);
			b.MouseEntered += () => TargetSelectHoverChanged?.Invoke(index);
			b.MouseExited += () => TargetSelectHoverChanged?.Invoke(null);
			host.AddChild(b);
		}

		var cancel = new Button { Text = "Cancel" };
		cancel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		cancel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		cancel.Pressed += () => TargetSelectCancelPressed?.Invoke();
		host.AddChild(cancel);

		parent.AddChild(host);
		_targetingButtonHost = host;
	}

	public void ExitTargetSelection()
	{
		if (_targetingButtonHost != null)
		{
			_targetingButtonHost.QueueFree();
			_targetingButtonHost = null;
		}

		if (_gameplayButtonGrid != null)
			_gameplayButtonGrid.Visible = true;
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
		_potionButton.Visible = exploration || combat;
		if (_defendButton != null)
			_defendButton.Visible = combat;
	}

	public void ApplyTakeButtonVisible(bool visible)
	{
		_takeButton.Visible = visible;
	}

	public void ApplyOpenButtonVisible(bool visible)
	{
		if (_openButton != null)
			_openButton.Visible = visible;
	}

	public void ApplyDisarmButtonVisible(bool visible)
	{
		if (_disarmButton != null)
			_disarmButton.Visible = visible;
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
		if (_openButton != null)
			_openButton.Disabled = disabled;
		_potionButton.Disabled = disabled;
		if (_defendButton != null)
			_defendButton.Disabled = disabled;
		if (_disarmButton != null)
			_disarmButton.Disabled = disabled;
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
		if (_openButton != null)
			_openButton.Visible = false;
		_potionButton.Visible = false;
		if (_defendButton != null)
			_defendButton.Visible = false;
		if (_disarmButton != null)
			_disarmButton.Visible = false;
	}

	public void ApplyPotionButtonState(PlayerState player)
	{
		var hpQty = player.InventoryState.SumQuantityForDefinitionId(InventoryIds.HealthPotion);
		_potionButton.Disabled = hpQty <= 0 || player.CurrentHp >= player.MaxHp;
	}

	public void ApplyCombatItemButtons(PlayerState player) =>
		ApplyPotionButtonState(player);

	public void ApplyCombatAbilityButtons(PlayerState player, GameSessionState session, ICombatService combat)
	{
		if (_defendButton == null)
			return;
		var hasDefend = player.HasAbility(AbilityIds.Defend);
		_defendButton.Visible = hasDefend;
		if (!hasDefend)
			return;
		var c = session.Combat;
		var awaiting = combat.IsAwaitingPlayerAction(session);
		var onCd = c != null && c.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend);
		var stance = c?.HasDefendStanceActive() == true;
		_defendButton.Disabled = !awaiting || stance || onCd;
	}

	public void RenderFloorExitButtons(DungeonRoom? currentRoom, PlayerState? player)
	{
		_floorUpButton.Visible = false;
		_floorDownButton.Visible = false;

		if (currentRoom == null || player == null)
			return;

		var floorExitFeature = RoomFeatureHelper.GetFeature<FloorExitFeature>(currentRoom);
		if (floorExitFeature == null)
			return;

		var holeTraversable =
			floorExitFeature.ExitType != FloorConnectionType.Hole ||
			floorExitFeature.RopeAnchored ||
			player.InventoryState.SumQuantityForDefinitionId(InventoryIds.Rope) > 0;

		switch (DirectionHelper.GetVerticalExitDirection(currentRoom.Position))
		{
			case VerticalDirection.Up when holeTraversable:
				_floorUpButton.Visible = true;
				break;
			case VerticalDirection.Down when holeTraversable:
				_floorDownButton.Visible = true;
				break;
		}
	}
}
