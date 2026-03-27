using System.Collections.Generic;

public static class RoomFeatureFactories
{
	public static MonsterFeature CreateMonsterFeature(MonsterDefinition definition) =>
		new()
		{
			Monsters = new List<MonsterInstance>
			{
				new()
				{
					Definition = definition,
					CurrentHp = definition.MaxHp
				}
			}
		};

	public static TrapFeature CreateTrapFeature(TrapDefinition definition) =>
		new()
		{
			Traps = new List<TrapInstance>
			{
				new() { Definition = definition, CurrentHp = 1 }
			}
		};

	public static TreasureFeature CreateTreasureFeature(TreasureDefinition definition) =>
		new()
		{
			RemoveFeatureWhenEmpty = true,
			TreasureItems = new List<TreasureInstance> { new() { Definition = definition } }
		};

	public static NpcFeature CreateNpcFeature(NpcDefinition definition) =>
		new()
		{
			NPCs = new List<NpcInstance>
			{
				new() { Definition = definition, CurrentHp = 10 }
			}
		};

	public static LoreFeature CreateLoreFeature(LoreDefinition definition) =>
		new()
		{
			Lore = new List<LoreInstance>
			{
				new() { Definition = definition, CurrentHp = 0 }
			}
		};
}
