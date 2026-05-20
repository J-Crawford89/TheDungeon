#nullable enable
using Godot;
using System;

public partial class MainUi : Control
{
	[Export] private MainViewPanel _mainViewPanel = null!;
	[Export] private CharacterPanel _characterPanel = null!;
	[Export] private CommandPanel _commandPanel = null!;
	[Export] private LogPanel _logPanel = null!;
	[Export] private MapPanel _mapPanel = null!;
	[Export] private GameOverOverlay _gameOverOverlay = null!;
	[Export] private NotebookOverlay? _notebookOverlay;

	[Export] private ContainerLootOverlay? _containerLootOverlay;
	[Export] private DiceRollOverlay? _diceRollOverlay;

	public event Action? QuitRequested;
	public event Action? ReturnToStartMenuRequested;

	private GameRunContext? _runContext;
	private bool _captureDebugDiagnostics;
	private bool _debugToolsEnabled;
	private Button? _debugRoomLootButton;

	private ExplorationUiPresenter _explorationPresenter = null!;
	private CombatUiPresenter _combatPresenter = null!;
	private GameUiCoordinator _coordinator = null!;

	public void Initialize(GameRunContext runContext, bool captureDebugDiagnostics, bool debugToolsEnabled = false)
	{
		_runContext = runContext;
		_captureDebugDiagnostics = captureDebugDiagnostics;
		_debugToolsEnabled = debugToolsEnabled;
	}

	public override void _Ready()
	{
		if (_runContext == null)
		{
			GD.PushError("MainUi.Initialize(GameRunContext, ...) must be called before the node enters the tree.");
			return;
		}

		_mainViewPanel.BindIconResolver(_runContext.Icons);

		_runContext.DicePresenterHost.Presenter = new GodotDiceRollPresenter(_diceRollOverlay);

		var session = _runContext.Session;
		var narrativeService = _runContext.NarrativeService;
		var explorationService = _runContext.ExplorationService;
		var combatService = _runContext.Combat;
		var treasurePickupService = _runContext.TreasurePickupService;
		var trapService = _runContext.TrapService;
		var potionEffects = _runContext.PotionEffects;
		var dungeonBootstrap = _runContext.DungeonBootstrap;

		Position = new Vector2(0, 0);
		Size = GetViewportRect().Size;
		GetViewport().SizeChanged += OnViewportSizeChanged;

		session.Debug.IsCaptureEnabled = _captureDebugDiagnostics;

		GameUiCoordinator? coordinator = null;
		void RefreshHudAndGameOver(UiRefreshFlags flags)
		{
			coordinator!.RefreshHud(flags);
			UpdateGameOverPanel();
		}

		_containerLootOverlay?.Bind(_runContext, RefreshHudAndGameOver);

		_explorationPresenter = new ExplorationUiPresenter(
			session,
			explorationService,
			narrativeService,
			treasurePickupService,
			_runContext.ContainerLootInteraction,
			trapService,
			potionEffects,
			dungeonBootstrap,
			RefreshHudAndGameOver,
			_containerLootOverlay);
		_combatPresenter = new CombatUiPresenter(session, combatService, RefreshHudAndGameOver);
		coordinator = new GameUiCoordinator(
			session,
			combatService,
			narrativeService,
			_explorationPresenter,
			_combatPresenter,
			_mainViewPanel,
			_characterPanel,
			_logPanel,
			_commandPanel,
			_mapPanel);
		_coordinator = coordinator;

		_mainViewPanel.TargetSlotHoverChanged += OnMainViewTargetHover;
		_mainViewPanel.TargetSlotClicked += OnMainViewTargetClick;

		if (_notebookOverlay == null)
			GD.PushError("MainUi: assign the Notebook Overlay export to your NotebookOverlay node.");
		else
		{
			_notebookOverlay.Bind(_runContext, f => _coordinator.RefreshHud(f));
			_notebookOverlay.HideNotebook();
			_notebookOverlay.ZIndex = 100;
		}

		_characterPanel.InventoryPressed += OnCharacterInventoryPressed;
		_characterPanel.CharacterPressed += OnCharacterCharacterPressed;

		_commandPanel.ForwardPressed += OnCommandForward;
		_commandPanel.BackwardPressed += OnCommandBackward;
		_commandPanel.LeftPressed += OnCommandLeft;
		_commandPanel.RightPressed += OnCommandRight;
		_commandPanel.InspectPressed += OnCommandInspect;
		_commandPanel.FloorUpPressed += OnCommandFloorUp;
		_commandPanel.FloorDownPressed += OnCommandFloorDown;
		_commandPanel.AttackPressed += OnCommandAttack;
		_commandPanel.FleePressed += OnCommandFlee;
		_commandPanel.TakePressed += OnCommandTake;
		_commandPanel.OpenPressed += OnCommandOpen;
		_commandPanel.PotionPressed += OnCommandPotion;
		_commandPanel.DefendPressed += OnCommandDefend;
		_commandPanel.DisarmPressed += OnCommandDisarm;
		_commandPanel.TargetSelectCancelPressed += OnTargetSelectCancel;
		_commandPanel.TargetSelectPicked += OnTargetSelectPicked;
		_commandPanel.TargetSelectHoverChanged += OnTargetSelectHoverChanged;

		_gameOverOverlay.ReturnToStartMenuPressed += OnGameOverReturnToMenu;
		_gameOverOverlay.QuitPressed += OnGameOverQuitPressed;

		_explorationPresenter.BootstrapDungeon();

		if (_debugToolsEnabled)
			AddDebugRoomLootButton();
	}

