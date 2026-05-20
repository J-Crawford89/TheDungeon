using Godot;

/// <summary>Builds four static walls around the dice play area (±X, ±Z) so dice bounce inside the camera view.</summary>
public partial class DicePlayAreaWalls : Node3D
{
	[Export] public Vector3 HalfExtents { get; set; } = new(8.5f, 0f, 4.5f);
	[Export] public float WallHeight { get; set; } = 4f;
	[Export] public float WallThickness { get; set; } = 0.25f;
	[Export] public PhysicsMaterial? WallPhysicsMaterial { get; set; }

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;
		BuildWalls();
	}

	private void BuildWalls()
	{
		var hx = HalfExtents.X;
		var hz = HalfExtents.Z;
		var h = WallHeight;
		var t = WallThickness;
		var yCenter = h * 0.5f;

		CreateWall("WallPosX", new Vector3(hx + t * 0.5f, yCenter, 0), new Vector3(t, h, hz * 2f + t * 2f));
		CreateWall("WallNegX", new Vector3(-hx - t * 0.5f, yCenter, 0), new Vector3(t, h, hz * 2f + t * 2f));
		CreateWall("WallPosZ", new Vector3(0, yCenter, hz + t * 0.5f), new Vector3(hx * 2f + t * 2f, h, t));
		CreateWall("WallNegZ", new Vector3(0, yCenter, -hz - t * 0.5f), new Vector3(hx * 2f + t * 2f, h, t));
	}

	private void CreateWall(string wallName, Vector3 position, Vector3 boxSize)
	{
		var body = new StaticBody3D { Name = wallName, Position = position };
		if (WallPhysicsMaterial != null)
			body.PhysicsMaterialOverride = WallPhysicsMaterial;

		var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = boxSize } };
		body.AddChild(shape);
		AddChild(body);
	}
}
