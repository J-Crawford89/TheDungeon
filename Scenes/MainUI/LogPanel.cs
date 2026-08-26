#nullable enable
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class LogPanel : PanelContainer
{
	[Export] private VBoxContainer _logEntriesContainer = null!;
	[Export] private PackedScene _logEntryScene = null!;

	private int _lastLogContentRevision = -1;
	private int _renderedCount;

	public void SyncFromSession(GameSessionState session)
	{
		var entries = session.LogEntries;
		if (session.LogContentRevision != _lastLogContentRevision)
		{
			FullRebuild(entries);
			_lastLogContentRevision = session.LogContentRevision;
			_renderedCount = entries.Count;
			ScrollToBottomDeferred();
			return;
		}

		for (var i = _renderedCount; i < entries.Count; i++)
		{
			var control = _logEntryScene.Instantiate<LogEntryControl>();
			_logEntriesContainer.AddChild(control);
			control.SetEntry(entries[i]);
		}

		_renderedCount = entries.Count;
		ScrollToBottomDeferred();
	}

	public void ClearLog()
	{
		foreach (var child in _logEntriesContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void FullRebuild(IReadOnlyList<LogEntry> entries)
	{
		ClearLog();
		for (var i = 0; i < entries.Count; i++)
		{
			var control = _logEntryScene.Instantiate<LogEntryControl>();
			_logEntriesContainer.AddChild(control);
			control.SetEntry(entries[i]);
		}
	}

	private void ScrollToBottomDeferred() => CallDeferred(nameof(ScrollLogToBottomImpl));

	private void ScrollLogToBottomImpl() => GodotAsyncEventHandler.Run(ScrollLogToBottomAsync, "Scroll game log");

	private async Task ScrollLogToBottomAsync()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var scrollContainer = _logEntriesContainer.GetParent<ScrollContainer>();
		scrollContainer.ScrollVertical = (int)scrollContainer.GetVScrollBar().MaxValue;
	}
}
