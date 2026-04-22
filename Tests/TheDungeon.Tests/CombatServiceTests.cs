using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class CombatServiceTests
{
	private sealed class EmptyItemDefinitionRepository : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	private static CombatService CreateCombatService()
	{
		var random = new Random(42);
		var dice = new DiceRollService(random);
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		var items = new EmptyItemDefinitionRepository();
		var treasure = new TreasurePickupService(narrative, items);
		var potionFx = new PotionEffectApplicationService(dice, narrative, items);
		var traps = new TrapService(resolution, narrative, vitals, items);
		return new CombatService(dice, resolution, narrative, vitals, downed, treasure, potionFx, traps);
	}

	[Fact]
	public void IsAwaitingPlayerAction_WhenPhaseGameOver_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		session.Phase = GamePlayPhase.GameOver;
		Assert.False(combat.IsAwaitingPlayerAction(session));
	}

	[Fact]
	public void IsAwaitingPlayerAction_WhenNotInCombat_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		Assert.False(combat.IsAwaitingPlayerAction(session));
	}

	[Fact]
	public void CombatService_ImplementsICombatService()
	{
		var combat = CreateCombatService();
		Assert.IsAssignableFrom<ICombatService>(combat);
	}
}
