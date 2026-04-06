#nullable enable
using Godot;
using System;

public partial class GameRoot : Control
{
	[Export] private Control _screenHost = null!;
	[Export] private PackedScene _startMenuScene = null!;
	[Export] private PackedScene _characterCreationScene = null!;
	[Export] private PackedScene _mainUiScene = null!;

	[Export] public MonsterResourceDatabase? MonsterDatabase { get; set; }
	[Export] public TrapResourceDatabase? TrapDatabase { get; set; }
	[Export] public TreasureResourceDatabase? TreasureDatabase { get; set; }
	[Export] public NpcResourceDatabase? NpcDatabase { get; set; }
	[Export] public LoreResourceDatabase? LoreDatabase { get; set; }

	[Export] public CharacterClassResourceDatabase? CharacterClassDatabase { get; set; }
	[Export] public CharacterRaceResourceDatabase? CharacterRaceDatabase { get; set; }
	[Export] public CharacterBackgroundResourceDatabase? CharacterBackgroundDatabase { get; set; }

	[Export] public bool CaptureDebugDiagnostics { get; set; }

	private GameRunContext? _activeRunContext;

	public override void _Ready()
	{
		if (_startMenuScene == null || _characterCreationScene == null || _mainUiScene == null)
		{
			GD.PushError("GameRoot: assign PackedScene exports for start menu, character creation, and main UI.");
			return;
		}

		ShowStartMenu();
	}

	private void ShowStartMenu()
	{
		_activeRunContext = null;
		ClearScreenHost();

		var menu = _startMenuScene.Instantiate<StartMenuScreen>();
		menu.NewGameButtonPressed += HandleNewGame;
		menu.LoadGameButtonPressed += HandleLoadGame;
		menu.OptionsButtonPressed += HandleMainOptions;
		menu.QuitButtonPressed += HandleQuitGame;
		_screenHost.AddChild(menu);
	}

	private void ClearScreenHost()
	{
		if (_screenHost == null)
			return;

		while (_screenHost.GetChildCount() > 0)
		{
			var child = _screenHost.GetChild(0);
			if (child is Control control)
				UnsubscribeScreen(control);
			_screenHost.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void UnsubscribeScreen(Control screen)
	{
		switch (screen)
		{
			case StartMenuScreen sm:
				sm.NewGameButtonPressed -= HandleNewGame;
				sm.LoadGameButtonPressed -= HandleLoadGame;
				sm.OptionsButtonPressed -= HandleMainOptions;
				sm.QuitButtonPressed -= HandleQuitGame;
				break;
			case CharacterCreationScreen cc:
				cc.BackButtonPressed -= HandleReturnToStartMenu;
				cc.StartGameRequested -= HandleStartGame;
				break;
			case MainUi mu:
				mu.QuitRequested -= HandleQuitGame;
				mu.ReturnToStartMenuRequested -= HandleReturnToStartMenu;
				break;
		}
	}

	private void HandleNewGame()
	{
		_activeRunContext = BuildGameRunContext();
		ClearScreenHost();

		var cc = _characterCreationScene.Instantiate<CharacterCreationScreen>();
		cc.Initialize(_activeRunContext, CaptureDebugDiagnostics);
		cc.BackButtonPressed += HandleReturnToStartMenu;
		cc.StartGameRequested += HandleStartGame;
		_screenHost.AddChild(cc);
	}

	private void HandleLoadGame()
	{
	}

	private void HandleMainOptions()
	{
	}

	private void HandleQuitGame()
	{
		GetTree().Quit();
	}

	private void HandleStartGame(CharacterCreationState creation)
	{
		if (_activeRunContext == null)
		{
			GD.PushError("GameRoot.HandleStartGame: no active run context.");
			return;
		}

		_activeRunContext.CharacterCreation.ApplyToPlayer(creation, _activeRunContext.Session.Player);

		ClearScreenHost();

		var mainUi = _mainUiScene.Instantiate<MainUi>();
		mainUi.Initialize(_activeRunContext, CaptureDebugDiagnostics);
		mainUi.QuitRequested += HandleQuitGame;
		mainUi.ReturnToStartMenuRequested += HandleReturnToStartMenu;
		_screenHost.AddChild(mainUi);
	}

	private void HandleReturnToStartMenu()
	{
		ShowStartMenu();
	}

	private GameRunContext BuildGameRunContext()
	{
		LogArchive.FileWriter = new GodotLogFileWriter();

		var session = new GameSessionState();
		var random = new Random();
		var narrativeService = new NarrativeService();

		var monsterRepo = new GodotMonsterDefinitionRepository(MonsterDatabase);
		var trapRepo = new GodotTrapDefinitionRepository(TrapDatabase);
		var treasureRepo = new GodotTreasureDefinitionRepository(TreasureDatabase);
		var npcRepo = new GodotNpcDefinitionRepository(NpcDatabase);
		var loreRepo = new GodotLoreDefinitionRepository(LoreDatabase);

		var classRepo = new GodotCharacterClassDefinitionRepository(CharacterClassDatabase);
		var raceRepo = new GodotCharacterRaceDefinitionRepository(CharacterRaceDatabase);
		var backgroundRepo = new GodotCharacterBackgroundDefinitionRepository(CharacterBackgroundDatabase);

		var roomFeaturePopulation = new RoomFeaturePopulationService(monsterRepo, trapRepo, treasureRepo, npcRepo, loreRepo);
		var dungeonBootstrap = new DungeonBootstrap(roomFeaturePopulation);
		var explorationService = new ExplorationService(roomFeaturePopulation);
		var diceRollService = new DiceRollService(random);
		var resolutionService = new ResolutionService(diceRollService);
		var treasurePickupService = new TreasurePickupService(narrativeService);
		var vitalsService = new PlayerVitalsService();
		var gameOverDownedHandler = new GameOverDownedHandler(narrativeService);
		var playerDownedResolutionService = new PlayerDownedResolutionService(new IPlayerDownedOutcomeHandler[] { gameOverDownedHandler });
		var combatService = new CombatService(
			diceRollService,
			resolutionService,
			narrativeService,
			vitalsService,
			playerDownedResolutionService,
			treasurePickupService);
		var characterCreation = new CharacterCreationService(diceRollService, random);

		return new GameRunContext(
			session,
			random,
			narrativeService,
			roomFeaturePopulation,
			dungeonBootstrap,
			explorationService,
			diceRollService,
			resolutionService,
			treasurePickupService,
			vitalsService,
			gameOverDownedHandler,
			playerDownedResolutionService,
			combatService,
			characterCreation,
			classRepo,
			raceRepo,
			backgroundRepo);
	}
}
