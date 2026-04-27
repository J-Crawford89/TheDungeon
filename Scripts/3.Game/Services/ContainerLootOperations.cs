#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Moves loot from an in-room <see cref="ContainerFeature"/> into the player inventory with narrative feedback.</summary>
public static class ContainerLootOperations
{
	public sealed class TransferResult
	{
		public int StacksGranted { get; init; }
		public int StacksSkippedMissingDefinition { get; init; }
		public bool RemovedContainerFromRoom { get; init; }
	}

	/// <summary>Takes every valid stack from <paramref name="container"/>; rows with unknown item ids remain in the container.</summary>
	public static TransferResult TransferAllContents(
		GameSessionState session,
		DungeonRoom room,
		ContainerFeature container,
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		string containerKindLabel)
	{
		var distinctKinds = container.Contents.Count(static c =>
			!string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
		session.AppendGameLog(narrative.ForContainerLootTransferStart(containerKindLabel, distinctKinds));

		var granted = 0;
		var skipped = 0;
		var remaining = new List<LootableItemDefinition>();

		foreach (var row in container.Contents.ToList())
		{
			var id = row.ItemDefinitionId.Trim();
			if (id.Length == 0 || row.Quantity <= 0)
				continue;

			var def = items.TryGetById(id);
			if (def == null)
			{
				session.AppendGameLog(narrative.ForLootDefinitionMissing(id));
				remaining.Add(row);
				skipped++;
				continue;
			}

			session.Player.InventoryState.AddOrStack(def, row.Quantity);
			session.AppendGameLog(narrative.ForLootTakenFromContainer(containerKindLabel, def.Name, row.Quantity));
			granted++;
		}

		container.Contents.Clear();
		container.Contents.AddRange(remaining);

		proficiency.Recompute(session.Player);

		var removed = false;
		if (container.RemoveFeatureWhenEmpty && container.Contents.Count == 0)
		{
			room.Features.Remove(container);
			removed = true;
		}

		return new TransferResult
		{
			StacksGranted = granted,
			StacksSkippedMissingDefinition = skipped,
			RemovedContainerFromRoom = removed,
		};
	}
}
