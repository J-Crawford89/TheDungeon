using System.Collections.Generic;
using Xunit;

public sealed class GameSessionStateLogTests
{
	private sealed class MemoryLogWriter : ILogFileWriter
	{
		public int CallCount { get; private set; }
		public int TotalLinesAppended { get; private set; }

		public void AppendLines(IReadOnlyList<string> lines)
		{
			CallCount++;
			TotalLinesAppended += lines.Count;
		}
	}

	[Fact]
	public void AppendGameLog_BlankInput_Ignored()
	{
		var session = new GameSessionState();

		session.AppendGameLog("   ");

		Assert.Empty(session.LogEntries);
		Assert.Equal(0, session.LogContentRevision);
	}

	[Fact]
	public void AppendLog_BlankEntry_Ignored()
	{
		var session = new GameSessionState();

		session.AppendLog(new LogEntry { Kind = LogEntryKind.Normal, Text = "   " });

		Assert.Empty(session.LogEntries);
	}

	[Fact]
	public void AppendLog_TrimOverflow_ArchivesAndIncrementsRevision()
	{
		var writer = new MemoryLogWriter();
		var session = new GameSessionState(writer);

		for (var i = 0; i <= GameSessionState.MaxLogEntries; i++)
		{
			session.AppendLog(new LogEntry { Kind = LogEntryKind.Normal, Text = $"line-{i}" });
		}

		Assert.Equal(GameSessionState.MaxLogEntries, session.LogEntries.Count);
		Assert.Equal(1, writer.CallCount);
		Assert.Equal(1, writer.TotalLinesAppended);
		Assert.Equal(1, session.LogContentRevision);
	}
}
