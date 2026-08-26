#nullable enable
using System;

public sealed class DefendCombatAbilityHandler : IPlayerCombatAbilityHandler
{
	private readonly NarrativeService _narrative;
	private readonly Func<GameSessionState, bool> _isAwaitingPlayerAction;

	public DefendCombatAbilityHandler(NarrativeService narrative, Func<GameSessionState, bool> isAwaitingPlayerAction)
	{
		_narrative = narrative;
		_isAwaitingPlayerAction = isAwaitingPlayerAction;
	}

	string IPlayerCombatAbilityHandler.AbilityId => AbilityIds.Defend;

	public bool IsVisible(GameSessionState session) => session.Player.HasAbility(AbilityIds.Defend);

	public bool CanExecute(GameSessionState session)
	{
		if (!_isAwaitingPlayerAction(session))
			return false;
		if (!IsVisible(session))
			return false;
		if (session.Combat is not { } c)
			return false;
		if (c.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend))
			return false;
		if (c.HasDefendStanceActive())
			return false;
		return true;
	}

	public void Execute(GameSessionState session, Action advanceTurn)
	{
		var c = session.Combat!;
		c.ActiveCombatEffects.Add(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);
		c.AbilityCooldowns.Start(AbilityIds.Defend, 2);
		session.AppendGameLog(_narrative.ForDefendStance());
		advanceTurn();
	}

	public void ReportCannotExecute(GameSessionState session)
	{
		if (session.Combat is not { } combat)
		{
			session.AppendGameLog(_narrative.ForDefendCannotUse());
			return;
		}

		if (combat.HasDefendStanceActive())
			session.AppendGameLog(_narrative.ForDefendAlreadyDefending());
		else if (combat.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend))
			session.AppendGameLog(_narrative.ForDefendOnCooldown());
		else
			session.AppendGameLog(_narrative.ForDefendCannotUse());
	}
}
