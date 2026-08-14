#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Moves loot from an in-room <see cref="ContainerFeature"/> into the player inventory with narrative feedback.</summary>
public static class ContainerLootOperations
{
	public sealed class TransferResult
	{
		public int StacksGranted { get; init; }
		public int StacksSkippedMissingDefinition { get; init; }
		public bool RemovedContainerFromRoom { get; init; }
	}

	/// <summary>Takes every valid stack from <paramref name="container"/>; stacks with unknown item ids remain in the container.</summary>
	public static TransferResult TransferAllContents(
		GameSessionState session,
		DungeonRoom room,
		ContainerFeature container,
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		ResolutionService resolution,
		string containerKindLabel) =>
		TransferAllContentsAsync(session, room, container, items, narrative, proficiency, resolution, containerKindLabel)
			.GetAwaiter().GetResult();

	public static async Task<TransferResult> TransferAllContentsAsync(
		GameSessionState session,
		DungeonRoom room,
		ContainerFeature container,
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		ResolutionService resolution,
		string containerKindLabel)
	{
		var distinctKinds = container.Contents.Count(static c =>
			!string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
		session.AppendGameLog(narrative.ForContainerLootTransferStart(containerKindLabel, distinctKinds));
		var harvestProfile = ResolveHarvestPresentationProfile(container.Contents, items);

		var granted = 0;
		var skipped = 0;
		var remaining = new List<LootableItemDefinition>();

		foreach (var stack in container.Contents.ToList())
		{
			var id = stack.ItemDefinitionId.Trim();
			if (id.Length == 0 || stack.Quantity <= 0)
				continue;

			var def = items.TryGetById(id);
			if (def == null)
			{
				session.AppendGameLog(narrative.ForLootDefinitionMissing(id));
				remaining.Add(stack);
				skipped++;
				continue;
			}

			if (await TryGrantStackAsync(session, stack, def, containerKindLabel, narrative, resolution, harvestProfile))
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

	/// <summary>Takes selected stacks by content index into <paramref name="container.Contents"/>; unknown definition ids are returned to the container.</summary>
	public static TransferResult TransferSelectedContents(
		GameSessionState session,
		DungeonRoom room,
		ContainerFeature container,
		IReadOnlyList<int> contentIndices,
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		ResolutionService resolution,
		string containerKindLabel) =>
		TransferSelectedContentsAsync(session, room, container, contentIndices, items, narrative, proficiency, resolution, containerKindLabel)
			.GetAwaiter().GetResult();

	public static async Task<TransferResult> TransferSelectedContentsAsync(
		GameSessionState session,
		DungeonRoom room,
		ContainerFeature container,
		IReadOnlyList<int> contentIndices,
		IItemDefinitionRepository items,
		NarrativeService narrative,
		PlayerProficiencyAggregationService proficiency,
		ResolutionService resolution,
		string containerKindLabel)
	{
		var distinctAscending = contentIndices.Distinct().OrderBy(i => i).ToList();
		foreach (var idx in distinctAscending)
		{
			if (idx < 0 || idx >= container.Contents.Count)
				throw new ArgumentOutOfRangeException(nameof(contentIndices), idx, "Content index out of range.");
		}

		var snapshots = distinctAscending.Select(idx => container.Contents[idx]).ToList();
		foreach (var idx in distinctAscending.OrderByDescending(i => i))
			container.Contents.RemoveAt(idx);

		var distinctKinds = snapshots.Count(static c =>
			!string.IsNullOrWhiteSpace(c.ItemDefinitionId) && c.Quantity > 0);
		session.AppendGameLog(narrative.ForContainerLootTransferStart(containerKindLabel, distinctKinds));
		var harvestProfile = ResolveHarvestPresentationProfile(snapshots, items);

		var granted = 0;
		var skipped = 0;

		foreach (var stack in snapshots)
		{
			var id = stack.ItemDefinitionId.Trim();
			if (id.Length == 0 || stack.Quantity <= 0)
			{
				container.Contents.Add(stack);
				continue;
			}

			var def = items.TryGetById(id);
			if (def == null)
			{
				session.AppendGameLog(narrative.ForLootDefinitionMissing(id));
				container.Contents.Add(stack);
				skipped++;
				continue;
			}

			if (await TryGrantStackAsync(session, stack, def, containerKindLabel, narrative, resolution, harvestProfile))
				granted++;
		}

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

	private static DicePresentationProfile ResolveHarvestPresentationProfile(
		IEnumerable<LootableItemDefinition> stacks,
		IItemDefinitionRepository items)
	{
		var attempts = 0;
		foreach (var stack in stacks)
		{
			if (stack.Quantity <= 0 || !RequiresHarvestRoll(stack))
				continue;
			var id = stack.ItemDefinitionId.Trim();
			if (id.Length == 0 || items.TryGetById(id) == null)
				continue;

			attempts += stack.Quantity;
			if (attempts > 1)
				return DicePresentationProfile.RapidSequence;
		}

		return DicePresentationProfile.Standard;
	}

	private static bool RequiresHarvestRoll(LootableItemDefinition stack) =>
		stack.Harvest is { HarvestDc: > 0 };

	private static bool IsHarvestSuccess(ResolutionOutcome outcome) =>
		outcome is ResolutionOutcome.Success or ResolutionOutcome.CriticalSuccess;

	private static async Task<bool> TryGrantStackAsync(
		GameSessionState session,
		LootableItemDefinition stack,
		ItemDefinition def,
		string containerKindLabel,
		NarrativeService narrative,
		ResolutionService resolution,
		DicePresentationProfile harvestProfile)
	{
		var qty = stack.Quantity;
		if (!RequiresHarvestRoll(stack))
		{
			session.Player.InventoryState.AddOrStack(def, qty);
			session.AppendGameLog(narrative.ForLootTakenFromContainer(containerKindLabel, def.Name, qty));
			return true;
		}

		var hr = stack.Harvest!;
		var successes = 0;
		for (var u = 0; u < qty; u++)
		{
			var req = BuildHarvestDiceRequest(def.Name, hr, session.Player.AbilityScores);
			var resolved = await resolution.RollAgainstTargetAsync(
				req,
				DieRollVisualKind.Player,
				harvestProfile);
			var succeeded = IsHarvestSuccess(resolved.Outcome);
			if (succeeded)
			{
				successes++;
				session.Player.InventoryState.AddOrStack(def, 1);
			}

			session.AppendLog(new LogEntry
			{
				Kind = LogEntryKind.Roll,
				Text = narrative.ForHarvestRollAttempt(def.Name, u + 1, qty, resolved.Roll.Total, hr.HarvestDc, succeeded, resolved.Roll.DetailText),
			});
			if (u == qty - 1)
			{
				var failures = qty - successes;
				session.AppendGameLog(narrative.ForHarvestStackOutcome(containerKindLabel, def.Name, successes, failures));
			}
			await resolution.NotifyResolvedRollAsync(session);
		}

		return successes > 0;
	}

	private static DiceRollRequest BuildHarvestDiceRequest(string itemDisplayName, HarvestRequirement hr, AbilityScores scores)
	{
		var mod = scores.GetScore(hr.HarvestAbility);
		var mods = new List<ModifierWithSource>();
		if (mod != 0)
			mods.Add(new ModifierWithSource { Modifier = mod, Source = hr.HarvestAbility.ToString() });

		return new DiceRollRequest
		{
			DiceRollLabel = $"Harvest ({itemDisplayName})",
			TargetNumber = hr.HarvestDc,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new()
				{
					NumberOfDice = 1,
					DieType = DieType.d20,
					InD20CheckPool = true,
				},
			},
			ModifiersWithSources = mods,
		};
	}
}
