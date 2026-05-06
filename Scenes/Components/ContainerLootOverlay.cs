#nullable enable
using System;
using System.Collections.Generic;
using Godot;

/// <summary>Modal loot picker for <see cref="ContainerLootInteractionService"/>; bind once via <see cref="Bind"/>.</summary>
public partial class ContainerLootOverlay : Control, IContainerLootOverlayOpener
{
	[Export] private Label _lootTitle = null!;
	[Export] private Label _lootSubtitle = null!;
	[Export] private Label _lootWarning = null!;
	[Export] private GridContainer _lootGrid = null!;
	[Export] private Button _lootSelectedButton = null!;
	[Export] private Button _lootAllButton = null!;
	[Export] private Button _closeButton = null!;

	/// <summary>Typically <c>LootableItemControl.tscn</c>; assign in Inspector.</summary>
	[Export] private PackedScene _lootableItemScene = null!;

	private GameRunContext? _ctx;
	private Action<UiRefreshFlags>? _refreshHud;
	private int _containerOrdinal = -1;

	private readonly HashSet<int> _selectedStackIndices = new();
	private readonly Dictionary<int, LootableItemControl> _lootableItemsByStackIndex = new();

	private bool _signalsConnected;

	public void Bind(GameRunContext context, Action<UiRefreshFlags> refreshHud)
	{
		_ctx = context;
		_refreshHud = refreshHud;
		if (!_signalsConnected)
		{
			_lootSelectedButton.Pressed += OnTakeSelectedPressed;
			_lootAllButton.Pressed += OnTakeAllPressed;
			_closeButton.Pressed += OnClosePressed;
			_signalsConnected = true;
		}

		MouseFilter = MouseFilterEnum.Stop;
		var dimmer = GetNodeOrNull<Control>("LootDimmer");
		if (dimmer != null)
			dimmer.MouseFilter = MouseFilterEnum.Stop;
	}

	public void OpenLootPanel(int containerOrdinal) => OpenInternal(containerOrdinal);

	private void OpenInternal(int containerOrdinal)
	{
		if (_ctx == null || _refreshHud == null)
		{
			GD.PushError("ContainerLootOverlay.Bind(GameRunContext, refresh) must run before Open.");
			return;
		}

		if (_lootableItemScene == null)
		{
			GD.PushError("ContainerLootOverlay: assign Lootable Item PackedScene export.");
			return;
		}

		var session = _ctx.Session;
		var result = _ctx.ContainerLootInteraction.TryBuildPanel(session, containerOrdinal);
		if (result.ErrorCode != ContainerLootErrorCode.None || result.Panel == null)
		{
			GD.PushWarning($"Container loot panel unavailable: {result.ErrorCode}");
			return;
		}

		if (session.Dungeon.CurrentRoom is { } room &&
		    RoomContainerLocator.TryGetNthContainer(room, containerOrdinal, out var openedContainer) &&
		    openedContainer != null)
			openedContainer.WasOpened = true;

		_containerOrdinal = containerOrdinal;
		_selectedStackIndices.Clear();
		ClearGrid();

		var dto = result.Panel;
		_lootTitle.Text = dto.PanelTitle;
		_lootSubtitle.Text = dto.PanelSubtitle;
		_lootSubtitle.Visible = !string.IsNullOrWhiteSpace(dto.PanelSubtitle);

		if (dto.LikelyCrowdedAfterTakeAll)
		{
			_lootWarning.Visible = true;
			_lootWarning.Text = "Taking everything may crowd your backpack.";
		}
		else
		{
			_lootWarning.Visible = false;
			_lootWarning.Text = "";
		}

		foreach (var stack in dto.Rows)
		{
			var node = _lootableItemScene.Instantiate<LootableItemControl>();
			var tex = _ctx.Icons.Resolve(PresentationIconKeys.MainView.Item(stack.ItemDefinitionId));
			node.Configure(stack.RowIndex, stack.DisplayName, stack.Quantity, tex, stack.HarvestDc, stack.HarvestAbility,
				OnLootItemToggled);
			_lootGrid.AddChild(node);
			_lootableItemsByStackIndex[stack.RowIndex] = node;
		}

		Visible = true;
	}

	private void OnLootItemToggled(int stackIndex)
	{
		if (!_lootableItemsByStackIndex.TryGetValue(stackIndex, out var ctrl))
			return;
		if (_selectedStackIndices.Contains(stackIndex))
			_selectedStackIndices.Remove(stackIndex);
		else
			_selectedStackIndices.Add(stackIndex);
		ctrl.SetSelected(_selectedStackIndices.Contains(stackIndex));
	}

	private void OnTakeAllPressed()
	{
		if (_ctx == null || _refreshHud == null || _containerOrdinal < 0)
			return;
		_ctx.ContainerLootInteraction.TryLootAll(_ctx.Session, _containerOrdinal);

		ClosePanel();
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	private void OnTakeSelectedPressed()
	{
		if (_ctx == null || _refreshHud == null || _containerOrdinal < 0)
			return;
		if (_selectedStackIndices.Count == 0)
		{
			_lootWarning.Visible = true;
			_lootWarning.Text = "Select one or more stacks.";
			return;
		}

		var indices = new List<int>(_selectedStackIndices);
		indices.Sort();
		var r = _ctx.ContainerLootInteraction.TryLootSelected(_ctx.Session, _containerOrdinal, indices);
		if (r.ErrorCode != ContainerLootErrorCode.None)
		{
			_lootWarning.Visible = true;
			_lootWarning.Text = r.ErrorCode switch
			{
				ContainerLootErrorCode.EmptyRowSelection => "Select one or more stacks.",
				ContainerLootErrorCode.InvalidRowSelection => "Invalid selection.",
				_ => "Could not take items.",
			};
			return;
		}

		ClosePanel();
		_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
	}

	private void OnClosePressed()
	{
		ClosePanel();
		_refreshHud?.Invoke(UiRefreshFlags.MainView);
	}

	private void ClosePanel()
	{
		Visible = false;
		_containerOrdinal = -1;
		_selectedStackIndices.Clear();
		ClearGrid();
	}

	private void ClearGrid()
	{
		foreach (var child in _lootGrid.GetChildren())
			((Node)child).QueueFree();
		_lootableItemsByStackIndex.Clear();
	}

	public override void _ExitTree()
	{
		if (_signalsConnected)
		{
			_lootSelectedButton.Pressed -= OnTakeSelectedPressed;
			_lootAllButton.Pressed -= OnTakeAllPressed;
			_closeButton.Pressed -= OnClosePressed;
			_signalsConnected = false;
		}

		base._ExitTree();
	}
}
