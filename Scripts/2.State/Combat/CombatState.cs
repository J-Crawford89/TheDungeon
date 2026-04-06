using System.Collections.Generic;

public sealed class CombatState
{
	public RoomCoord FleeReturnCoord { get; set; }
	public int FleeReturnFloorLevel { get; set; }
	public int FleeDc { get; set; } = 12;
	public List<CombatTurnSlot> TurnOrder { get; set; } = new();
	public int CurrentTurnIndex { get; set; }

	public List<ActiveCombatEffectKind> ActiveCombatEffects { get; set; } = new();
	public CombatAbilityCooldowns AbilityCooldowns { get; set; } = new();

	public bool HasDefendStanceActive() =>
		ActiveCombatEffects.Contains(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);

	public static int FirstLivingMonsterIndex(MonsterFeature feature)
	{
		for (var i = 0; i < feature.Monsters.Count; i++)
		{
			if (feature.Monsters[i].CurrentHp > 0)
				return i;
		}

		return -1;
	}
}
