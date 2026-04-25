using System;
using System.Collections.Generic;

public static class LogArchive
{
	public static void AppendEntries(IReadOnlyList<LogEntry> entries, ILogFileWriter? writer)
	{
		if (entries.Count == 0 || writer == null)
			return;

		var lines = new List<string>(entries.Count);
		foreach (var e in entries)
		{
			var detail = string.IsNullOrEmpty(e.DetailText) ? "" : $" | {e.DetailText}";
			lines.Add($"[{DateTime.Now:O}] [{e.Kind}] {e.Text}{detail}");
		}

		writer.AppendLines(lines);
	}
}
