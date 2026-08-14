#nullable enable
using System.Threading.Tasks;

public interface ICombatService
{
	bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel);

	Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel) =>
		Task.FromResult(TryBeginCombatIfHostile(session, previousCoord, floorLevel));

	bool CanAcceptPlayerAction(GameSessionState session);

	bool CanExecuteCombatAbility(GameSessionState session, string abilityId);

	void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice);

	Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice)
	{
		ExecutePlayerAttack(session, livingMonsterOrdinal, attackChoice);
		return Task.CompletedTask;
	}

	void ExecutePlayerFlee(GameSessionState session);

	Task ExecutePlayerFleeAsync(GameSessionState session)
	{
		ExecutePlayerFlee(session);
		return Task.CompletedTask;
	}

	void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload);

	Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload)
	{
		ExecutePlayerTakeTreasure(session, payload);
		return Task.CompletedTask;
	}

	void ExecutePlayerUseHealthPotion(GameSessionState session);

	Task ExecutePlayerUseHealthPotionAsync(GameSessionState session)
	{
		ExecutePlayerUseHealthPotion(session);
		return Task.CompletedTask;
	}

	void ExecutePlayerDefend(GameSessionState session);

	Task ExecutePlayerDefendAsync(GameSessionState session)
	{
		ExecutePlayerDefend(session);
		return Task.CompletedTask;
	}

	void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload);

	Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload)
	{
		ExecutePlayerDisarmTrap(session, payload);
		return Task.CompletedTask;
	}
}
