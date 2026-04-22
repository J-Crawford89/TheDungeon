#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Trap disarm checks, tripped-trap resolution, and room-exit tripwires (rolls in <see cref="ResolutionService"/>).</summary>
public sealed class TrapService
{
	private readonly ResolutionService _resolution;
	private readonly NarrativeService _narrative;
	private readonly PlayerVitalsService _vitals;
	private readonly IItemDefinitionRepository _items;

	public TrapService(
		ResolutionService resolution,
		NarrativeService narrative,
		PlayerVitalsService vitals,
		IItemDefinitionRepository items)
	{
		_resolution = resolution;
		_narrative = narrative;
		_vitals = vitals;
		_items = items;
	}

	public static bool CurrentRoomHasTrap(GameSessionState session)
	{
		if (session.Dungeon.CurrentFloor == null || session.Dungeon.CurrentRoom is not { } room)
			return false;
		return RoomFeatureHelper.GetFirstTrapFeatureOrdered(room) != null;
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

	public TrapDisarmResult TryDisarm(GameSessionState session)
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

		var resolution = _resolution.RollAgainstTarget(req);
		session.AppendLog(new LogEntry
		{
			Kind = LogEntryKind.Roll,
			Text = _narrative.ForTrapDisarmRoll(trapDef.Name, resolution.Roll.Total, trapDef.DisarmDc, resolution.Roll.DetailText),
		});

		var success = resolution.Outcome is ResolutionOutcome.Success or ResolutionOutcome.CriticalSuccess;

		if (!success)
			return ApplyFail(session, trapDef, resolution);

		return ApplySuccess(session, room, trapFeature, resolution, trapDef);
	}

	private TrapDisarmResult ApplyFail(GameSessionState session, TrapDefinition trapDef, ResolutionResult resolution)
	{
		var damageDealt = TripTrap(session, trapDef, TrapTripCause.DisarmFailed, wasHiddenBeforeTrip: false);

		return new TrapDisarmResult
		{
			ResultCode = TrapDisarmResultCode.DisarmCheckResolved,
			ResolvedCheck = resolution,
			TrapFeatureRemoved = false,
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

		var grantedIds = new List<string>();

		switch (trapDef.Id)
		{
			case TrapIds.Snare:
				AddInventoryItemById(session, InventoryIds.Rope);
				grantedIds.Add(InventoryIds.Rope);
				break;
		}

		room.Features.Remove(trapFeature);

		return new TrapDisarmResult
		{
			ResultCode = TrapDisarmResultCode.DisarmCheckResolved,
			ResolvedCheck = resolution,
			TrapFeatureRemoved = true,
			DamageDealtToPlayer = 0,
			GrantedInventoryItemDefinitionIds = grantedIds,
		};
	}

	private static void RemoveTrapInstanceFromRoomAfterTrip(DungeonRoom room, TrapFeature trapFeature, TrapInstance instance)
	{
		trapFeature.Traps.Remove(instance);
		if (trapFeature.Traps.Count == 0)
			room.Features.Remove(trapFeature);
	}

	private void AddInventoryItemById(GameSessionState session, string itemId)
	{
		var itemDef = _items.TryGetById(itemId);
		if (itemDef == null)
		{
			session.AppendGameLog(_narrative.ForTrapDisarmGrantItemMissing(itemId));
			return;
		}

		session.Player.InventoryState.AddOrStackOne(itemDef);
		session.AppendGameLog(_narrative.ForTrapDisarmGrantedItem(itemDef.Name));
	}
}
