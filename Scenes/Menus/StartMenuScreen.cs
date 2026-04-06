#nullable enable
using Godot;
using System;

public partial class StartMenuScreen : Control
{
	[Export] private Button _newGameButton = null!;
	[Export] private Button _loadGameButton = null!;
	[Export] private Button _optionsButton = null!;
	[Export] private Button _quitButton = null!;

	public event Action? NewGameButtonPressed;
	public event Action? LoadGameButtonPressed;
	public event Action? OptionsButtonPressed;
	public event Action? QuitButtonPressed;

	public override void _Ready()
	{
		_newGameButton.Pressed += () => NewGameButtonPressed?.Invoke();
		_loadGameButton.Pressed += () => LoadGameButtonPressed?.Invoke();
		_optionsButton.Pressed += () => OptionsButtonPressed?.Invoke();
		_quitButton.Pressed += () => QuitButtonPressed?.Invoke();
	}
}
