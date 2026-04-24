#nullable enable

public interface ICombatService
{
	bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel);

	bool IsAwaitingPlayerAction(GameSessionState session);

	void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal);

	void ExecutePlayerFlee(GameSessionState session);

	void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload);

	void ExecutePlayerUseHealthPotion(GameSessionState session);

	void ExecutePlayerDefend(GameSessionState session);

	void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload);
}
