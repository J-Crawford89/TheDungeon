#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal sealed class InventoryNotebookCoordinator
{
	private readonly NotebookOverlay _o;
	private readonly GameSessionState _session;
	private readonly IconResolver _icons;
	private readonly PotionEffectApplicationService _potions;
	private readonly PlayerProficiencyAggregationService _proficiencyAggregation;
	private readonly Action _stateChanged;
	private readonly int _maxUnequippedBackpackRows;

	private readonly List<InventorySlotControl> _slotRefs = new();
	private IReadOnlyList<ItemInstance> _backpackRows = Array.Empty<ItemInstance>();
	private InventoryNotebookSelection _selection = InventoryNotebookSelection.None;
	private static bool _overflowWarned;
	private Guid? _pendingEquipItemInstanceId;
	private HashSet<EquipmentSlot>? _pendingEquipEligibleSlots;
	private bool _isUsingItem;

	public InventoryNotebookCoordinator(
		NotebookOverlay o,
		GameRunContext context,
		Action stateChanged,
		int maxUnequippedBackpackRows = 16)
	{
		_o = o;
		_session = context.Session;
		_icons = context.Icons;
		_potions = context.PotionEffects;
		_proficiencyAggregation = context.ProficiencyAggregation;
		_stateChanged = stateChanged;
		_maxUnequippedBackpackRows = maxUnequippedBackpackRows;
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
		DismissEquipSlotPicker();
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
		if (_pendingEquipItemInstanceId.HasValue && _pendingEquipEligibleSlots != null)
		{
			if (slot.SlotKind == InventorySlotKind.Equipment && slot.EquipmentSlot is { } es &&
				_pendingEquipEligibleSlots.Contains(es))
			{
				TryCompletePendingEquipToSlot(es);
				return;
			}

			ClearPendingEquipSlotMode();
		}

		_selection = slot.SlotKind switch
		{
			InventorySlotKind.Equipment when slot.EquipmentSlot is { } es2 => InventoryNotebookSelection.Equipment(es2),
			InventorySlotKind.Backpack when slot.BackpackIndex is { } ix => InventoryNotebookSelection.Backpack(ix),
			_ => InventoryNotebookSelection.None,
		};
		ApplySelectionVisuals();
		UpdateDetailPanel();
	}

	public void ClearSelection()
	{
		ClearPendingEquipSlotMode();
		_selection = InventoryNotebookSelection.None;
		ApplySelectionVisuals();
		UpdateDetailPanel();
	}

	public void DismissEquipSlotPicker()
	{
		ClearPendingEquipSlotMode();
		ApplySelectionVisuals();
	}

	private void ClearPendingEquipSlotMode()
	{
		_pendingEquipItemInstanceId = null;
		_pendingEquipEligibleSlots = null;
	}

	private void BeginPendingEquipSlotSelection(EquipmentDefinition eqDef, ItemInstance item)
	{
		ClearPendingEquipSlotMode();
		_pendingEquipItemInstanceId = item.InstanceId;
		_pendingEquipEligibleSlots = new HashSet<EquipmentSlot>(InventoryEquipmentOperations.SingleSlotEquipChoices(eqDef));
		ApplySelectionVisuals();
	}

	private void TryCompletePendingEquipToSlot(EquipmentSlot chosenSlot)
	{
		if (!_pendingEquipItemInstanceId.HasValue || _pendingEquipEligibleSlots == null)
			return;
		if (!_pendingEquipEligibleSlots.Contains(chosenSlot))
			return;

		var instanceId = _pendingEquipItemInstanceId.Value;
		ClearPendingEquipSlotMode();

		var inv = _session.Player.InventoryState;
		ItemInstance? row = null;
		foreach (var r in inv.Items)
		{
			if (r.InstanceId == instanceId)
			{
				row = r;
				break;
			}
		}

		if (row == null || !InventoryEquipmentOperations.TryEquipOneFromBackpackRowToSlot(inv, row, chosenSlot))
		{
			ApplySelectionVisuals();
			UpdateDetailPanel();
			return;
		}

		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(_session.Player);
		_proficiencyAggregation.Recompute(_session.Player);
		_selection = InventoryNotebookSelection.Equipment(chosenSlot);
		_stateChanged();
		RefreshAll();
	}

	private void ApplySelectionVisuals()
	{
		var pending = _pendingEquipItemInstanceId.HasValue && _pendingEquipEligibleSlots != null;
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

			var pendingTarget = pending &&
								s.SlotKind == InventorySlotKind.Equipment &&
								s.EquipmentSlot is { } es2 &&
								_pendingEquipEligibleSlots!.Contains(es2);
			s.SetPendingEquipCandidate(pendingTarget);
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
			var (rows, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(inv, _maxUnequippedBackpackRows);
		_backpackRows = rows;
		if (overflow && !_overflowWarned)
		{
			_overflowWarned = true;
				GD.PushWarning($"NotebookOverlay: more than {_maxUnequippedBackpackRows} unequipped backpack stacks; only the first {_maxUnequippedBackpackRows} are shown.");
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

		CoinPurseLabelHelper.ApplyDenominationLabels(
			_o._inventoryCopperCoinLabel,
			_o._inventorySilverCoinLabel,
			_o._inventoryGoldCoinLabel,
			_o._inventoryPlatinumCoinLabel,
			_session.Player.Purse);
	}

	private void OnEquipPressed()
	{
		if (!InventoryNotebookActionRules.ShouldEnableEquip(_selection, GetSelectedItem()))
			return;
		var item = GetSelectedItem();
		if (item == null)
			return;
		if (item.Definition is not EquipmentDefinition eqDef)
			return;

		if (InventoryEquipmentOperations.RequiresPlayerSlotChoiceForEquip(eqDef))
		{
			BeginPendingEquipSlotSelection(eqDef, item);
			return;
		}

		if (!InventoryEquipmentOperations.TryEquipOneFromBackpackRow(_session.Player.InventoryState, item))
			return;
		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(_session.Player);
		_proficiencyAggregation.Recompute(_session.Player);
		_stateChanged();
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
		PlayerDefenseAggregationHelper.RecomputeFromEquippedArmor(_session.Player);
		_proficiencyAggregation.Recompute(_session.Player);
		_stateChanged();
		RefreshAll();
	}

	private void OnUsePressed() => GodotAsyncEventHandler.Run(UseSelectedItemAsync, "Use inventory item");

	private async Task UseSelectedItemAsync()
	{
		if (_isUsingItem)
			return;
		if (!InventoryNotebookActionRules.ShouldEnableUse(_selection, GetSelectedItem()))
			return;

		_isUsingItem = true;
		SetAllActionsDisabled();
		try
		{
			await _potions.TryUseHealthPotionAsync(_session);
			_stateChanged();
		}
		finally
		{
			_isUsingItem = false;
			RefreshAll();
		}
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
