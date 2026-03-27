public sealed class CombatTurnSlot
{
	public bool IsPlayer { get; init; }

	/// <summary>Index into <see cref="MonsterFeature.Monsters"/> when <see cref="IsPlayer"/> is false.</summary>
	public int MonsterIndex { get; init; }
}
