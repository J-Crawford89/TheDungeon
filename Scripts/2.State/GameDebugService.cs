public sealed class GameDebugService
{
	public bool IsCaptureEnabled { get; set; }

	public void Report(GameSessionState? session, string message)
	{
		if (session == null || string.IsNullOrWhiteSpace(message) || !IsCaptureEnabled)
			return;
		session.AppendLog(new LogEntry { Kind = LogEntryKind.Debug, Text = message.Trim() });
	}
}
