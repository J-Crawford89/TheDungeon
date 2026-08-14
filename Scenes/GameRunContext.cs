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
	public DiceRollPresenterHost DicePresenterHost { get; }
	public ResolvedRollReactionHost ResolvedRollReactionHost { get; }
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
	public GameBalanceSettingsResource? GameBalanceSettings { get; }
	public PlayerExperienceService PlayerExperienceService { get; }
	public PotionEffectApplicationService PotionEffects { get; }
	public TrapService TrapService { get; }
	public ContainerLootInteractionService ContainerLootInteraction { get; }
	public ITrapDefinitionRepository TrapDefinitions { get; }
	public ITreasureDefinitionRepository TreasureDefinitions { get; }

	public IconResolver Icons { get; }
	public PlayerProficiencyAggregationService ProficiencyAggregation { get; }

	public GameRunContext(
		GameSessionState session,
		Random random,
		NarrativeService narrativeService,
		RoomFeaturePopulationService roomFeaturePopulation,
		DungeonBootstrap dungeonBootstrap,
		ExplorationService explorationService,
		DiceRollService diceRollService,
		ResolutionService resolutionService,
		DiceRollPresenterHost dicePresenterHost,
		ResolvedRollReactionHost resolvedRollReactionHost,
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
		GameBalanceSettingsResource? gameBalanceSettings,
		PlayerExperienceService playerExperienceService,
		PotionEffectApplicationService potionEffects,
		TrapService trapService,
		ContainerLootInteractionService containerLootInteraction,
		ITrapDefinitionRepository trapDefinitions,
		ITreasureDefinitionRepository treasureDefinitions,
		IconResolver icons,
		PlayerProficiencyAggregationService proficiencyAggregation)
	{
		Session = session;
		Random = random;
		NarrativeService = narrativeService;
		RoomFeaturePopulation = roomFeaturePopulation;
		DungeonBootstrap = dungeonBootstrap;
		ExplorationService = explorationService;
		DiceRollService = diceRollService;
		ResolutionService = resolutionService;
		DicePresenterHost = dicePresenterHost;
		ResolvedRollReactionHost = resolvedRollReactionHost;
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
		GameBalanceSettings = gameBalanceSettings;
		PlayerExperienceService = playerExperienceService;
		PotionEffects = potionEffects;
		TrapService = trapService;
		ContainerLootInteraction = containerLootInteraction;
		TrapDefinitions = trapDefinitions;
		TreasureDefinitions = treasureDefinitions;
		Icons = icons;
		ProficiencyAggregation = proficiencyAggregation;
	}
}
