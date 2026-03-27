using System.Linq;

public sealed class TreasurePickupService
{
	private readonly NarrativeService _narrative;

	public TreasurePickupService(NarrativeService narrative)
	{
		_narrative = narrative;
	}

	public static bool HasTakeableLootInCurrentRoom(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;
		var treasureFeature = RoomFeatureHelper.GetFeature<TreasureFeature>(room);
		return treasureFeature != null && treasureFeature.TreasureItems.Count > 0;
	}

	public TakeTreasureOutcome TakeAllFromCurrentRoom(GameSessionState session)
	{
		if (session.Dungeon.CurrentFloor == null)
			return TakeTreasureOutcome.NoCurrentFloor;
		if (session.Dungeon.CurrentRoom is not { } room)
			return TakeTreasureOutcome.NoCurrentRoom;

		var treasureFeature = RoomFeatureHelper.GetFeature<TreasureFeature>(room);
		if (treasureFeature == null || treasureFeature.TreasureItems.Count == 0)
		{
			session.AppendGameLog(_narrative.ForTakeNothingHere());
			return TakeTreasureOutcome.NothingToTake;
		}

		foreach (var instance in treasureFeature.TreasureItems.ToList())
			ApplyInstance(session, instance);

		treasureFeature.TreasureItems.Clear();

		if (treasureFeature.RemoveFeatureWhenEmpty)
			room.Features.Remove(treasureFeature);

		return TakeTreasureOutcome.TookItems;
	}

	private void ApplyInstance(GameSessionState session, TreasureInstance instance)
	{
		var def = instance.Definition;
		switch (def.GrantKind)
		{
			case TreasureKind.Gold:
				session.Player.Gold += def.ValueInGp;
				session.AppendGameLog(_narrative.ForTookGold(def.ValueInGp, def.Name, session.Player.Gold));
				break;
			case TreasureKind.InventoryItem:
				if (def.InventoryItemId == InventoryConstants.HealthPotionItemId)
				{
					session.Player.HealthPotionCount++;
					session.AppendGameLog(_narrative.ForTookItem(def.Name));
				}

				break;
		}
	}
}
