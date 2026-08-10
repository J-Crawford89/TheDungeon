using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DieFaceCalibration : Node
{
	[Export] public Godot.Collections.Array<DieFaceCalibrationEntry> Faces { get; set; } = [];

	private Dictionary<int, Quaternion>? _orientations;

	public bool TryGetFaceOrientation(int faceValue, out Quaternion orientation)
	{
		_orientations ??= BuildLookup();
		if (_orientations.TryGetValue(faceValue, out orientation))
			return true;
		orientation = Quaternion.Identity;
		return false;
	}

	public IReadOnlyDictionary<int, Vector3> GetLocalFaceNormals()
	{
		var map = new Dictionary<int, Vector3>();
		_orientations ??= BuildLookup();
		foreach (var (face, quat) in _orientations)
		{
			var basis = new Basis(quat);
			map[face] = basis.Inverse().Y;
		}
		return map;
	}

	public IReadOnlyDictionary<int, Quaternion> GetFaceUpOrientations()
	{
		_orientations ??= BuildLookup();
		return new Dictionary<int, Quaternion>(_orientations);
	}

	private Dictionary<int, Quaternion> BuildLookup()
	{
		var map = new Dictionary<int, Quaternion>();
		if (Faces == null)
			return map;
		foreach (var entry in Faces)
		{
			if (entry == null)
				continue;
			map[entry.Face] = entry.FaceOrientation;
		}
		return map;
	}
}
