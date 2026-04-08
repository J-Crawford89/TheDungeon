using System;
using System.Collections.Generic;

public static class LogArchive
{
	/// <summary>Set once at startup from the host (Godot) layer.</summary>
	public static ILogFileWriter? FileWriter { get; set; }

	/// <param name="writerOverride">When non-null, used instead of <see cref="FileWriter"/> (e.g. tests or alternate sinks).</param>
	public static void AppendEntries(IReadOnlyList<LogEntry> entries, ILogFileWriter? writerOverride = null)
	{
		var writer = writerOverride ?? FileWriter;
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
