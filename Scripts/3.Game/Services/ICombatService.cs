#nullable enable

public interface ICombatService
{
	bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel);

	bool IsAwaitingPlayerAction(GameSessionState session);

	void ExecutePlayerAttack(GameSessionState session);

	void ExecutePlayerFlee(GameSessionState session);

	void ExecutePlayerTakeTreasure(GameSessionState session);

	void ExecutePlayerUseHealthPotion(GameSessionState session);

	void ExecutePlayerDefend(GameSessionState session);
}
