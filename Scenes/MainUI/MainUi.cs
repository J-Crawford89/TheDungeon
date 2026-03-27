using Godot;
using System;

public partial class MainUi : Control
{
	[Export] private MainViewPanel _mainViewPanel;
	[Export] private CharacterPanel _characterPanel;
	[Export] private CommandPanel _commandPanel;
	[Export] private LogPanel _logPanel;
	[Export] private MapPanel _mapPanel;

	[Export] public bool CaptureDebugDiagnostics { get; set; }

	private readonly GameSessionState _session = new();
	private readonly Random _random = new();
	private readonly RoomFeaturePopulationService _roomFeaturePopulation = new();
	private readonly DungeonBootstrap _dungeonBootstrap;
	private readonly ExplorationService _explorationService;
	private readonly NarrativeService _narrativeService = new();
	private readonly DiceRollService _diceRollService;
	private readonly ResolutionService _resolutionService;
	private readonly TreasurePickupService _treasurePickupService;
	private readonly CombatService _combatService;
	private readonly PlayerDefeatService _playerDefeatService;

	private ExplorationUiPresenter _explorationPresenter = null!;
	private CombatUiPresenter _combatPresenter = null!;
	private GameUiCoordinator _coordinator = null!;

	public MainUi()
	{
		_dungeonBootstrap = new DungeonBootstrap(_roomFeaturePopulation);
		_explorationService = new ExplorationService(_roomFeaturePopulation);
		_diceRollService = new DiceRollService(_random);
		_resolutionService = new ResolutionService(_diceRollService);
		_playerDefeatService = new PlayerDefeatService(_narrativeService);
		_treasurePickupService = new TreasurePickupService(_narrativeService);
		_combatService = new CombatService(
			_diceRollService,
			_resolutionService,
			_narrativeService,
			_playerDefeatService,
			_treasurePickupService);
	}

	public override void _Ready()
	{
		Position = new Vector2(0,0);
		Size = GetViewportRect().Size;
		GetViewport().SizeChanged += () => Size = GetViewportRect().Size;

		_session.Debug.IsCaptureEnabled = CaptureDebugDiagnostics;

		GameUiCoordinator? coordinator = null;
		_explorationPresenter = new ExplorationUiPresenter(
			_session,
			_explorationService,
			_narrativeService,
			_combatService,
			_treasurePickupService,
			_dungeonBootstrap,
			f => coordinator!.RefreshHud(f));
		_combatPresenter = new CombatUiPresenter(_session, _combatService, f => coordinator!.RefreshHud(f));
		coordinator = new GameUiCoordinator(
			_session,
			_explorationPresenter,
			_combatPresenter,
			_mainViewPanel,
			_characterPanel,
			_logPanel,
			_commandPanel,
			_mapPanel);
		_coordinator = coordinator;

		_commandPanel.ForwardPressed += () => _coordinator.OnForwardPressed();
		_commandPanel.BackwardPressed += () => _coordinator.OnBackwardPressed();
		_commandPanel.LeftPressed += () => _coordinator.OnTurn(DirectionTurned.Left);
		_commandPanel.RightPressed += () => _coordinator.OnTurn(DirectionTurned.Right);
		_commandPanel.InspectPressed += () => _coordinator.OnInspectPressed();
		_commandPanel.FloorUpPressed += () => _coordinator.OnFloorUpPressed();
		_commandPanel.FloorDownPressed += () => _coordinator.OnFloorDownPressed();
		_commandPanel.AttackPressed += () => _coordinator.OnAttackPressed();
		_commandPanel.FleePressed += () => _coordinator.OnFleePressed();
		_commandPanel.TakePressed += () => _coordinator.OnTakePressed();
		_commandPanel.PotionPressed += () => _coordinator.OnPotionPressed();

		_explorationPresenter.BootstrapDungeon();
	}
}
