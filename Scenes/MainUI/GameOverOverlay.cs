#nullable enable
using Godot;
using System;

public partial class GameOverOverlay : Control
{
	[Export] private Label _titleLabel = null!;
	[Export] private Label _bodyLabel = null!;
	[Export] private Button _newGameButton = null!;
	[Export] private Button _quitButton = null!;

	public event Action? ReturnToMenuPressed;
	public event Action? QuitPressed;

    public override void _Ready()
	{
		_newGameButton.Pressed += () => ReturnToMenuPressed?.Invoke();
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
