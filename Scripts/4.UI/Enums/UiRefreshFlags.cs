using System;

[Flags]
public enum UiRefreshFlags
{
	None = 0,
	MainView = 1 << 0,
	Character = 1 << 1,
	Log = 1 << 2,
	Command = 1 << 3,
	Map = 1 << 4,

	All = MainView | Character | Log | Command | Map,
}
