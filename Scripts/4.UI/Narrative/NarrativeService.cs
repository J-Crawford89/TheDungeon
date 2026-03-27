using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class NarrativeService
{
	public string ForEnterDungeon() => "You enter the dungeon.";

	public string ForMoveForward(ExplorationServiceResult result)
	{
		if (result.Success && result.DestinationAfterMove is { } destination)
			return $"You move forward into room {destination}.";

		return result.ErrorCode switch
		{
			ExplorationErrorCode.NoCurrentFloor => "There is nowhere to go.",
			ExplorationErrorCode.NoRoomAtPlayer => "You are nowhere.",
			ExplorationErrorCode.MoveBlocked => "You cannot go that way.",
			ExplorationErrorCode.NoRoomAtTargetCoord => "You cannot go that way.",
			_ => "You cannot go that way."
		};
	}

	public string? ForTurn(ExplorationServiceResult result, DirectionTurned turn)
	{
		if (!result.Success)
			return null;

		if (result.FacingAfterRotation is { } facing)
			return $"You turn {turn.ToString().ToLower()}. You now face {facing}.";

		return null;
	}

	public string ForAboutFace(ExplorationServiceResult result)
	{
		if (result.Success && result.FacingAfterRotation is { } facing)
			return $"You turn around. You now face {facing}.";

		return string.Empty;
	}

	public string ForInspect(ExplorationServiceResult result)
	{
		if (result.Success && result.InspectData is { } data)
			return FormatInspectRoom(data);

		return result.ErrorCode switch
		{
			ExplorationErrorCode.InspectNoFloor or ExplorationErrorCode.InspectNoRoom => "There is nothing to see.",
			_ => "There is nothing to see."
		};
	}

	public string ForMoveUpFloor(ExplorationServiceResult result)
	{
		if (result.Success && result.DestinationAfterMove is { } destination && result.FloorAfterMove is { } floor)
			return $"You ascend to level {floor}. You are in room {destination}.";

		return result.ErrorCode switch
		{
			_ => "You cannot go that way."
		};
	}

    public string ForMoveDownFloor(ExplorationServiceResult result)
    {
        if (result.Success && result.DestinationAfterMove is { } destination && result.FloorAfterMove is { } floor)
            return $"You descend to level {floor}. You are in room {destination}.";

        return result.ErrorCode switch
        {
            _ => "You cannot go that way."
        };
    }

    private static string FormatInspectRoom(InspectRoomData data)
	{
		var description = new StringBuilder();
		description.Append("Exits: ");
		var exitDescriptions = new List<string>();
		foreach (var (side, connection) in data.Exits)
			exitDescriptions.Add($"{side}: {DescribeConnection(connection)}");

		if (exitDescriptions.Count == 0)
			description.Append("none obvious.");
		else
			description.Append(string.Join(", ", exitDescriptions));

		return description.ToString();
	}

	/// <summary>
	/// Vertical connection and room features are logged separately as <see cref="LogEntryKind.Important"/>; not inlined into the main inspect line.
	/// </summary>
	public IReadOnlyList<string> GetInspectImportantLogLines(InspectRoomData data)
	{
		var lines = new List<string>();
		if (data.VerticalConnection != FloorConnectionType.None)
		{
			var vertical = DescribeVertical(data.VerticalConnection);
			if (!string.IsNullOrEmpty(vertical))
				lines.Add(vertical + ".");
		}

		foreach (var line in data.FeatureLines)
		{
			if (!string.IsNullOrEmpty(line.Text))
				lines.Add(line.Text);
		}

		return lines;
	}

	private static string DescribeConnection(RoomConnectionType connectionType) =>
		connectionType switch
		{
			RoomConnectionType.Passage => "open passage",
			RoomConnectionType.Door => "a door",
			RoomConnectionType.SecretDoor => "a solid wall",
			RoomConnectionType.IllusoryWall => "a solid wall",
			_ => "blocked"
		};

	private static string DescribeVertical(FloorConnectionType verticalKind) =>
		verticalKind switch
		{
			FloorConnectionType.Stairs => "Stairs lead down",
			FloorConnectionType.Ladder => "A ladder leads down",
			FloorConnectionType.Hole => "A hole opens in the floor",
			_ => ""
		};

	public string ForCombatStarted(IReadOnlyList<string> livingMonsterNames)
	{
		if (livingMonsterNames.Count == 0)
			return "You thought you saw a goblin, but it was your shadow.";
		var parts = livingMonsterNames
			.Select((name, i) => (name, i))
			.GroupBy(x => x.name)
			.OrderBy(g => g.Min(x => x.i))
			.Select(g =>
			{
				var n = g.Count();
				return n == 1 ? g.Key : $"{n} {g.Key}";
			});
		return $"Combat begins! Facing: {string.Join(", ", parts)}.";
	}

	public string ForCombatInitiativeRoll(string who, DiceRollResult roll) =>
		$"{who} initiative: {roll.SummaryText}. {roll.DetailText}".Trim();

	public string ForCombatTurnOrderSummary(IReadOnlyList<string> namesInOrder) =>
		$"Turn order: {string.Join(" → ", namesInOrder)}.";

	public string ForAttackRoll(string attacker, string target, int totalVsAc, int targetNumber, string detail) =>
		$"{attacker} vs {target} (need {targetNumber}+): rolled {totalVsAc}. {detail}".Trim();

	public string ForAttackMiss(string attacker, string target) =>
		$"{attacker} misses {target}.";

	public string ForDamageDealt(string targetName, int damage, int hpRemaining, int d6Face) =>
		$"You hit {targetName} for {damage} damage (½×d6 from {d6Face}). {targetName} has {hpRemaining} HP left.";

	public string ForMonsterHitPlayer(string monsterName, int damage, int playerHp) =>
		$"{monsterName} hits you for {damage} damage. You have {playerHp} HP left.";

	public string ForFleeRoll(int total, int fleeDc, string detail) =>
		$"Flee attempt: {total} vs DC {fleeDc}. {detail}".Trim();

	public string ForFleeSuccess() => "You break away and retreat to the previous room!";

	public string ForFleeFailure() => "You fail to escape!";

	public string ForCombatVictory() => "You are victorious! The threats here are finished.";

	public string ForPlayerDefeated() => "You fall unconscious. The dungeon goes dark…";

	public string ForTakeNothingHere() => "There is nothing here to take.";

	public string ForTookGold(int amountGp, string treasureName, int totalGoldAfter) =>
		$"You take {treasureName} and gain {amountGp} gp. You now have {totalGoldAfter} gp.";

	public string ForTookItem(string itemName) => $"You take {itemName}.";

	public string ForUsedHealthPotion(int healed, int hpAfter) =>
		$"You drink a health potion and recover {healed} HP. You now have {hpAfter} HP.";

	public string ForHealthPotionAtFullHealth() =>
		"You are already at full health; you save the potion for later.";

	public string ForHealthPotionNoneLeft() => "You have no health potions to use.";
}
