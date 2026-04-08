#nullable enable
using System.Collections.Generic;
using Godot;
using FileAccess = Godot.FileAccess;

/// <summary>Writes log lines to user://dungeon_log_trace.txt using Godot file access.</summary>
public sealed class GodotLogFileWriter : ILogFileWriter
{
	private const string ArchiveRelativePath = "user://dungeon_log_trace.txt";

	public void AppendLines(IReadOnlyList<string> lines)
	{
		if (lines.Count == 0)
			return;

		using var file = OpenForAppend();
		if (file == null)
			return;

		foreach (var line in lines)
			file.StoreLine(line);
	}

	private static FileAccess? OpenForAppend()
	{
		if (!FileAccess.FileExists(ArchiveRelativePath))
		{
			using var created = FileAccess.Open(ArchiveRelativePath, FileAccess.ModeFlags.Write);
			if (created == null)
			{
				GD.PrintErr($"GodotLogFileWriter: could not create {ArchiveRelativePath}");
				return null;
			}
		}

		var file = FileAccess.Open(ArchiveRelativePath, FileAccess.ModeFlags.ReadWrite);
		if (file == null)
		{
			GD.PrintErr($"GodotLogFileWriter: could not open {ArchiveRelativePath}");
			return null;
		}

		file.Seek(file.GetLength());
		return file;
	}
}
