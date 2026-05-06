#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Phase 3 UI-agnostic container loot API (panel snapshot, loot all, loot selected).</summary>
public sealed class ContainerLootInteractionService
{
	private readonly IItemDefinitionRepository _items;
	private readonly IMonsterDefinitionRepository? _monsters;
	private readonly ITrapDefinitionRepository? _traps;
	private readonly NarrativeService _narrative;
	private readonly PlayerProficiencyAggregationService _proficiency;
	private readonly ResolutionService _resolution;
	private readonly int _maxUnequippedBackpackRows;

	public ContainerLootInteractionService(
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		ResolutionService resolution,
		int maxUnequippedBackpackRows = 16,
		IMonsterDefinitionRepository? monsters = null,
		ITrapDefinitionRepository? traps = null)
	{
		_items = items;
		_monsters = monsters;
		_traps = traps;
		_narrative = narrative;
		_proficiency = proficiency;
		_resolution = resolution;
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
		var (panelTitle, panelSubtitle) = BuildPanelHeadings(container, kind);
		var rows = new List<ContainerLootStackRowDto>();
		for (var i = 0; i < container.Contents.Count; i++)
		{
			var row = container.Contents[i];
			var id = row.ItemDefinitionId.Trim();
			if (row.Quantity <= 0 || string.IsNullOrWhiteSpace(id))
				continue;

			var def = _items.TryGetById(id);
			var display = def?.Name ?? "?";
			int? harvestDc = null;
			AbilityScore? harvestAbility = null;
			if (row.Harvest is { HarvestDc: > 0 } h)
			{
				harvestDc = h.HarvestDc;
				harvestAbility = h.HarvestAbility;
			}

			rows.Add(new ContainerLootStackRowDto
			{
				RowIndex = i,
				ItemDefinitionId = id,
				DisplayName = display,
				Quantity = row.Quantity,
				HarvestDc = harvestDc,
				HarvestAbility = harvestAbility,
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
			PanelTitle = panelTitle,
			PanelSubtitle = panelSubtitle,
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
			_resolution,
			label);

		return ContainerLootTransferResult.Ok(
			tr.StacksGranted,
			tr.StacksSkippedMissingDefinition,
			tr.RemovedContainerFromRoom);
	}

	public ContainerLootTransferResult TryLootSelected(
		GameSessionState session,
		int containerOrdinal,
		IReadOnlyList<int> contentIndices)
	{
		if (contentIndices == null || contentIndices.Count == 0)
			return ContainerLootTransferResult.Fail(ContainerLootErrorCode.EmptyRowSelection);

		if (!TryResolveRoom(session, out var room, out var err))
			return ContainerLootTransferResult.Fail(err);

		var resolvedRoom = room!;
		if (!RoomContainerLocator.TryGetNthContainer(resolvedRoom, containerOrdinal, out var container) ||
		    container == null)
			return ContainerLootTransferResult.Fail(ContainerLootErrorCode.ContainerOrdinalOutOfRange);

		var distinct = contentIndices.Distinct().ToList();
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
			_resolution,
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

	private (string PanelTitle, string PanelSubtitle) BuildPanelHeadings(ContainerFeature container, ContainerLootKind kind)
	{
		var title = kind switch
		{
			ContainerLootKind.Chest => "Chest",
			ContainerLootKind.Corpse => "Corpse",
			ContainerLootKind.Salvage => "Salvage",
			_ => "Container",
		};

		var subtitle = string.Empty;
		switch (container)
		{
			case ChestFeature:
				break;
			case CorpseFeature corpse:
				subtitle = ResolveMonsterDisplayName(corpse.SourceMonsterDefinitionId);
				break;
			case SalvageFeature salvage:
				subtitle = ResolveTrapDisplayName(salvage.SourceTrapDefinitionId);
				break;
		}

		return (title, subtitle);
	}

	private string ResolveMonsterDisplayName(string monsterDefinitionId)
	{
		var id = monsterDefinitionId.Trim();
		if (string.IsNullOrEmpty(id) || _monsters == null)
			return string.Empty;
		foreach (var m in _monsters.All)
		{
			if (string.Equals(m.Id, id, StringComparison.Ordinal))
				return m.Name ?? string.Empty;
		}

		return string.Empty;
	}

	private string ResolveTrapDisplayName(string trapDefinitionId)
	{
		var id = trapDefinitionId.Trim();
		if (string.IsNullOrEmpty(id) || _traps == null)
			return string.Empty;
		foreach (var t in _traps.All)
		{
			if (string.Equals(t.Id, id, StringComparison.Ordinal))
				return t.Name ?? string.Empty;
		}

		return string.Empty;
	}
}