	private void OnCharacterInventoryPressed()
	{
		_notebookOverlay?.ShowInventory();
	}

	private void OnCharacterCharacterPressed()
	{
		_notebookOverlay?.ShowCharacter();
	}

	private void AddDebugRoomLootButton()
	{
		var b = new Button { Text = "Debug: +room loot" };
		b.Name = "DebugRoomLootButton";
		b.TooltipText = "Adds two revealed snares and two revealed health potion piles to the current room (debug builds only).";
		b.ZIndex = 100;
		b.SetAnchorsPreset(LayoutPreset.TopLeft);
		b.OffsetLeft = 12;
		b.OffsetRight = 220;
		b.OffsetTop = 12;
		b.OffsetBottom = 44;
		b.Pressed += OnDebugRoomLootPressed;
		AddChild(b);
		_debugRoomLootButton = b;
	}

	private void OnDebugRoomLootPressed()
	{
		if (_runContext == null)
			return;
		var session = _runContext.Session;
		if (!DebugRoomLootSpawn.TrySpawnSnaresAndPotions(session, _runContext.TrapDefinitions, _runContext.TreasureDefinitions))
			return;
		session.AppendGameLog("[Debug] Added two snares and two health potion piles to the current room.");
		_coordinator.RefreshHud(UiRefreshFlags.All);
		UpdateGameOverPanel();
	}

	private void OnViewportSizeChanged()
	{
		Size = GetViewportRect().Size;
	}

	private void OnCommandForward() => _coordinator.OnForwardPressed();
	private void OnCommandBackward() => _coordinator.OnBackwardPressed();
	private void OnCommandLeft() => _coordinator.OnTurn(DirectionTurned.Left);
	private void OnCommandRight() => _coordinator.OnTurn(DirectionTurned.Right);
	private void OnCommandInspect() => _coordinator.OnInspectPressed();
	private void OnCommandFloorUp() => _coordinator.OnFloorUpPressed();
	private void OnCommandFloorDown() => _coordinator.OnFloorDownPressed();
	private void OnCommandAttack() => _coordinator.OnAttackPressed();
	private void OnCommandFlee() => _coordinator.OnFleePressed();
	private void OnCommandTake() => _coordinator.OnTakePressed();
	private void OnCommandOpen() => _coordinator.OnOpenPressed();
	private void OnCommandPotion() => _coordinator.OnPotionPressed();
	private void OnCommandDefend() => _coordinator.OnDefendPressed();
	private void OnCommandDisarm() => _coordinator.OnDisarmPressed();

