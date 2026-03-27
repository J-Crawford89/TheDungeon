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

	[Export] public MonsterResourceDatabase? MonsterDatabase { get; set; }
	[Export] public TrapResourceDatabase? TrapDatabase { get; set; }
	[Export] public TreasureResourceDatabase? TreasureDatabase { get; set; }
	[Export] public NpcResourceDatabase? NpcDatabase { get; set; }
	[Export] public LoreResourceDatabase? LoreDatabase { get; set; }

	[Export] public bool CaptureDebugDiagnostics { get; set; }

	private readonly GameSessionState _session = new();
	private readonly Random _random = new();
	private readonly NarrativeService _narrativeService = new();

	private RoomFeaturePopulationService _roomFeaturePopulation = null!;
	private DungeonBootstrap _dungeonBootstrap = null!;
	private ExplorationService _explorationService = null!;
	private DiceRollService _diceRollService = null!;
	private ResolutionService _resolutionService = null!;
	private TreasurePickupService _treasurePickupService = null!;
	private CombatService _combatService = null!;
	private PlayerDefeatService _playerDefeatService = null!;

	private ExplorationUiPresenter _explorationPresenter = null!;
	private CombatUiPresenter _combatPresenter = null!;
	private GameUiCoordinator _coordinator = null!;

	public override void _Ready()
	{
		LogArchive.FileWriter = new GodotLogFileWriter();

		var monsterRepo = new GodotMonsterDefinitionRepository(MonsterDatabase);
		var trapRepo = new GodotTrapDefinitionRepository(TrapDatabase);
		var treasureRepo = new GodotTreasureDefinitionRepository(TreasureDatabase);
		var npcRepo = new GodotNpcDefinitionRepository(NpcDatabase);
		var loreRepo = new GodotLoreDefinitionRepository(LoreDatabase);

		_roomFeaturePopulation = new RoomFeaturePopulationService(monsterRepo, trapRepo, treasureRepo, npcRepo, loreRepo);
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

		Position = new Vector2(0, 0);
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
