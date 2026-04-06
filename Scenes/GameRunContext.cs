#nullable enable
using System;

public sealed class GameRunContext
{
	public GameSessionState Session { get; }
	public Random Random { get; }
	public NarrativeService NarrativeService { get; }
	public RoomFeaturePopulationService RoomFeaturePopulation { get; }
	public DungeonBootstrap DungeonBootstrap { get; }
	public ExplorationService ExplorationService { get; }
	public DiceRollService DiceRollService { get; }
	public ResolutionService ResolutionService { get; }
	public TreasurePickupService TreasurePickupService { get; }
	public PlayerVitalsService VitalsService { get; }
	public GameOverDownedHandler GameOverDownedHandler { get; }
	public PlayerDownedResolutionService PlayerDownedResolutionService { get; }
	public CombatService CombatService { get; }
	public CharacterCreationService CharacterCreation { get; }
	public ICharacterClassDefinitionRepository CharacterClasses { get; }
	public ICharacterRaceDefinitionRepository CharacterRaces { get; }
	public ICharacterBackgroundDefinitionRepository CharacterBackgrounds { get; }

	public GameRunContext(
		GameSessionState session,
		Random random,
		NarrativeService narrativeService,
		RoomFeaturePopulationService roomFeaturePopulation,
		DungeonBootstrap dungeonBootstrap,
		ExplorationService explorationService,
		DiceRollService diceRollService,
		ResolutionService resolutionService,
		TreasurePickupService treasurePickupService,
		PlayerVitalsService vitalsService,
		GameOverDownedHandler gameOverDownedHandler,
		PlayerDownedResolutionService playerDownedResolutionService,
		CombatService combatService,
		CharacterCreationService characterCreation,
		ICharacterClassDefinitionRepository characterClasses,
		ICharacterRaceDefinitionRepository characterRaces,
		ICharacterBackgroundDefinitionRepository characterBackgrounds)
	{
		Session = session;
		Random = random;
		NarrativeService = narrativeService;
		RoomFeaturePopulation = roomFeaturePopulation;
		DungeonBootstrap = dungeonBootstrap;
		ExplorationService = explorationService;
		DiceRollService = diceRollService;
		ResolutionService = resolutionService;
		TreasurePickupService = treasurePickupService;
		VitalsService = vitalsService;
		GameOverDownedHandler = gameOverDownedHandler;
		PlayerDownedResolutionService = playerDownedResolutionService;
		CombatService = combatService;
		CharacterCreation = characterCreation;
		CharacterClasses = characterClasses;
		CharacterRaces = characterRaces;
		CharacterBackgrounds = characterBackgrounds;
	}
}
