#nullable enable
using System.Linq;

public sealed class TreasurePickupService
{
	private readonly NarrativeService _narrative;
	private readonly IItemDefinitionRepository _items;

	public TreasurePickupService(NarrativeService narrative, IItemDefinitionRepository items)
	{
		_narrative = narrative;
		_items = items;
	}

	public static bool HasTakeableLootInCurrentRoom(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;
		var treasureFeature = RoomFeatureHelper.GetFeature<TreasureFeature>(room);
		return treasureFeature != null && treasureFeature.TreasureItems.Any(t => t.IsRevealed);
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

		var revealed = treasureFeature.TreasureItems.Where(t => t.IsRevealed).ToList();
		if (revealed.Count == 0)
		{
			session.AppendGameLog(_narrative.ForTakeNothingHere());
			return TakeTreasureOutcome.NothingToTake;
		}

		foreach (var instance in revealed)
			ApplyInstance(session, instance);

		foreach (var instance in revealed)
			treasureFeature.TreasureItems.Remove(instance);

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
				AddInventoryItem(session, def);
				break;
		}
	}

	private void AddInventoryItem(GameSessionState session, TreasureDefinition treasureDef)
	{
		var itemId = treasureDef.InventoryItemId;
		if (string.IsNullOrWhiteSpace(itemId))
		{
			session.AppendGameLog($"You find {treasureDef.Name}, but it has no linked item id.");
			return;
		}

		var itemDef = _items.TryGetById(itemId.Trim());
		if (itemDef == null)
		{
			session.AppendGameLog($"You find {treasureDef.Name}, but no item definition exists for '{itemId.Trim()}'.");
			return;
		}

		session.Player.InventoryState.AddOrStackOne(itemDef);
		session.AppendGameLog(_narrative.ForTookItem(treasureDef.Name));
	}
}
