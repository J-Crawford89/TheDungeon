#nullable enable
using System;
using System.Collections.Generic;

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
	public ICombatService Combat { get; }
	public CharacterCreationService CharacterCreation { get; }
	public ICharacterClassDefinitionRepository CharacterClasses { get; }
	public ICharacterRaceDefinitionRepository CharacterRaces { get; }
	public ICharacterBackgroundDefinitionRepository CharacterBackgrounds { get; }
	public IAbilityDefinitionRepository AbilityDefinitions { get; }
	public IDamageTypeDefinitionRepository DamageTypeDefinitions { get; }
	public IItemDefinitionRepository ItemDefinitions { get; }
	public PotionEffectApplicationService PotionEffects { get; }
	public TrapService TrapService { get; }
	public ITrapDefinitionRepository TrapDefinitions { get; }
	public ITreasureDefinitionRepository TreasureDefinitions { get; }

	public IconResolver Icons { get; }

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
		ICombatService combat,
		CharacterCreationService characterCreation,
		ICharacterClassDefinitionRepository characterClasses,
		ICharacterRaceDefinitionRepository characterRaces,
		ICharacterBackgroundDefinitionRepository characterBackgrounds,
		IAbilityDefinitionRepository abilityDefinitions,
		IDamageTypeDefinitionRepository damageTypeDefinitions,
		IItemDefinitionRepository itemDefinitions,
		PotionEffectApplicationService potionEffects,
		TrapService trapService,
		ITrapDefinitionRepository trapDefinitions,
		ITreasureDefinitionRepository treasureDefinitions,
		IconResolver icons)
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
		Combat = combat;
		CharacterCreation = characterCreation;
		CharacterClasses = characterClasses;
		CharacterRaces = characterRaces;
		CharacterBackgrounds = characterBackgrounds;
		AbilityDefinitions = abilityDefinitions;
		DamageTypeDefinitions = damageTypeDefinitions;
		ItemDefinitions = itemDefinitions;
		PotionEffects = potionEffects;
		TrapService = trapService;
		TrapDefinitions = trapDefinitions;
		TreasureDefinitions = treasureDefinitions;
		Icons = icons;
	}
}
