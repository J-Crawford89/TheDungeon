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
	[Export] private Control? _lootDimmer;

	private GameRunContext? _ctx;
	private Action<UiRefreshFlags>? _refreshHud;
	private int _containerOrdinal = -1;

	private readonly HashSet<int> _selectedStackIndices = new();
	private readonly Dictionary<int, LootableItemControl> _lootableItemsByStackIndex = new();

	private bool _signalsConnected;
	private bool _isResolvingLoot;

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
		if (_lootDimmer != null)
			_lootDimmer.MouseFilter = MouseFilterEnum.Stop;
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

	private async void OnTakeAllPressed()
	{
		if (_isResolvingLoot || _ctx == null || _refreshHud == null || _containerOrdinal < 0)
			return;
		SetResolvingLoot(true);
		try
		{
			await _ctx.ContainerLootInteraction.TryLootAllAsync(_ctx.Session, _containerOrdinal);

			ClosePanel();
			_refreshHud(UiRefreshFlags.Log | UiRefreshFlags.Character | UiRefreshFlags.Command | UiRefreshFlags.MainView);
		}
		finally
		{
			SetResolvingLoot(false);
		}
	}

	private async void OnTakeSelectedPressed()
	{
		if (_isResolvingLoot || _ctx == null || _refreshHud == null || _containerOrdinal < 0)
			return;
		if (_selectedStackIndices.Count == 0)
		{
			_lootWarning.Visible = true;
			_lootWarning.Text = "Select one or more stacks.";
			return;
		}

		var indices = new List<int>(_selectedStackIndices);
		indices.Sort();
		SetResolvingLoot(true);
		ContainerLootTransferResult r;
		try
		{
			r = await _ctx.ContainerLootInteraction.TryLootSelectedAsync(_ctx.Session, _containerOrdinal, indices);
		}
		finally
		{
			SetResolvingLoot(false);
		}
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

	private void SetResolvingLoot(bool resolving)
	{
		_isResolvingLoot = resolving;
		_lootSelectedButton.Disabled = resolving;
		_lootAllButton.Disabled = resolving;
		_closeButton.Disabled = resolving;
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
