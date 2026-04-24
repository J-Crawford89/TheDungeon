#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Editor/debug-only helpers to mutate the current room for faster manual testing.</summary>
public static class DebugRoomLootSpawn
{
	/// <summary>
	/// Appends one <see cref="TrapFeature"/> with two revealed snare instances and two <see cref="TreasureFeature"/> piles
	/// (each one revealed health potion) to the player's current room.
	/// </summary>
	/// <returns>True if anything was added.</returns>
	public static bool TrySpawnSnaresAndPotions(
		GameSessionState session,
		ITrapDefinitionRepository traps,
		ITreasureDefinitionRepository treasures)
	{
		if (session.Phase != GamePlayPhase.InProgress)
			return false;
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;

		var snare = traps.All.FirstOrDefault(d => d.Id == TrapIds.Snare);
		var potion = treasures.All.FirstOrDefault(d => d.Id == TreasureIds.HealthPotion);
		if (snare == null || potion == null)
			return false;

		if (!TryAddCopper(treasures, room)) return false;

		for (var i = 0; i < 2; i++)
		{
			room.Features.Add(new TrapFeature
			{
				Traps = new List<TrapInstance>
				{
					new() { Definition = snare, CurrentHp = 1, IsRevealed = true },
				},
			});
		}

		for (var i = 0; i < 2; i++)
		{
			room.Features.Add(new TreasureFeature
			{
				RemoveFeatureWhenEmpty = true,
				TreasureItems = new List<TreasureInstance>
				{
					new() { Definition = potion, IsRevealed = true },
				},
			});
		}

		return true;
	}

	private static bool TryAddCopper(ITreasureDefinitionRepository treasures, DungeonRoom room)
	{
		var copper = treasures.All.FirstOrDefault(d => d.Id == TreasureIds.CopperCoins);
		if (copper == null)
			return false;

		room.Features.Add(new TreasureFeature
		{
			RemoveFeatureWhenEmpty = true,
			TreasureItems = new List<TreasureInstance>
			{
				new() { Definition = copper, IsRevealed = true },
			},
		});

		return true;
    }
}
