#nullable enable
using Godot;
using System;
using System.Collections.Generic;

/// <summary>Modal notebook shell; selection and refresh live in <see cref="InventoryNotebookCoordinator"/>.</summary>
public partial class NotebookOverlay : Control
{
	[Export] private Button? _closeButton;

	[Export] private Button? _characterTabButton;
	[Export] private Button? _inventoryTabButton;
	[Export] private Button? _journalTabButton;
	[Export] private Button? _mapsTabButton;
	[Export] private Button? _bestiaryTabButton;
	[Export] private Button? _optionsTabButton;

	[Export] private Control? _dimBackground;
	[Export] private GridContainer? _backpackGrid;

	[Export] private Label? _itemNameTitle;
	[Export] private TextureRect? _itemImage;
	[Export] private RichTextLabel? _itemDetailDescription;

	[Export] private Button? _equipButton;
	[Export] private Button? _unequipButton;
	[Export] private Button? _useButton;
	[Export] private Button? _dropButton;
	[Export] private Button? _moveToBeltButton;
	[Export] private Button? _compareButton;

	[Export] private InventorySlotControl? _headSlotControl;
	[Export] private InventorySlotControl? _neckSlotControl;
	[Export] private InventorySlotControl? _torsoSlotControl;
	[Export] private InventorySlotControl? _beltSlotControl;
	[Export] private InventorySlotControl? _legsSlotControl;
	[Export] private InventorySlotControl? _feetSlotControl;
	[Export] private InventorySlotControl? _leftShoulderSlotControl;
	[Export] private InventorySlotControl? _rightShoulderSlotControl;
	[Export] private InventorySlotControl? _leftArmSlotControl;
	[Export] private InventorySlotControl? _rightArmSlotControl;
	[Export] private InventorySlotControl? _leftWristSlotControl;
	[Export] private InventorySlotControl? _rightWristSlotControl;
	[Export] private InventorySlotControl? _leftHandSlotControl;
	[Export] private InventorySlotControl? _rightHandSlotControl;
	[Export] private InventorySlotControl? _backSlotControl;
	[Export] private InventorySlotControl? _backpackSlotControl;
	[Export] private InventorySlotControl? _ring1SlotControl;
	[Export] private InventorySlotControl? _ring2SlotControl;
	[Export] private InventorySlotControl? _trinket1SlotControl;
	[Export] private InventorySlotControl? _trinket2SlotControl;
	[Export] private InventorySlotControl? _belt1SlotControl;
	[Export] private InventorySlotControl? _belt2SlotControl;
	[Export] private InventorySlotControl? _belt3SlotControl;
	[Export] private InventorySlotControl? _belt4SlotControl;
	[Export] private InventorySlotControl? _mainHand1SlotControl;
	[Export] private InventorySlotControl? _offHand1SlotControl;
	[Export] private InventorySlotControl? _mainHand2SlotControl;
	[Export] private InventorySlotControl? _offHand2SlotControl;

	private InventoryNotebookCoordinator? _coordinator;
	private bool _wired;

	public override void _Ready()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Stop;
		if (_dimBackground != null)
			_dimBackground.MouseFilter = MouseFilterEnum.Stop;

		DisablePlaceholderTabs();

		if (_closeButton != null)
			_closeButton.Pressed += OnClosePressed;

