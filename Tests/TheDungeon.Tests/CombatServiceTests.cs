using System;
using Xunit;

public sealed class CombatServiceTests
{
	private static CombatService CreateCombatService()
	{
		var random = new Random(42);
		var dice = new DiceRollService(random);
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		var treasure = new TreasurePickupService(narrative);
		return new CombatService(dice, resolution, narrative, vitals, downed, treasure);
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
