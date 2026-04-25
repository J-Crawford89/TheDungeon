#nullable enable
using Godot;
using System;

/// <summary>Visual backpack or equipment cell; emits click only — no game rules.</summary>
public partial class InventorySlotControl : PanelContainer
{
	[Export] private TextureRect? _itemIcon;
	[Export] private Label? _quantityLabel;
	[Export] private Control? _selectionBorder;

	public InventorySlotKind SlotKind { get; private set; }
	public EquipmentSlot? EquipmentSlot { get; private set; }
	public int? BackpackIndex { get; private set; }

	public event Action<InventorySlotControl>? SlotClicked;

	private bool _selected;
	private bool _pendingEquipCandidate;

	public void ConfigureEquipment(EquipmentSlot slot)
	{
		SlotKind = InventorySlotKind.Equipment;
		EquipmentSlot = slot;
		BackpackIndex = null;
	}

	public void ConfigureBackpack(int index)
	{
		SlotKind = InventorySlotKind.Backpack;
		EquipmentSlot = null;
		BackpackIndex = index;
	}

	public void SetEmpty()
	{
		if (_itemIcon != null)
			_itemIcon.Texture = null;
		if (_quantityLabel != null)
		{
			_quantityLabel.Visible = false;
			_quantityLabel.Text = string.Empty;
		}
	}

	public void SetItem(Texture2D? icon, int quantity)
	{
		if (_itemIcon != null)
			_itemIcon.Texture = icon;
		if (_quantityLabel != null)
		{
			var showQty = quantity > 1;
			_quantityLabel.Visible = showQty;
			_quantityLabel.Text = showQty ? quantity.ToString() : string.Empty;
		}
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		RefreshChrome();
	}

	/// <summary>Highlight valid equipment targets while choosing where to equip a multi-slot item (inventory notebook).</summary>
	public void SetPendingEquipCandidate(bool pending)
	{
		_pendingEquipCandidate = pending;
		RefreshChrome();
	}

	private void RefreshChrome()
	{
		var pendingGlow = _pendingEquipCandidate && !_selected;
		if (_selectionBorder != null)
		{
			_selectionBorder.Visible = _selected;
			Modulate = pendingGlow ? new Color(1.12f, 1.22f, 0.72f) : Colors.White;
		}
		else if (_selected)
			Modulate = new Color(1.15f, 1.15f, 1f);
		else if (_pendingEquipCandidate)
			Modulate = new Color(1.12f, 1.22f, 0.72f);
		else
			Modulate = Colors.White;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			AcceptEvent();
			SlotClicked?.Invoke(this);
		}
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		// Child controls default to MouseFilter.Stop and receive clicks first; _GuiInput on this node
		// would never run. Let hits reach the PanelContainer so selection / SlotClicked work.
		foreach (var child in GetChildren())
			SetDescendantsMouseFilterIgnore(child);

		if (_itemIcon == null || _quantityLabel == null)
			GD.PushWarning("InventorySlotControl: assign ItemIcon and QuantityLabel exports in the inspector.");

		SetSelected(false);
		SetEmpty();
	}

	private static void SetDescendantsMouseFilterIgnore(Node node)
	{
		if (node is Control c)
			c.MouseFilter = MouseFilterEnum.Ignore;
		foreach (var ch in node.GetChildren())
			SetDescendantsMouseFilterIgnore(ch);
	}
}
