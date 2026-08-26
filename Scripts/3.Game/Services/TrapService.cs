#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Trap disarm checks, tripped-trap resolution, and room-exit tripwires (rolls in <see cref="ResolutionService"/>).</summary>
public sealed class TrapService
{
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly IItemDefinitionRepository _items;
	private readonly PlayerExperienceService? _experience;

	public TrapService(
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		IItemDefinitionRepository items,
		PlayerExperienceService? experience = null)
	{
		_resolution = resolution;
		_narrative = narrative;
		_vitals = vitals;
		_items = items;
		_experience = experience;
	}

	public static bool CurrentRoomHasTrap(GameSessionState session)
	{
		if (session.Dungeon.CurrentFloor == null || session.Dungeon.CurrentRoom is not { } room)
			return false;
		return room.Features.OfType<TrapFeature>().Any(tf => tf.Traps.Any(t => t.IsRevealed));
	}

	/// <summary>When the player leaves a room, every armed trap may fire unless this is a no-trip backtrack move.</summary>
	public void ProcessTrapsOnRoomExit(GameSessionState session, DungeonRoom room, bool exemptFromTripBecauseBacktracking)
	{
		if (exemptFromTripBecauseBacktracking)
			return;

		foreach (var trapFeature in room.Features.OfType<TrapFeature>().ToList())
		{
			foreach (var trapInstance in trapFeature.Traps.Where(t => t.CurrentHp > 0).ToList())
			{
				var wasHidden = !trapInstance.IsRevealed;
				trapInstance.IsRevealed = true;
				var def = trapInstance.Definition;
				TripTrap(session, def, TrapTripCause.LeftRoom, wasHidden);
				if (def.IsRemovedAfterTripped)
					RemoveTrapInstanceFromRoomAfterTrip(room, trapFeature, trapInstance);
			}
		}
	}

	/// <summary>Resolves a sprung trap: lead-in from <see cref="TrapTripCause"/>, then damage and effect text from the definition.</summary>
	public int TripTrap(GameSessionState session, TrapDefinition def, TrapTripCause cause, bool wasHiddenBeforeTrip)
	{
		session.AppendGameLog(_narrative.ForTrapTripLeadIn(cause, def.Name, wasHiddenBeforeTrip));
		return ApplyTrippedTrapEffects(session, def);
	}

	/// <summary>Applies damage and outcome log lines for a tripped trap (no lead-in).</summary>
	public int ApplyTrippedTrapEffects(GameSessionState session, TrapDefinition def)
	{
		var damageDealt = 0;
		var dm = def.Damage;
		if (dm > 0)
		{
			var dmg = _vitals.ApplyDamage(session.Player, dm);
			damageDealt = dmg.HpBefore - dmg.HpAfterClamped;
			session.AppendGameLog(_narrative.ForTrapTripOutcomeDamage(def.Name, dm, session.Player.CurrentHp));
		}
		else
			session.AppendGameLog(_narrative.ForTrapTripOutcomeNoDamage(def.Name));

		if (!string.IsNullOrWhiteSpace(def.Effect))
			session.AppendGameLog(_narrative.ForTrapTripEffectLine(def.Name, def.Effect.Trim()));

		return damageDealt;
	}

	public async Task<TrapDisarmResult> TryDisarmAsync(GameSessionState session)
	{
		if (session.Dungeon.CurrentFloor == null)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoCurrentFloor };

