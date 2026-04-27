#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Phase 3 UI-agnostic container loot API (panel snapshot, loot all, loot selected).</summary>
public sealed class ContainerLootInteractionService
{
	private readonly IItemDefinitionRepository _items;
	private readonly NarrativeService _narrative;
	private readonly PlayerProficiencyAggregationService _proficiency;
	private readonly int _maxUnequippedBackpackRows;

	public ContainerLootInteractionService(
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		int maxUnequippedBackpackRows = 16)
	{
		_items = items;
		_narrative = narrative;
		_proficiency = proficiency;
		_maxUnequippedBackpackRows = maxUnequippedBackpackRows;
	}

	public ContainerLootPanelResult TryBuildPanel(GameSessionState session, int containerOrdinal)
	{
		if (!TryResolveRoom(session, out var room, out var err))
			return ContainerLootPanelResult.Fail(err);

		var resolvedRoom = room!;
		if (!RoomContainerLocator.TryGetNthContainer(resolvedRoom, containerOrdinal, out var container) ||
		    container == null)
			return ContainerLootPanelResult.Fail(ContainerLootErrorCode.ContainerOrdinalOutOfRange);

		var (kind, label) = Describe(container);
		var rows = new List<ContainerLootStackRowDto>();
		for (var i = 0; i < container.Contents.Count; i++)
		{
			var row = container.Contents[i];
			var id = row.ItemDefinitionId.Trim();
			if (row.Quantity <= 0 || string.IsNullOrWhiteSpace(id))
				continue;

			var def = _items.TryGetById(id);
			var display = def?.Name ?? "?";
			rows.Add(new ContainerLootStackRowDto
			{
				RowIndex = i,
				ItemDefinitionId = id,
				DisplayName = display,
				Quantity = row.Quantity,
			});
		}

		var additions = container.Contents.Where(static c =>
			!string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0).ToList();
		var crowded = ContainerLootCapacityPeek.LikelyCrowdedAfterTake(
			session.Player.InventoryState,
			additions,
			_items,
			_maxUnequippedBackpackRows);

		return ContainerLootPanelResult.Ok(new ContainerLootPanelDto
		{
			Kind = kind,
			ContainerKindLabel = label,
			Rows = rows,
			LikelyCrowdedAfterTakeAll = crowded,
		});
	}

	public ContainerLootTransferResult TryLootAll(GameSessionState session, int containerOrdinal)
	{
		if (!TryResolveRoom(session, out var room, out var err))
			return ContainerLootTransferResult.Fail(err);

		var resolvedRoom = room!;
		if (!RoomContainerLocator.TryGetNthContainer(resolvedRoom, containerOrdinal, out var container) ||
		    container == null)
			return ContainerLootTransferResult.Fail(ContainerLootErrorCode.ContainerOrdinalOutOfRange);

		var (_, label) = Describe(container);
		var tr = ContainerLootOperations.TransferAllContents(
			session,
			resolvedRoom,
			container,
			_items,
			_narrative,
			_proficiency,
			label);

		return ContainerLootTransferResult.Ok(
			tr.StacksGranted,
			tr.StacksSkippedMissingDefinition,
			tr.RemovedContainerFromRoom);
	}

	public ContainerLootTransferResult TryLootSelected(
		GameSessionState session,
		int containerOrdinal,
		IReadOnlyList<int> rowIndices)
	{
		if (rowIndices == null || rowIndices.Count == 0)
			return ContainerLootTransferResult.Fail(ContainerLootErrorCode.EmptyRowSelection);

		if (!TryResolveRoom(session, out var room, out var err))
			return ContainerLootTransferResult.Fail(err);

		var resolvedRoom = room!;
		if (!RoomContainerLocator.TryGetNthContainer(resolvedRoom, containerOrdinal, out var container) ||
		    container == null)
			return ContainerLootTransferResult.Fail(ContainerLootErrorCode.ContainerOrdinalOutOfRange);

		var distinct = rowIndices.Distinct().ToList();
		foreach (var idx in distinct)
		{
			if (idx < 0 || idx >= container.Contents.Count)
				return ContainerLootTransferResult.Fail(ContainerLootErrorCode.InvalidRowSelection);
		}

		var (_, label) = Describe(container);
		var tr = ContainerLootOperations.TransferSelectedContents(
			session,
			resolvedRoom,
			container,
			distinct,
			_items,
			_narrative,
			_proficiency,
			label);

		return ContainerLootTransferResult.Ok(
			tr.StacksGranted,
			tr.StacksSkippedMissingDefinition,
			tr.RemovedContainerFromRoom);
	}

	private static bool TryResolveRoom(GameSessionState session, out DungeonRoom? room, out ContainerLootErrorCode error)
	{
		room = null;
		error = ContainerLootErrorCode.None;
		if (session.Dungeon.CurrentFloor == null)
		{
			error = ContainerLootErrorCode.NoCurrentFloor;
			return false;
		}

		if (session.Dungeon.CurrentRoom is not { } r)
		{
			error = ContainerLootErrorCode.NoCurrentRoom;
			return false;
		}

		room = r;
		return true;
	}

	private static (ContainerLootKind Kind, string Label) Describe(ContainerFeature feature) =>
		feature switch
		{
			SalvageFeature => (ContainerLootKind.Salvage, "salvage pile"),
			CorpseFeature => (ContainerLootKind.Corpse, "remains"),
			ChestFeature => (ContainerLootKind.Chest, "chest"),
			_ => (ContainerLootKind.Other, "container"),
		};
}
