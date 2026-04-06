using Godot;
using System;

public partial class CharacterPanel : PanelContainer
{
	[Export] private Label _hpLabel;
	[Export] private Label _levelLabel;
	[Export] private Label _goldLabel;
	[Export] private Label _equipmentLabel;
	[Export] private Label _nameLabel;

	public void Render(PlayerState player)
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
	}
}