	private void OnTargetSelectCancel() => _coordinator.OnTargetSelectionCanceled();

	private void OnTargetSelectPicked(int index) => _coordinator.OnTargetSelectionPicked(index);

	private void OnTargetSelectHoverChanged(int? index) => _coordinator.OnTargetSelectionHoverChanged(index);

	private void OnMainViewTargetHover(string? highlightKey) => _coordinator.OnMainViewTargetHover(highlightKey);

	private void OnMainViewTargetClick(string highlightKey) => _coordinator.OnMainViewTargetClick(highlightKey);

	private void OnGameOverReturnToMenu() => ReturnToStartMenu();

	private void OnGameOverQuitPressed() => QuitRequested?.Invoke();

	private void ReturnToStartMenu()
	{
		_gameOverOverlay?.HidePanel();
		ReturnToStartMenuRequested?.Invoke();
	}

	private void UpdateGameOverPanel()
	{
		if (_gameOverOverlay == null || _runContext == null)
			return;
		var session = _runContext.Session;
		if (session.Phase == GamePlayPhase.GameOver &&
			session.GameOverTitle is { } title &&
			session.GameOverBody is { } body)
			_gameOverOverlay.ShowPanel(title, body);
		else
			_gameOverOverlay.HidePanel();
	}

	public override void _ExitTree()
	{
		if (GetViewport() != null)
			GetViewport().SizeChanged -= OnViewportSizeChanged;

		_characterPanel.InventoryPressed -= OnCharacterInventoryPressed;
		_characterPanel.CharacterPressed -= OnCharacterCharacterPressed;

		if (_mainViewPanel != null)
		{
			_mainViewPanel.TargetSlotHoverChanged -= OnMainViewTargetHover;
			_mainViewPanel.TargetSlotClicked -= OnMainViewTargetClick;
		}

		if (_commandPanel != null)
		{
			_commandPanel.ForwardPressed -= OnCommandForward;
			_commandPanel.BackwardPressed -= OnCommandBackward;
			_commandPanel.LeftPressed -= OnCommandLeft;
			_commandPanel.RightPressed -= OnCommandRight;
			_commandPanel.InspectPressed -= OnCommandInspect;
			_commandPanel.FloorUpPressed -= OnCommandFloorUp;
			_commandPanel.FloorDownPressed -= OnCommandFloorDown;
			_commandPanel.AttackPressed -= OnCommandAttack;
			_commandPanel.FleePressed -= OnCommandFlee;
			_commandPanel.TakePressed -= OnCommandTake;
			_commandPanel.OpenPressed -= OnCommandOpen;
			_commandPanel.PotionPressed -= OnCommandPotion;
			_commandPanel.DefendPressed -= OnCommandDefend;
			_commandPanel.DisarmPressed -= OnCommandDisarm;
			_commandPanel.TargetSelectCancelPressed -= OnTargetSelectCancel;
			_commandPanel.TargetSelectPicked -= OnTargetSelectPicked;
			_commandPanel.TargetSelectHoverChanged -= OnTargetSelectHoverChanged;
		}

		if (_gameOverOverlay != null)
		{
			_gameOverOverlay.ReturnToStartMenuPressed -= OnGameOverReturnToMenu;
			_gameOverOverlay.QuitPressed -= OnGameOverQuitPressed;
		}

		if (_debugRoomLootButton != null)
		{
			_debugRoomLootButton.Pressed -= OnDebugRoomLootPressed;
			_debugRoomLootButton = null;
		}
	}
}
