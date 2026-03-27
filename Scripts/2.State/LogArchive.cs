using System;
using System.Collections.Generic;
using Godot;

public static class LogArchive
{
	private const string ArchiveRelativePath = "user://dungeon_log_trace.txt";

	public static void AppendEntries(IReadOnlyList<LogEntry> entries)
	{
		if (entries.Count == 0)
			return;

		using var file = OpenForAppend();
		if (file == null)
			return;

		foreach (var e in entries)
		{
			var detail = string.IsNullOrEmpty(e.DetailText) ? "" : $" | {e.DetailText}";
			file.StoreLine($"[{DateTime.Now:O}] [{e.Kind}] {e.Text}{detail}");
		}
	}

	private static FileAccess? OpenForAppend()
	{
		if (!FileAccess.FileExists(ArchiveRelativePath))
		{
			using var created = FileAccess.Open(ArchiveRelativePath, FileAccess.ModeFlags.Write);
			if (created == null)
			{
				GD.PrintErr($"LogArchive: could not create {ArchiveRelativePath}");
				return null;
			}
		}

		var file = FileAccess.Open(ArchiveRelativePath, FileAccess.ModeFlags.ReadWrite);
		if (file == null)
		{
			GD.PrintErr($"LogArchive: could not open {ArchiveRelativePath}");
			return null;
		}

		file.Seek(file.GetLength());
		return file;
	}
}
