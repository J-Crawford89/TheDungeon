#nullable enable
using Godot;
using System.Collections.Generic;

public partial class MainViewPanel : PanelContainer
{
	[Export] private Control _viewArea = null!;
	[Export] private Control _stage = null!;

	[Export] private Label _mainViewTitle = null!;
	[Export] private TextureRect _backgroundRect = null!;
	[Export] private TextureRect _leftWallRect = null!;
	[Export] private TextureRect _backWallRect = null!;
	[Export] private TextureRect _rightWallRect = null!;
	[Export] private HBoxContainer _featureContainer = null!;

	[Export] private Texture2D _backgroundTexture = null!;
	[Export] private Texture2D _doorTexture = null!;
	[Export] private Texture2D _passageTexture = null!;

	[Export] private Texture2D _ratTexture = null!;
	[Export] private Texture2D _giantRatTexture = null!;
	[Export] private Texture2D _ratKingTexture = null!;
	[Export] private Texture2D _coinsTexture = null!;
	[Export] private Texture2D _healthPotionTexture = null!;
	[Export] private Texture2D _loreTexture = null!;
	[Export] private Texture2D _npcTexture = null!;
	[Export] private Texture2D _trapTexture = null!;
	[Export] private Texture2D _stairsTexture = null!;
	[Export] private Texture2D _holeTexture = null!;
	[Export] private Texture2D _ladderTexture = null!;

	public override void _Ready()
	{
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
		SetTitle(model.Title);
		ApplyWall(_leftWallRect, model.LeftConnection);
		ApplyWall(_backWallRect, model.FrontConnection);
		ApplyWall(_rightWallRect, model.RightConnection);

		ClearFeatures();
		if (_featureContainer == null)
			return;
		foreach (var kind in model.FeatureIcons)
		{
			var tex = TextureForFeatureIcon(kind);
			if (tex != null)
				AddFeatureTexture(tex);
		}
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

	private Texture2D? TextureForFeatureIcon(MainViewFeatureIconKind kind) =>
		kind switch
		{
			MainViewFeatureIconKind.Rat => _ratTexture,
			MainViewFeatureIconKind.GiantRat => _giantRatTexture,
			MainViewFeatureIconKind.RatKing => _ratKingTexture,
			MainViewFeatureIconKind.Coins => _coinsTexture,
			MainViewFeatureIconKind.HealthPotion => _healthPotionTexture,
			MainViewFeatureIconKind.Lore => _loreTexture,
			MainViewFeatureIconKind.Npc => _npcTexture,
			MainViewFeatureIconKind.Trap => _trapTexture,
			MainViewFeatureIconKind.Stairs => _stairsTexture,
			MainViewFeatureIconKind.Hole => _holeTexture,
			MainViewFeatureIconKind.Ladder => _ladderTexture,
			_ => null
		};

	public void ClearFeatures()
	{
		if (_featureContainer == null)
			return;
		foreach (Node child in _featureContainer.GetChildren())
			child.QueueFree();
	}

	public void AddFeatureTexture(Texture2D texture)
	{
		if (texture == null || _featureContainer == null)
			return;

		var textureRect = new TextureRect();
		textureRect.Texture = texture;
		textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		textureRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		textureRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		textureRect.CustomMinimumSize = new Vector2(80, 80);

		_featureContainer.AddChild(textureRect);
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
