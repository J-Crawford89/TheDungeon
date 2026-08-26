#nullable enable
using Godot;
using System;
using System.Collections.Generic;

/// <summary>Modal notebook shell; selection and refresh live in <see cref="InventoryNotebookCoordinator"/>.</summary>
public partial class NotebookOverlay : Control
{
	[Export] internal Button? _closeButton;

	[Export] internal Button? _characterTabButton;
	[Export] internal Button? _inventoryTabButton;
	[Export] internal Button? _journalTabButton;
	[Export] internal Button? _mapsTabButton;
	[Export] internal Button? _bestiaryTabButton;
	[Export] internal Button? _optionsTabButton;

	[Export] internal Control? _dimBackground;
	[Export] internal GridContainer? _backpackGrid;

	[Export] internal Label? _itemNameTitle;
	[Export] internal TextureRect? _itemImage;
	[Export] internal RichTextLabel? _itemDetailDescription;

	[Export] internal Button? _equipButton;
	[Export] internal Button? _unequipButton;
	[Export] internal Button? _useButton;
	[Export] internal Button? _dropButton;
	[Export] internal Button? _moveToBeltButton;
	[Export] internal Button? _compareButton;

	[Export] internal InventorySlotControl? _headSlotControl;
	[Export] internal InventorySlotControl? _neckSlotControl;
	[Export] internal InventorySlotControl? _torsoSlotControl;
	[Export] internal InventorySlotControl? _beltSlotControl;
	[Export] internal InventorySlotControl? _legsSlotControl;
	[Export] internal InventorySlotControl? _feetSlotControl;
	[Export] internal InventorySlotControl? _leftShoulderSlotControl;
	[Export] internal InventorySlotControl? _rightShoulderSlotControl;
	[Export] internal InventorySlotControl? _leftArmSlotControl;
	[Export] internal InventorySlotControl? _rightArmSlotControl;
	[Export] internal InventorySlotControl? _leftWristSlotControl;
	[Export] internal InventorySlotControl? _rightWristSlotControl;
	[Export] internal InventorySlotControl? _leftHandSlotControl;
	[Export] internal InventorySlotControl? _rightHandSlotControl;
	[Export] internal InventorySlotControl? _backSlotControl;
	[Export] internal InventorySlotControl? _backpackSlotControl;
	[Export] internal InventorySlotControl? _ring1SlotControl;
	[Export] internal InventorySlotControl? _ring2SlotControl;
	[Export] internal InventorySlotControl? _trinket1SlotControl;
	[Export] internal InventorySlotControl? _trinket2SlotControl;
	[Export] internal InventorySlotControl? _belt1SlotControl;
	[Export] internal InventorySlotControl? _belt2SlotControl;
	[Export] internal InventorySlotControl? _belt3SlotControl;
	[Export] internal InventorySlotControl? _belt4SlotControl;
	[Export] internal InventorySlotControl? _mainHand1SlotControl;
	[Export] internal InventorySlotControl? _offHand1SlotControl;
	[Export] internal InventorySlotControl? _mainHand2SlotControl;
	[Export] internal InventorySlotControl? _offHand2SlotControl;

	[Export] internal Control? _inventoryPageRoot;
	[Export] internal Label? _inventoryCopperCoinLabel;
	[Export] internal Label? _inventorySilverCoinLabel;
	[Export] internal Label? _inventoryGoldCoinLabel;
	[Export] internal Label? _inventoryPlatinumCoinLabel;
	[Export] internal CharacterPage? _characterPage;

	private InventoryNotebookCoordinator? _coordinator;
	private bool _wired;
	private GameRunContext? _runContext;

	public event Action? StateChanged;

	public override void _Ready()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Stop;
		if (_dimBackground != null)
			_dimBackground.MouseFilter = MouseFilterEnum.Stop;

		DisablePlaceholderTabs();

		if (_closeButton != null)
			_closeButton.Pressed += OnClosePressed;

		if (_inventoryTabButton != null)
			_inventoryTabButton.Pressed += OnInventoryTabPressed;
		if (_characterTabButton != null)
			_characterTabButton.Pressed += OnCharacterTabPressed;

		ValidateExports();
	}

	public override void _ExitTree()
	{
		if (_closeButton != null)
			_closeButton.Pressed -= OnClosePressed;
		if (_inventoryTabButton != null)
			_inventoryTabButton.Pressed -= OnInventoryTabPressed;
		if (_characterTabButton != null)
			_characterTabButton.Pressed -= OnCharacterTabPressed;

		if (_wired && _coordinator != null)
			_coordinator.DisconnectSlots();
		_wired = false;
		_coordinator = null;
	}

	public void Bind(GameRunContext context)
	{
		_runContext = context;

		if (_wired)
			return;
		var maxRows = context.GameBalanceSettings?.MaxUnequippedBackpackRows ?? 16;
		if (maxRows < 1)
			maxRows = 16;
		_coordinator = new InventoryNotebookCoordinator(this, context, NotifyStateChanged, maxRows);
		_coordinator.WireSlotsAndButtons();
		_wired = true;
	}

	public void ShowInventory()
	{
		Visible = true;
		SwitchToInventoryPage();
		_coordinator?.DismissEquipSlotPicker();
		_coordinator?.ClearSelection();
		_coordinator?.RefreshAll();
	}

	public void ShowCharacter()
	{
		Visible = true;
		SwitchToCharacterPage();
		_coordinator?.DismissEquipSlotPicker();
		_coordinator?.ClearSelection();

		if (_runContext != null && _characterPage != null)
			_characterPage.Populate(_runContext);
	}

	public void HideNotebook()
	{
		Visible = false;
		_coordinator?.DismissEquipSlotPicker();
		_coordinator?.ClearSelection();
	}

	private void OnClosePressed() => HideNotebook();

	private void OnInventoryTabPressed() => ShowInventory();

	private void OnCharacterTabPressed() => ShowCharacter();

	private void NotifyStateChanged() => StateChanged?.Invoke();

	private void SwitchToInventoryPage()
	{
		if (_inventoryPageRoot != null)
			_inventoryPageRoot.Visible = true;
		if (_characterPage != null)
			_characterPage.Visible = false;
	}

	private void SwitchToCharacterPage()
	{
		if (_inventoryPageRoot != null)
			_inventoryPageRoot.Visible = false;
		if (_characterPage != null)
			_characterPage.Visible = true;
	}

	private void DisablePlaceholderTabs()
	{
		void dis(Button? b)
		{
			if (b != null)
				b.Disabled = true;
		}

		dis(_journalTabButton);
		dis(_mapsTabButton);
		dis(_bestiaryTabButton);
		dis(_optionsTabButton);
	}

	private void ValidateExports()
	{
		if (_closeButton == null || _backpackGrid == null || _itemNameTitle == null || _itemImage == null ||
			_itemDetailDescription == null)
			GD.PushWarning("NotebookOverlay: assign CloseButton, BackpackGrid, and item detail exports.");
		if (_inventoryPageRoot == null || _characterPage == null)
			GD.PushWarning("NotebookOverlay: assign Inventory Page Root and Character Page exports for tab switching.");
	}
}
