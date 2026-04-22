public sealed class NpcInstance
{
	public NpcDefinition Definition { get; set; } = default!;
	public int CurrentHp { get; set; }

	public bool IsRevealed { get; set; } = true;
}