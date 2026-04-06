using System.Collections.Generic;
using System.Linq;

public sealed class CombatAbilityCooldowns
{
	private readonly Dictionary<string, int> _remainingPlayerTurnRounds = new();

	public void Start(string abilityId, int playerTurnRounds)
	{
		if (string.IsNullOrWhiteSpace(abilityId) || playerTurnRounds <= 0)
			return;
		_remainingPlayerTurnRounds[abilityId.Trim()] = playerTurnRounds;
	}

	public int GetRemaining(string abilityId)
	{
		if (string.IsNullOrWhiteSpace(abilityId))
			return 0;
		return _remainingPlayerTurnRounds.TryGetValue(abilityId.Trim(), out var r) ? r : 0;
	}

	public bool IsOnCooldown(string abilityId) => GetRemaining(abilityId) > 0;

	public void OnPlayerTurnStarted()
	{
		var keys = _remainingPlayerTurnRounds.Keys.ToList();
		foreach (var k in keys)
		{
			var next = _remainingPlayerTurnRounds[k] - 1;
			if (next <= 0)
				_remainingPlayerTurnRounds.Remove(k);
			else
				_remainingPlayerTurnRounds[k] = next;
		}
	}

	public void Clear() => _remainingPlayerTurnRounds.Clear();
}
