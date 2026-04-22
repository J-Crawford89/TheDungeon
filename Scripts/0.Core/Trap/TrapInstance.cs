public sealed class TrapInstance
{
	public TrapDefinition Definition { get; set; } = default!;
	public int CurrentHp { get; set; }

	/// <summary>False when <see cref="TrapDefinition.DiscoverDc"/> &gt; 0 until discovered via Inspect.</summary>
	public bool IsRevealed { get; set; } = true;
}