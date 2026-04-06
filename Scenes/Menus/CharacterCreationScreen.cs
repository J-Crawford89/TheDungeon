#nullable enable
using Godot;
using System;

public partial class CharacterCreationScreen : Control
{
	[Export] private Button _rerollAbilityScoresButton = null!;
	[Export] private Button _backButton = null!;
	[Export] private Button _startGameButton = null!;

	public event Action? BackButtonPressed;
	public event Action? StartGameButtonPressed;

	private GameRunContext? _runContext;
	private bool _captureDebugDiagnostics;

	public void Initialize(GameRunContext runContext, bool captureDebugDiagnostics)
	{
		_runContext = runContext;
		_captureDebugDiagnostics = captureDebugDiagnostics;
	}


	public override void _Ready()
	{
		_rerollAbilityScoresButton.Pressed += () => GD.Print("Reroll ability scores button pressed");
		_backButton.Pressed += () => BackButtonPressed?.Invoke();
		_startGameButton.Pressed += () => StartGameButtonPressed?.Invoke();
	}
}
