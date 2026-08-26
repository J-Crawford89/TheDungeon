#nullable enable
using System.Threading.Tasks;

public interface ICombatService
{
	Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel);

	bool CanAcceptPlayerAction(GameSessionState session);

	bool CanExecuteCombatAbility(GameSessionState session, string abilityId);

	Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice);

	Task ExecutePlayerFleeAsync(GameSessionState session);

	Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload);

	Task ExecutePlayerUseHealthPotionAsync(GameSessionState session);

	Task ExecutePlayerDefendAsync(GameSessionState session);

	Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload);
}
