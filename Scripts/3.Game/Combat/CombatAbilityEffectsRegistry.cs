#nullable enable
using System;
using System.Collections.Generic;

public sealed class CombatAbilityEffectsRegistry
{
	private readonly Dictionary<string, IPlayerCombatAbilityHandler> _handlers = new(StringComparer.Ordinal);

	public void Register(IPlayerCombatAbilityHandler handler) =>
		_handlers[handler.AbilityId] = handler;

	public bool CanExecute(string abilityId, GameSessionState session)
	{
		if (!_handlers.TryGetValue(abilityId, out var handler))
			return false;
		return handler.CanExecute(session);
	}

	public bool TryExecute(string abilityId, GameSessionState session, Action advanceTurnAndProcessMonsterPhases)
	{
		if (!_handlers.TryGetValue(abilityId, out var handler))
			return false;
		if (!handler.CanExecute(session))
			return false;
		handler.Execute(session, advanceTurnAndProcessMonsterPhases);
		return true;
	}
}
