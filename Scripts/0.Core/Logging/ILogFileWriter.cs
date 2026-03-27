using System.Collections.Generic;

/// <summary>Appends text lines to a persistent log (implementation may be Godot file I/O or tests).</summary>
public interface ILogFileWriter
{
	void AppendLines(IReadOnlyList<string> lines);
}
