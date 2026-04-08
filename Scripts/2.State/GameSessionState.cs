using System.Collections.Generic;

public sealed class GameSessionState : IGameLog
{
	public const int MaxLogEntries = 3000;

	private readonly ILogFileWriter? _logArchiveWriter;

	public GameSessionState(ILogFileWriter? logArchiveWriter = null) =>
		_logArchiveWriter = logArchiveWriter;

	public PlayerState Player { get; } = new();
	public DungeonState Dungeon { get; } = new();
	public GameDebugService Debug { get; } = new();
	public CombatState? Combat { get; set; }
	public List<LogEntry> LogEntries { get; } = new();

	public GamePlayPhase Phase { get; set; } = GamePlayPhase.InProgress;
	public FallenAdventurerRecord? LastFallenAdventurer { get; set; }
	public string? GameOverTitle { get; set; }
	public string? GameOverBody { get; set; }

	public int LogContentRevision { get; private set; }

	/// <summary>
	/// Clears dungeon, combat, log, and resets the player for a new run. Preserves <see cref="LastFallenAdventurer"/> for future same-layout corpse placement.
	/// </summary>
	public void ResetForNewRunPreservingFallenRecord()
	{
		Phase = GamePlayPhase.InProgress;
		GameOverTitle = null;
		GameOverBody = null;
		Combat = null;
		Player.ResetToNewAdventurer();
		Dungeon.Floors.Clear();
		Dungeon.DiscoveredRoomsByFloor.Clear();
		Dungeon.CurrentFloor = null;
		Dungeon.PlayerCoord = default;
		Dungeon.DungeonMode = DungeonMode.Exploration;
		LogEntries.Clear();
		LogContentRevision++;
	}

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
		LogArchive.AppendEntries(batch, _logArchiveWriter);
		LogContentRevision++;
	}
}
