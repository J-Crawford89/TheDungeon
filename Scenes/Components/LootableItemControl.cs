#nullable enable
using System;
using Godot;

/// <summary>One selectable loot stack item in the container loot grid.</summary>
public partial class LootableItemControl : PanelContainer
{
	[Export] private TextureRect? _icon;
	[Export] private Label? _qty;
	[Export] private Panel? _selectionBorder;
	[Export] private Label? _itemName;
	/// <summary>Optional; assign in Inspector to show per-stack harvest requirement text.</summary>
	[Export] private Label? _harvestHint;

	/// <summary>Index into the container&apos;s <c>Contents</c> list (matches <see cref="ContainerLootStackRowDto.RowIndex"/>).</summary>
	private int _stackIndex;

	public void Configure(
		int stackIndex,
		string displayName,
		int quantity,
		Texture2D? iconTex,
		int? harvestDc,
		AbilityScore? harvestAbility,
		Action<int>? onStackToggled)
	{
		_stackIndex = stackIndex;
		if (_qty != null)
			_qty.Text = quantity.ToString();
		if (_itemName != null)
			_itemName.Text = displayName;
		if (_icon != null)
			_icon.Texture = iconTex;
		if (_selectionBorder != null)
			_selectionBorder.Visible = false;
		if (_harvestHint != null)
		{
			if (harvestDc is > 0 && harvestAbility is { } ab)
			{
				_harvestHint.Visible = true;
				var abilityName = ab.ToString();
				var abbrev = abilityName.Length <= 3 ? abilityName : abilityName[..3];
				_harvestHint.Text = $"{abbrev} DC: {harvestDc}";
			}
			else
			{
				_harvestHint.Visible = false;
				_harvestHint.Text = "";
			}
		}

		_onStackToggled = onStackToggled;
	}

	public void SetSelected(bool selected)
	{
		if (_selectionBorder != null)
			_selectionBorder.Visible = selected;
	}

	public int StackIndex => _stackIndex;

	private Action<int>? _onStackToggled;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		SetDescendantsMouseIgnore(this);
	}

	/// <summary>Let this control receive clicks on its full bounds; children must not steal hits.</summary>
	private static void SetDescendantsMouseIgnore(Node root)
	{
		foreach (var child in root.GetChildren())
		{
			if (child is Control c)
				c.MouseFilter = MouseFilterEnum.Ignore;
			SetDescendantsMouseIgnore(child);
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb &&
			mb.ButtonIndex == MouseButton.Left &&
			mb.Pressed)
		{
			_onStackToggled?.Invoke(_stackIndex);
			AcceptEvent();
		}
	}
}
