#nullable enable
using Godot;

[GlobalClass]
public partial class GameBalanceSettingsResource : Resource
{
	[Export] public int ExperiencePerFirstRoomVisit { get; set; }
	[Export] public int ExperiencePerFloorEntry { get; set; }
}
