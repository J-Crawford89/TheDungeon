#nullable enable
using Godot;
using System;

public partial class GameOverOverlay : Control
{
	[Export] private Label _titleLabel = null!;
	[Export] private Label _bodyLabel = null!;
	[Export] private Button _returnToStartMenuButton = null!;
	[Export] private Button _quitButton = null!;

	public event Action? ReturnToStartMenuPressed;
	public event Action? QuitPressed;

    public override void _Ready()
	{
		_returnToStartMenuButton.Pressed += () => ReturnToStartMenuPressed?.Invoke();
		_quitButton.Pressed += () => QuitPressed?.Invoke();
        Visible = false;
	}

	public void ShowPanel(string title, string body)
	{
		_titleLabel.Text = title;
		_bodyLabel.Text = body;
		Visible = true;
	}

	public void HidePanel() =>
		Visible = false;
}
