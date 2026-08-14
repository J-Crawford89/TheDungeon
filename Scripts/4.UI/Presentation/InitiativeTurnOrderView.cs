using System.Collections.Generic;

public sealed class InitiativeTurnOrderView
{
	public IReadOnlyList<string> Names { get; init; } = [];

	public int CurrentIndex { get; init; }

	public string OverlayText =>
		Names.Count == 0 ? string.Empty : string.Join(" → ", Names);

	public static InitiativeTurnOrderView FromSession(GameSessionState session)
	{
		if (session.Dungeon.DungeonMode != DungeonMode.Combat)
			return new InitiativeTurnOrderView();
		if (session.Combat is not { } combat || combat.TurnOrder.Count == 0)
			return new InitiativeTurnOrderView();

		var feature = session.Dungeon.CurrentRoom is { } room
			? RoomFeatureHelper.GetFeature<MonsterFeature>(room)
			: null;
		var names = new List<string>(combat.TurnOrder.Count);
		foreach (var slot in combat.TurnOrder)
			names.Add(NameFor(slot, feature));

		var index = combat.CurrentTurnIndex;
		if (index < 0 || index >= names.Count)
			index = 0;

		return new InitiativeTurnOrderView
		{
			Names = names,
			CurrentIndex = index,
		};
	}

	private static string NameFor(CombatTurnSlot slot, MonsterFeature? feature)
	{
		if (slot.IsPlayer)
			return "You";
		if (feature != null && slot.MonsterIndex >= 0 && slot.MonsterIndex < feature.Monsters.Count)
			return feature.Monsters[slot.MonsterIndex].Definition.Name;
		return "Monster";
	}
}
