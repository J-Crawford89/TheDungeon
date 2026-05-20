using Godot;

[GlobalClass]
public partial class DieFaceCalibrationEntry : Resource
{
	[Export] public int Face { get; set; } = 1;

	/// <summary>
	/// Die rotation (quaternion) when this face is the intended "up" read face before camera snap.
	/// Do not use Euler degrees here.
	/// </summary>
	[Export] public Quaternion FaceOrientation { get; set; } = Quaternion.Identity;
}
