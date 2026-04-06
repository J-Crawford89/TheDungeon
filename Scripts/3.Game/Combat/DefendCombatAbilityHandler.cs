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

	public bool CanExecute(GameSessionState session)
	{
		if (!_isAwaitingPlayerAction(session))
			return false;
		if (!session.Player.HasAbility(AbilityIds.Defend))
			return false;
		if (session.Combat is not { } c)
			return false;
		if (c.AbilityCooldowns.IsOnCooldown(AbilityIds.Defend))
			return false;
		if (c.HasDefendStanceActive())
			return false;
		return true;
	}

	public void Execute(GameSessionState session, Action advanceTurnAndProcessMonsterPhases)
	{
		var c = session.Combat!;
		c.ActiveCombatEffects.Add(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);
		c.AbilityCooldowns.Start(AbilityIds.Defend, 2);
		session.AppendGameLog(_narrative.ForDefendStance());
		advanceTurnAndProcessMonsterPhases();
	}
}
