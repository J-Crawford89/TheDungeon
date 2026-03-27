using System.Collections.Generic;

public sealed class GameSessionState
{
	public const int MaxLogEntries = 3000;

	public PlayerState Player { get; } = new();
	public DungeonState Dungeon { get; } = new();
	public GameDebugService Debug { get; } = new();
	public CombatState? Combat { get; set; }
	public List<LogEntry> LogEntries { get; } = new();

	public int LogContentRevision { get; private set; }

	public void AppendGameLog(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
			return;
		AppendLog(new LogEntry { Kind = LogEntryKind.Normal, Text = line.TrimEnd() });
	}

	public void AppendLog(LogEntry entry)
	{
		if (entry == null || string.IsNullOrWhiteSpace(entry.Text))
			return;
		var text = entry.Text.TrimEnd();
		if (string.IsNullOrWhiteSpace(text))
			return;
		var detail = string.IsNullOrWhiteSpace(entry.DetailText) ? null : entry.DetailText.TrimEnd();
		LogEntries.Add(new LogEntry { Kind = entry.Kind, Text = text, DetailText = detail });
		TrimIfNeeded();
	}

	private void TrimIfNeeded()
	{
		if (LogEntries.Count <= MaxLogEntries)
			return;
		var overflow = LogEntries.Count - MaxLogEntries;
		var batch = LogEntries.GetRange(0, overflow);
		LogEntries.RemoveRange(0, overflow);
		LogArchive.AppendEntries(batch);
		LogContentRevision++;
	}
}
