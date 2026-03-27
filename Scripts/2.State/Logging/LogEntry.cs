public sealed class LogEntry
{
	public string Text { get; set; } = string.Empty;
	public string? DetailText { get; set; }
	public LogEntryKind Kind { get; set; } = LogEntryKind.Normal;
}
