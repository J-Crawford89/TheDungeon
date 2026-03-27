using Godot;
using System.Collections.Generic;

public partial class MainViewPanel : PanelContainer
{
	[Export] private Control _viewArea;
	[Export] private Control _stage;

	[Export] private Label _mainViewTitle;
	[Export] private TextureRect _backgroundRect;
	[Export] private TextureRect _leftWallRect;
	[Export] private TextureRect _backWallRect;
	[Export] private TextureRect _rightWallRect;
	[Export] private HBoxContainer _featureContainer;

	[Export] private Texture2D _backgroundTexture;
	[Export] private Texture2D _doorTexture;
	[Export] private Texture2D _passageTexture;

	[Export] private Texture2D _ratTexture;
	[Export] private Texture2D _giantRatTexture;
	[Export] private Texture2D _ratKingTexture;
	[Export] private Texture2D _coinsTexture;
	[Export] private Texture2D _healthPotionTexture;
	[Export] private Texture2D _loreTexture;
	[Export] private Texture2D _npcTexture;
	[Export] private Texture2D _trapTexture;
	[Export] private Texture2D _stairsTexture;
	[Export] private Texture2D _holeTexture;
	[Export] private Texture2D _ladderTexture;

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

	private Texture2D TextureForFeatureIcon(MainViewFeatureIconKind kind) =>
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
		textureRect.CustomMinimumSize = new Vector2(64, 64);
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