		if (session.Dungeon.CurrentRoom is not { } room)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoCurrentRoom };

		var trapFeature = RoomFeatureHelper.GetFirstTrapFeatureOrdered(room);
		if (trapFeature == null || trapFeature.Traps.Count == 0)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoTrapPresent };

		var trapInstance =
			trapFeature.Traps.FirstOrDefault(t => t.IsRevealed && t.CurrentHp > 0) ??
			trapFeature.Traps.FirstOrDefault(t => t.IsRevealed);

		if (trapInstance == null)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoTrapPresent };

		return await TryDisarmAsync(session, room, trapFeature, trapInstance);
	}

	public async Task<TrapDisarmResult> TryDisarmAtSlotAsync(
		GameSessionState session,
		int trapFeatureOrdinal,
		int trapIndexInFeature)
	{
		if (session.Dungeon.CurrentFloor == null)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoCurrentFloor };

		if (session.Dungeon.CurrentRoom is not { } room)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoCurrentRoom };

		if (!MainViewRoomSlots.TryGetTrapSlot(room, trapFeatureOrdinal, trapIndexInFeature, out var trapFeature, out var trapInstance) ||
		    trapFeature == null || trapInstance == null)
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoTrapPresent };

		return await TryDisarmAsync(session, room, trapFeature, trapInstance);
	}

	public async Task<TrapDisarmResult> TryDisarmAsync(
		GameSessionState session,
		DungeonRoom room,
		TrapFeature trapFeature,
		TrapInstance trapInstance)
	{
		if (!trapFeature.Traps.Contains(trapInstance))
			return new TrapDisarmResult { ResultCode = TrapDisarmResultCode.NoTrapPresent };

		var trapDef = trapInstance.Definition;
		var dex = session.Player.AbilityScores.Dexterity;
		var req = new DiceRollRequest
		{
			DiceRollLabel = $"Disarm trap ({trapDef.Name})",
			TargetNumber = trapDef.DisarmDc,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions = new List<DiceExpression>
			{
				new() { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true },
			},
			ModifiersWithSources = new List<ModifierWithSource>
			{
				new() { Modifier = dex, Source = "Dexterity" },
			},
		};

		var resolution = await _resolution.RollAgainstTargetAsync(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForTrapDisarmRoll(trapDef.Name, resolution.Roll.Total, trapDef.DisarmDc, resolution.Roll.DetailText),
		});

		var success = resolution.Outcome is ResolutionOutcome.Success or ResolutionOutcome.CriticalSuccess;
		var result = success
			? ApplySuccess(session, room, trapFeature, resolution, trapDef)
			: ApplyFail(session, room, trapFeature, trapInstance, trapDef, resolution);
		await _resolution.NotifyResolvedRollAsync();
		return result;
	}

	private TrapDisarmResult ApplyFail(
		GameSessionState session,
		DungeonRoom room,
		TrapFeature trapFeature,
		TrapInstance trapInstance,
		TrapDefinition trapDef,
		ResolutionResult resolution)
	{
		var damageDealt = TripTrap(session, trapDef, TrapTripCause.DisarmFailed, wasHiddenBeforeTrip: false);

		var trapFeatureRemoved = false;
		if (trapDef.IsRemovedAfterTripped)
		{
			trapFeatureRemoved = trapFeature.Traps.Count == 1;
			RemoveTrapInstanceFromRoomAfterTrip(room, trapFeature, trapInstance);
		}

		return new TrapDisarmResult
		{
			ResultCode = TrapDisarmResultCode.DisarmCheckResolved,
			ResolvedCheck = resolution,
			TrapFeatureRemoved = trapFeatureRemoved,
			DamageDealtToPlayer = damageDealt,
		};
	}

	private TrapDisarmResult ApplySuccess(
		GameSessionState session,
		DungeonRoom room,
		TrapFeature trapFeature,
		ResolutionResult resolution,
		TrapDefinition trapDef)
	{
		session.AppendGameLog(_narrative.ForTrapDisarmSuccess(trapDef.Name));
		_experience?.GrantExperience(session, trapDef.ExperienceReward);

		var salvageRows = new List<LootableItemDefinition>();
		if (trapDef.DisarmLoot is { Count: > 0 })
		{
			foreach (var row in trapDef.DisarmLoot)
			{
				var id = row.ItemDefinitionId?.Trim() ?? "";
				var qty = row.Quantity < 0 ? 0 : row.Quantity;
				if (qty <= 0 || string.IsNullOrWhiteSpace(id))
					continue;

				if (_items.TryGetById(id) == null)
				{
					session.AppendGameLog(_narrative.ForTrapDisarmGrantItemMissing(id));
					// Use trace (not Godot GD) so unit tests run without the Godot runtime.
					Trace.TraceWarning(
						"TrapService: trap '{0}' ({1}) disarm loot references unknown item id '{2}'.",
						trapDef.Id,
						trapDef.Name,
						id);
					continue;
				}

				salvageRows.Add(new LootableItemDefinition { ItemDefinitionId = id, Quantity = qty });
			}
		}

		room.Features.Remove(trapFeature);

		if (salvageRows.Count > 0)
		{
			var salvage = new SalvageFeature { SourceTrapDefinitionId = trapDef.Id ?? string.Empty };
			salvage.Contents.AddRange(salvageRows);
			room.Features.Add(salvage);
		}

		return new TrapDisarmResult
		{
			ResultCode = TrapDisarmResultCode.DisarmCheckResolved,
			ResolvedCheck = resolution,
			TrapFeatureRemoved = true,
			DamageDealtToPlayer = 0,
			GrantedInventoryItemDefinitionIds = Array.Empty<string>(),
		};
	}

	private static void RemoveTrapInstanceFromRoomAfterTrip(DungeonRoom room, TrapFeature trapFeature, TrapInstance instance)
	{
		trapFeature.Traps.Remove(instance);
		if (trapFeature.Traps.Count == 0)
			room.Features.Remove(trapFeature);
	}

}
