using Xunit;

public sealed class DefendCombatAbilityHandlerTests
{
	private static GameSessionState SessionWithCombatAndDefendAbility()
	{
		var session = new GameSessionState
		{
			Combat = new CombatState(),
		};
		session.Player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });
		return session;
	}

	[Fact]
	public void IsVisible_TracksWhetherDefendIsGranted()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = new GameSessionState { Combat = new CombatState() };

		Assert.False(handler.IsVisible(session));

		session.Player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });

		Assert.True(handler.IsVisible(session));
	}

	[Fact]
	public void CanExecute_False_WhenNotAwaitingPlayerAction()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => false);
		var session = SessionWithCombatAndDefendAbility();

		Assert.False(handler.CanExecute(session));
	}

	[Fact]
	public void CanExecute_False_WhenAbilityNotGranted()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = new GameSessionState { Combat = new CombatState() };

		Assert.False(handler.CanExecute(session));
	}

	[Fact]
	public void CanExecute_False_WhenOnCooldown()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = SessionWithCombatAndDefendAbility();
		session.Combat!.AbilityCooldowns.Start(AbilityIds.Defend, 1);

		Assert.False(handler.CanExecute(session));
	}

	[Fact]
	public void CanExecute_False_WhenDefendStanceAlreadyActive()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = SessionWithCombatAndDefendAbility();
		session.Combat!.ActiveCombatEffects.Add(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);

		Assert.False(handler.CanExecute(session));
	}

	[Fact]
	public void Execute_AddsEffect_StartsCooldown_AndInvokesAdvance()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = SessionWithCombatAndDefendAbility();
		var advanceCalls = 0;

		handler.Execute(session, () => advanceCalls++);

		Assert.Contains(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage, session.Combat!.ActiveCombatEffects);
		Assert.Equal(2, session.Combat.AbilityCooldowns.GetRemaining(AbilityIds.Defend));
		Assert.Equal(1, advanceCalls);
		Assert.NotEmpty(session.LogEntries);
	}

	[Fact]
	public void ReportCannotExecute_WhenAlreadyDefending_UsesSpecificNarrative()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = SessionWithCombatAndDefendAbility();
		session.Combat!.ActiveCombatEffects.Add(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);

		handler.ReportCannotExecute(session);

		Assert.Contains(session.LogEntries, entry =>
			entry.Text.Contains("already defending", System.StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ReportCannotExecute_WhenOnCooldown_UsesSpecificNarrative()
	{
		var handler = new DefendCombatAbilityHandler(new NarrativeService(), _ => true);
		var session = SessionWithCombatAndDefendAbility();
		session.Combat!.AbilityCooldowns.Start(AbilityIds.Defend, 1);

		handler.ReportCannotExecute(session);

		Assert.Contains(session.LogEntries, entry =>
			entry.Text.Contains("cannot defend yet", System.StringComparison.OrdinalIgnoreCase));
	}
}
