#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Trap disarm checks and outcome application (rolls in <see cref="ResolutionService"/>).</summary>
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
			trapFeature.Traps.FirstOrDefault(t => t.CurrentHp > 0) ??
			trapFeature.Traps.FirstOrDefault();

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
		var damageDealt = 0;
		var dm = trapDef.Damage;
		if (dm > 0)
		{
			var dmg = _vitals.ApplyDamage(session.Player, dm);
			damageDealt = dmg.HpBefore - dmg.HpAfterClamped;
			session.AppendGameLog(_narrative.ForTrapDisarmFailDamage(trapDef.Name, dm, session.Player.CurrentHp));
		}
		else
			session.AppendGameLog(_narrative.ForTrapDisarmFailSafe(trapDef.Name));

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
