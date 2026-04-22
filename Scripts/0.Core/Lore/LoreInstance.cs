public sealed class LoreInstance
{
	public LoreDefinition Definition { get; set; } = default!;
	public int CurrentHp { get; set; }

	public bool IsRevealed { get; set; } = true;
}