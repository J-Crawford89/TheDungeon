public sealed class GrantedAbility
{
	public string AbilityId { get; set; } = string.Empty;
	public AbilityGrantSourceType SourceType { get; set; }
	public string SourceId { get; set; } = string.Empty;
	public int LevelGranted { get; set; }
	public bool IsTemporary { get; set; }
}
