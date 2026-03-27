using System;
using System.Collections.Generic;

public static class LogArchive
{
	/// <summary>Set once at startup from the host (Godot) layer.</summary>
	public static ILogFileWriter? FileWriter { get; set; }

	public static void AppendEntries(IReadOnlyList<LogEntry> entries)
	{
		if (entries.Count == 0 || FileWriter == null)
			return;

		var lines = new List<string>(entries.Count);
		foreach (var e in entries)
		{
			var detail = string.IsNullOrEmpty(e.DetailText) ? "" : $" | {e.DetailText}";
			lines.Add($"[{DateTime.Now:O}] [{e.Kind}] {e.Text}{detail}");
		}

		FileWriter.AppendLines(lines);
	}
}
