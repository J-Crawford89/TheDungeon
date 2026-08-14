#nullable enable
using System;
using Godot;
using System.Collections.Generic;

public partial class MainViewPanel : PanelContainer
{
	private const float HighlightModulate = 1.35f;

	[Export] private Control _viewArea = null!;
	[Export] private Control _stage = null!;

	[Export] private Label _mainViewTitle = null!;
	[Export] private TextureRect _backgroundRect = null!;
	[Export] private TextureRect _leftWallRect = null!;
	[Export] private TextureRect _backWallRect = null!;
	[Export] private TextureRect _rightWallRect = null!;
	[Export] private HBoxContainer _featureContainer = null!;
	[Export] private InitiativeStripView? _initiativeStrip;

	[Export] private Texture2D _backgroundTexture = null!;
	[Export] private Texture2D _doorTexture = null!;
	[Export] private Texture2D _passageTexture = null!;

	public InitiativeStripView? InitiativeStrip => _initiativeStrip;

	private IconResolver? _iconResolver;

	private readonly Dictionary<string, Control> _slotRootByHighlightKey = new();
	private IReadOnlyList<string>? _activeHighlightKeys;

	/// <summary>Emitted when the pointer enters/exits a feature slot (highlight key, or <c>null</c> on exit).</summary>
	public event Action<string?>? TargetSlotHoverChanged;

	/// <summary>Emitted on left-button press on a feature slot while it is a valid target.</summary>
	public event Action<string>? TargetSlotClicked;

	public void BindIconResolver(IconResolver resolver) =>
		_iconResolver = resolver;

	public override void _Ready()
	{
		if (_initiativeStrip == null)
			GD.PushError("MainViewPanel: assign the Initiative Strip export to InitiativeStrip.");
		if (_backgroundRect != null)
			_backgroundRect.Texture = _backgroundTexture;

		CallDeferred(nameof(CenterStage));
		Resized += CenterStage;
	}

	public void CenterStage()
	{
		if (_viewArea == null || _stage == null)
			return;
		_stage.Position = new Vector2((_viewArea.Size.X - _stage.Size.X) / 2f, (_viewArea.Size.Y - _stage.Size.Y) / 2f);
	}

	public void SetTitle(string text)
	{
		if (_mainViewTitle != null)
			_mainViewTitle.Text = text;
	}

	public void Render(MainViewRenderModel model)
	{
		_activeHighlightKeys = null;
		SetTitle(model.Title);
		ApplyWall(_leftWallRect, model.LeftConnection);
		ApplyWall(_backWallRect, model.FrontConnection);
		ApplyWall(_rightWallRect, model.RightConnection);

		ClearFeatures();
		if (_featureContainer == null)
			return;
		foreach (var slot in model.FeatureSlots)
			AddFeatureSlot(slot);
		ApplyHighlightVisuals();
	}

	/// <summary>Highlight one main-view slot (or clear when null).</summary>
	public void SetTargetingHighlight(string? highlightKey) =>
		SetTargetingHighlight(highlightKey == null ? null : new[] { highlightKey });

	/// <summary>Highlight multiple slots (e.g. Take All hover). Pass null or empty to clear.</summary>
	public void SetTargetingHighlight(IReadOnlyList<string>? highlightKeys)
	{
		_activeHighlightKeys = highlightKeys is { Count: > 0 } ? highlightKeys : null;
		ApplyHighlightVisuals();
	}

	public void ClearTargetingHighlight() => SetTargetingHighlight((IReadOnlyList<string>?)null);

	private void ApplyHighlightVisuals()
	{
		foreach (var (key, root) in _slotRootByHighlightKey)
		{
			if (!GodotObject.IsInstanceValid(root))
				continue;
			var on = _activeHighlightKeys != null && ContainsHighlight(key);
			root.Modulate = on ? new Color(HighlightModulate, HighlightModulate, 0.85f) : Colors.White;
		}
	}

	private bool ContainsHighlight(string key)
	{
		if (_activeHighlightKeys == null)
			return false;
		foreach (var k in _activeHighlightKeys)
		{
			if (k == key)
				return true;
		}

		return false;
	}

	private void ApplyWall(TextureRect rect, RoomConnectionType connection)
	{
		if (!GodotObject.IsInstanceValid(rect))
			return;
		switch (connection)
		{
			case RoomConnectionType.Door:
				ShowWallTexture(rect, _doorTexture);
				break;
			case RoomConnectionType.Passage:
				ShowWallTexture(rect, _passageTexture);
				break;
			default:
				HideWallTexture(rect);
				break;
		}
	}

	public void ClearFeatures()
	{
		_slotRootByHighlightKey.Clear();
		if (_featureContainer == null)
			return;
		foreach (Node child in _featureContainer.GetChildren())
			child.QueueFree();
	}

	private void AddFeatureSlot(MainViewFeatureSlot slot)
	{
		if (_featureContainer == null)
			return;

		var root = new VBoxContainer();
		root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		root.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		root.MouseFilter = Control.MouseFilterEnum.Stop;

		var tex = _iconResolver?.Resolve(slot.PresentationIconKey);
		if (tex != null)
		{
			var textureRect = new TextureRect();
			textureRect.Texture = tex;
			textureRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			textureRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			textureRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			textureRect.CustomMinimumSize = new Vector2(80, 80);
			root.AddChild(textureRect);
		}
		else
		{
			var placeholder = new Control();
			placeholder.MouseFilter = Control.MouseFilterEnum.Ignore;
			placeholder.CustomMinimumSize = new Vector2(80, 80);
			root.AddChild(placeholder);
		}

		if (!string.IsNullOrEmpty(slot.TargetingLabel))
		{
			var panel = new PanelContainer();
			panel.MouseFilter = Control.MouseFilterEnum.Ignore;
			panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			panel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			var backing = new StyleBoxFlat
			{
				BgColor = new Color(0.2f, 0.2f, 0.2f, 0.95f)
			};
			backing.SetContentMarginAll(6);
			panel.AddThemeStyleboxOverride("panel", backing);

			var label = new Label();
			label.MouseFilter = Control.MouseFilterEnum.Ignore;
			label.Text = slot.TargetingLabel;
			// Word wrap uses the parent's width budget; in a tight HBox slot that can collapse to ~1 char wide.
			label.AutowrapMode = TextServer.AutowrapMode.Off;
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			label.AddThemeColorOverride("font_color", Colors.White);
			panel.AddChild(label);
			root.AddChild(panel);
		}

		var hk = slot.HighlightKey;
		root.MouseEntered += () => TargetSlotHoverChanged?.Invoke(hk);
		root.MouseExited += () => TargetSlotHoverChanged?.Invoke(null);
		root.GuiInput += ev =>
		{
			if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				TargetSlotClicked?.Invoke(hk);
		};

		_featureContainer.AddChild(root);
		_slotRootByHighlightKey[slot.HighlightKey] = root;
	}

	private static void ShowWallTexture(TextureRect rect, Texture2D texture)
	{
		rect.Texture = texture;
		rect.Visible = texture != null;
	}

	private static void HideWallTexture(TextureRect rect)
	{
		rect.Texture = null;
		rect.Visible = false;
	}
}
