#nullable enable
using Godot;
using System;

public partial class CharacterPanel : PanelContainer
{
	[Export] private Label _hpLabel = null!;
	[Export] private Label _levelLabel = null!;
	[Export] private Label _goldLabel = null!;
	[Export] private Label _equipmentLabel = null!;
	[Export] private Label _nameLabel = null!;
	[Export] private Label? _spLabel;
	[Export] private Control? _defendActiveIndicator;

	public void Render(PlayerState player, GameSessionState? session = null)
	{
		_nameLabel.Text = $"Name: {player.Name}";
		_hpLabel.Text = $"HP: {player.CurrentHp} / {player.MaxHp}";
		_levelLabel.Text = $"LEVEL: {player.Level}";
		if (_goldLabel != null)
			_goldLabel.Text = $"GOLD: {player.Gold}";
		var itemsLine = player.HealthPotionCount > 0
			? $"Health Potion ×{player.HealthPotionCount}"
			: "(none)";
		_equipmentLabel.Text = $"ITEMS: {itemsLine}";

		if (_spLabel != null)
		{
			if (player.CurrentSpellPoints is { } curSp && player.MaxSpellPoints is { } maxSp)
			{
				_spLabel.Visible = true;
				_spLabel.Text = $"SP: {curSp} / {maxSp}";
			}
			else
				_spLabel.Visible = false;
		}

		if (_defendActiveIndicator != null)
		{
			var defendActive = session?.Combat?.HasDefendStanceActive() == true;
			_defendActiveIndicator.Visible = defendActive;
			if (_defendActiveIndicator is Label defendLabel)
				defendLabel.Text = "Defend: active";
		}
	}
}
