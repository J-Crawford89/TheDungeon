using System.Collections.Generic;
using System.Linq;

public sealed class PlayerDownedResolutionService
{
	private readonly IReadOnlyList<IPlayerDownedOutcomeHandler> _handlers;

	public PlayerDownedResolutionService(IEnumerable<IPlayerDownedOutcomeHandler> handlers)
	{
		_handlers = handlers.OrderBy(h => h.Priority).ToList();
	}

	public void Resolve(GameSessionState session, PlayerDownedContext context)
	{
		foreach (var handler in _handlers)
		{
			if (handler.TryResolve(session, context))
				return;
		}
	}
}