		ValidateExports();
	}

	public override void _ExitTree()
	{
		if (_closeButton != null)
			_closeButton.Pressed -= OnClosePressed;
		if (_wired && _coordinator != null)
			_coordinator.DisconnectSlots();
		_wired = false;
		_coordinator = null;
	}

	public void Bind(GameRunContext context, Action<UiRefreshFlags>? refreshHud)
	{
		if (_wired)
			return;
		_coordinator = new InventoryNotebookCoordinator(this, context, refreshHud);
		_coordinator.WireSlotsAndButtons();
		_wired = true;
	}

	public void ShowInventory()
	{
		Visible = true;
		_coordinator?.ClearSelection();
		_coordinator?.RefreshAll();
	}

	public void HideNotebook()
	{
		Visible = false;
		_coordinator?.ClearSelection();
	}

	private void OnClosePressed() => HideNotebook();

	private void DisablePlaceholderTabs()
	{
		void dis(Button? b)
		{
			if (b != null)
				b.Disabled = true;
		}

		dis(_characterTabButton);
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
	}

	private sealed class InventoryNotebookCoordinator
	{
		private readonly NotebookOverlay _o;
		private readonly GameSessionState _session;
		private readonly IconResolver _icons;
		private readonly PotionEffectApplicationService _potions;
		private readonly Action<UiRefreshFlags>? _refreshHud;

		private readonly List<InventorySlotControl> _slotRefs = new();
		private IReadOnlyList<ItemInstance> _backpackRows = Array.Empty<ItemInstance>();
		private InventoryNotebookSelection _selection = InventoryNotebookSelection.None;
		private static bool _overflowWarned;

		public InventoryNotebookCoordinator(NotebookOverlay o, GameRunContext context, Action<UiRefreshFlags>? refreshHud)
		{
			_o = o;
			_session = context.Session;
			_icons = context.Icons;
			_potions = context.PotionEffects;
			_refreshHud = refreshHud;
		}

		public void WireSlotsAndButtons()
		{
			if (_o._backpackGrid == null)
			{
				GD.PushError("NotebookOverlay: BackpackGrid missing.");
				return;
			}

			RegisterEquipment(_o._headSlotControl, EquipmentSlot.Head);
			RegisterEquipment(_o._neckSlotControl, EquipmentSlot.Neck);
			RegisterEquipment(_o._torsoSlotControl, EquipmentSlot.Torso);
			RegisterEquipment(_o._beltSlotControl, EquipmentSlot.Waist);
			RegisterEquipment(_o._legsSlotControl, EquipmentSlot.Legs);
			RegisterEquipment(_o._feetSlotControl, EquipmentSlot.Feet);
			RegisterEquipment(_o._leftShoulderSlotControl, EquipmentSlot.Shoulders);
			RegisterEquipment(_o._rightShoulderSlotControl, EquipmentSlot.Shoulders);
			RegisterEquipment(_o._leftArmSlotControl, EquipmentSlot.Arms);
			RegisterEquipment(_o._rightArmSlotControl, EquipmentSlot.Arms);
			RegisterEquipment(_o._leftWristSlotControl, EquipmentSlot.Wrist1);
			RegisterEquipment(_o._rightWristSlotControl, EquipmentSlot.Wrist2);
			RegisterEquipment(_o._leftHandSlotControl, EquipmentSlot.Hands);
			RegisterEquipment(_o._rightHandSlotControl, EquipmentSlot.Hands);
			RegisterEquipment(_o._backSlotControl, EquipmentSlot.Back);
			RegisterEquipment(_o._backpackSlotControl, EquipmentSlot.Backpack);
			RegisterEquipment(_o._ring1SlotControl, EquipmentSlot.Ring1);
			RegisterEquipment(_o._ring2SlotControl, EquipmentSlot.Ring2);
			RegisterEquipment(_o._trinket1SlotControl, EquipmentSlot.Trinket1);
			RegisterEquipment(_o._trinket2SlotControl, EquipmentSlot.Trinket2);
			RegisterEquipment(_o._belt1SlotControl, EquipmentSlot.BeltSlot1);
			RegisterEquipment(_o._belt2SlotControl, EquipmentSlot.BeltSlot2);
			RegisterEquipment(_o._belt3SlotControl, EquipmentSlot.BeltSlot3);
			RegisterEquipment(_o._belt4SlotControl, EquipmentSlot.BeltSlot4);
			RegisterEquipment(_o._mainHand1SlotControl, EquipmentSlot.WeaponMainHand1);
			RegisterEquipment(_o._offHand1SlotControl, EquipmentSlot.WeaponOffHand1);
			RegisterEquipment(_o._mainHand2SlotControl, EquipmentSlot.WeaponMainHand2);
			RegisterEquipment(_o._offHand2SlotControl, EquipmentSlot.WeaponOffHand2);

			var bi = 0;
			foreach (var child in _o._backpackGrid!.GetChildren())
			{
				if (child is not InventorySlotControl isc)
					continue;
				isc.ConfigureBackpack(bi);
				isc.SlotClicked += OnSlotClicked;
				_slotRefs.Add(isc);
				bi++;
			}

			if (_o._equipButton != null)
				_o._equipButton.Pressed += OnEquipPressed;
			if (_o._unequipButton != null)
				_o._unequipButton.Pressed += OnUnequipPressed;
			if (_o._useButton != null)
				_o._useButton.Pressed += OnUsePressed;
			if (_o._dropButton != null)
				_o._dropButton.Pressed += OnDropPressed;
			if (_o._moveToBeltButton != null)
				_o._moveToBeltButton.Pressed += OnMoveToBeltPressed;
			if (_o._compareButton != null)
				_o._compareButton.Pressed += OnComparePressed;
		}

		public void DisconnectSlots()
		{
			foreach (var s in _slotRefs)
				s.SlotClicked -= OnSlotClicked;
			if (_o._equipButton != null)
				_o._equipButton.Pressed -= OnEquipPressed;
			if (_o._unequipButton != null)
				_o._unequipButton.Pressed -= OnUnequipPressed;
			if (_o._useButton != null)
				_o._useButton.Pressed -= OnUsePressed;
			if (_o._dropButton != null)
				_o._dropButton.Pressed -= OnDropPressed;
			if (_o._moveToBeltButton != null)
				_o._moveToBeltButton.Pressed -= OnMoveToBeltPressed;
			if (_o._compareButton != null)
				_o._compareButton.Pressed -= OnComparePressed;
		}

		private void RegisterEquipment(InventorySlotControl? slot, EquipmentSlot equipmentSlot)
		{
			if (slot == null)
				return;
			slot.ConfigureEquipment(equipmentSlot);
			slot.SlotClicked += OnSlotClicked;
			_slotRefs.Add(slot);
		}

		private void OnSlotClicked(InventorySlotControl slot)
		{
			_selection = slot.SlotKind switch
			{
				InventorySlotKind.Equipment when slot.EquipmentSlot is { } es => InventoryNotebookSelection.Equipment(es),
				InventorySlotKind.Backpack when slot.BackpackIndex is { } ix => InventoryNotebookSelection.Backpack(ix),
				_ => InventoryNotebookSelection.None,
			};
			ApplySelectionVisuals();
			UpdateDetailPanel();
		}

		public void ClearSelection()
		{
			_selection = InventoryNotebookSelection.None;
			ApplySelectionVisuals();
			UpdateDetailPanel();
		}

		private void ApplySelectionVisuals()
		{
			foreach (var s in _slotRefs)
			{
				var on = s.SlotKind switch
				{
					InventorySlotKind.Equipment when s.EquipmentSlot is { } es =>
						_selection.Kind == InventorySlotKind.Equipment && _selection.EquipmentSlot == es,
					InventorySlotKind.Backpack when s.BackpackIndex is { } ix =>
						_selection.Kind == InventorySlotKind.Backpack && _selection.BackpackGridIndex == ix,
					_ => false,
				};
				s.SetSelected(on);
			}
		}

		private ItemInstance? GetSelectedItem() =>
			_selection.ResolveItem(_session.Player.InventoryState, _backpackRows);

		private void UpdateDetailPanel()
		{
			var item = GetSelectedItem();
			if (item == null || _selection.Kind == InventorySlotKind.None)
			{
				if (_o._itemNameTitle != null)
					_o._itemNameTitle.Text = "No item selected";
				if (_o._itemImage != null)
					_o._itemImage.Texture = null;
				if (_o._itemDetailDescription != null)
					_o._itemDetailDescription.Text = string.Empty;
				SetAllActionsDisabled();
				return;
			}

			if (_o._itemNameTitle != null)
				_o._itemNameTitle.Text = item.Definition.Name;
			if (_o._itemImage != null)
				_o._itemImage.Texture = _icons.Resolve(PresentationIconKeys.Inventory.Item(item.Definition.Id));
			if (_o._itemDetailDescription != null)
				_o._itemDetailDescription.Text = string.IsNullOrEmpty(item.Definition.Description)
					? string.Empty
					: item.Definition.Description;

			if (_o._equipButton != null)
				_o._equipButton.Disabled = !InventoryNotebookActionRules.ShouldEnableEquip(_selection, item);
			if (_o._unequipButton != null)
				_o._unequipButton.Disabled = !InventoryNotebookActionRules.ShouldEnableUnequip(_selection, item);
			if (_o._useButton != null)
				_o._useButton.Disabled = !InventoryNotebookActionRules.ShouldEnableUse(_selection, item);
			if (_o._dropButton != null)
				_o._dropButton.Disabled = !InventoryNotebookActionRules.ShouldEnableDrop(item);
			if (_o._moveToBeltButton != null)
				_o._moveToBeltButton.Disabled = !InventoryNotebookActionRules.ShouldEnableMoveToBelt(_selection, item);
			if (_o._compareButton != null)
				_o._compareButton.Disabled = !InventoryNotebookActionRules.ShouldEnableCompare();
		}

		private void SetAllActionsDisabled()
		{
			if (_o._equipButton != null)
				_o._equipButton.Disabled = true;
			if (_o._unequipButton != null)
				_o._unequipButton.Disabled = true;
			if (_o._useButton != null)
				_o._useButton.Disabled = true;
			if (_o._dropButton != null)
				_o._dropButton.Disabled = true;
			if (_o._moveToBeltButton != null)
				_o._moveToBeltButton.Disabled = true;
			if (_o._compareButton != null)
				_o._compareButton.Disabled = true;
		}

		public void RefreshAll()
		{
			var inv = _session.Player.InventoryState;
			var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(inv, 16);
			_backpackRows = rows;
			if (overflow && !_overflowWarned)
			{
				_overflowWarned = true;
				GD.PushWarning("NotebookOverlay: more than 16 unequipped backpack stacks; only the first 16 are shown.");
			}

			foreach (var slot in _slotRefs)
			{
				if (slot.SlotKind == InventorySlotKind.Equipment && slot.EquipmentSlot is { } es)
				{
					if (inv.EquippedBySlot.TryGetValue(es, out var eq) && eq != null)
					{
						var tex = _icons.Resolve(PresentationIconKeys.Inventory.Item(eq.Definition.Id));
						slot.SetItem(tex, eq.Quantity);
					}
					else
						slot.SetEmpty();
				}
				else if (slot.SlotKind == InventorySlotKind.Backpack && slot.BackpackIndex is { } ix)
				{
					if (ix < _backpackRows.Count)
					{
						var row = _backpackRows[ix];
						var tex = _icons.Resolve(PresentationIconKeys.Inventory.Item(row.Definition.Id));
						slot.SetItem(tex, row.Quantity);
					}
					else
						slot.SetEmpty();
				}
			}

			ApplySelectionVisuals();
			UpdateDetailPanel();
		}

		private void OnEquipPressed()
		{
			if (!InventoryNotebookActionRules.ShouldEnableEquip(_selection, GetSelectedItem()))
				return;
			var item = GetSelectedItem();
			if (item == null)
				return;
			if (!InventoryEquipmentOperations.TryEquipOneFromBackpackRow(_session.Player.InventoryState, item))
				return;
			_refreshHud?.Invoke(UiRefreshFlags.Character | UiRefreshFlags.Command);
			RefreshAll();
		}

		private void OnUnequipPressed()
		{
			if (!InventoryNotebookActionRules.ShouldEnableUnequip(_selection, GetSelectedItem()))
				return;
			if (_selection.Kind != InventorySlotKind.Equipment)
				return;
			if (!InventoryEquipmentOperations.TryUnequipSlot(_session.Player.InventoryState, _selection.EquipmentSlot))
				return;
			_refreshHud?.Invoke(UiRefreshFlags.Character | UiRefreshFlags.Command);
			RefreshAll();
		}

		private void OnUsePressed()
		{
			if (!InventoryNotebookActionRules.ShouldEnableUse(_selection, GetSelectedItem()))
				return;
			_potions.TryUseHealthPotion(_session);
			_refreshHud?.Invoke(UiRefreshFlags.Character | UiRefreshFlags.Command);
			RefreshAll();
		}

		private void OnDropPressed()
		{
			GD.Print("[Notebook] Drop not implemented yet.");
		}

		private void OnMoveToBeltPressed()
		{
			GD.Print("[Notebook] Move to belt not implemented yet.");
		}

		private void OnComparePressed()
		{
			GD.Print("[Notebook] Compare not implemented yet.");
		}
	}
}
