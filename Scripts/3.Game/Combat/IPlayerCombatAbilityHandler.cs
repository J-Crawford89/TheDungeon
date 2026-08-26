#nullable enable
using System;

public interface IPlayerCombatAbilityHandler
{
	string AbilityId { get; }

	bool IsVisible(GameSessionState session);

	bool CanExecute(GameSessionState session);

	void Execute(GameSessionState session, Action advanceTurn);

	void ReportCannotExecute(GameSessionState session);
}
