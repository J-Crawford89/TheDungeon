#nullable enable
using System;

public interface IPlayerCombatAbilityHandler
{
	string AbilityId { get; }

	bool CanExecute(GameSessionState session);

	void Execute(GameSessionState session, Action advanceTurnAndProcessMonsterPhases);
}
