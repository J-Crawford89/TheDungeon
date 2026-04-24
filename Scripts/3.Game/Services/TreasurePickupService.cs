#nullable enable
using System.Collections.Generic;
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

	/// <summary>Whether this instance can be taken via room Take actions (revealed; future: not locked).</summary>
	public static bool IsInstanceEligibleForRoomTake(TreasureInstance instance)
	{
		if (!instance.IsRevealed)
			return false;
		return true;
	}

	public static bool HasTakeableLootInCurrentRoom(GameSessionState session)
	{
		if (session.Dungeon.CurrentRoom is not { } room)
			return false;
		return room.Features.OfType<TreasureFeature>().Any(tf =>
			tf.TreasureItems.Any(IsInstanceEligibleForRoomTake));
	}

	/// <summary>Removes every eligible revealed treasure instance from every <see cref="TreasureFeature"/> in the current room.</summary>
	public TakeTreasureOutcome TakeAllEligibleFromCurrentRoom(GameSessionState session)
	{
		if (session.Dungeon.CurrentFloor == null)
			return TakeTreasureOutcome.NoCurrentFloor;
		if (session.Dungeon.CurrentRoom is not { } room)
			return TakeTreasureOutcome.NoCurrentRoom;

		var toRemove = new List<(TreasureFeature Feature, TreasureInstance Instance)>();
		foreach (var tf in room.Features.OfType<TreasureFeature>())
		{
			foreach (var inst in tf.TreasureItems.Where(IsInstanceEligibleForRoomTake))
				toRemove.Add((tf, inst));
		}

		if (toRemove.Count == 0)
		{
			session.AppendGameLog(_narrative.ForTakeNothingHere());
			return TakeTreasureOutcome.NothingToTake;
		}

		foreach (var (_, inst) in toRemove)
			ApplyInstance(session, inst);

		foreach (var (tf, inst) in toRemove)
		{
			tf.TreasureItems.Remove(inst);
			if (tf.RemoveFeatureWhenEmpty && tf.TreasureItems.Count == 0)
				room.Features.Remove(tf);
		}

		return TakeTreasureOutcome.TookItems;
	}

	public TakeTreasureOutcome TakeTreasureInstanceAtSlot(GameSessionState session, int treasureFeatureOrdinal, int itemIndexInFeature)
	{
		if (session.Dungeon.CurrentFloor == null)
			return TakeTreasureOutcome.NoCurrentFloor;
		if (session.Dungeon.CurrentRoom is not { } room)
			return TakeTreasureOutcome.NoCurrentRoom;

		if (!MainViewRoomSlots.TryGetTreasureSlot(room, treasureFeatureOrdinal, itemIndexInFeature, out var tf, out var inst) ||
		    tf == null || inst == null)
		{
			session.AppendGameLog(_narrative.ForTakeNothingHere());
			return TakeTreasureOutcome.NothingToTake;
		}

		if (!IsInstanceEligibleForRoomTake(inst))
		{
			session.AppendGameLog(_narrative.ForTakeNothingHere());
			return TakeTreasureOutcome.NothingToTake;
		}

		ApplyInstance(session, inst);
		tf.TreasureItems.Remove(inst);
		if (tf.RemoveFeatureWhenEmpty && tf.TreasureItems.Count == 0)
			room.Features.Remove(tf);

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
