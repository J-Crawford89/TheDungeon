public interface IPlayerDownedOutcomeHandler
{
	int Priority { get; }

	bool TryResolve(GameSessionState session, PlayerDownedContext context);
}
