using Godot;
using NumericVector3 = System.Numerics.Vector3;

/// <summary>Builds four static walls around the dice play area (±X, ±Z) so dice bounce inside the camera view.</summary>
public partial class DicePlayAreaWalls : Node3D
{
	[Export] public Vector3 HalfExtents { get; set; } = new(8.5f, 0f, 4.5f);
	[Export] public float WallHeight { get; set; } = 4f;
	[Export] public float WallThickness { get; set; } = 0.25f;
	[Export] public PhysicsMaterial? WallPhysicsMaterial { get; set; }
	[Export] public bool ShowDebugWallMeshes { get; set; } = false;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;
		BuildWalls();
	}

	private void BuildWalls()
	{
		var specs = DicePlayAreaWallLayout.Build(
			new NumericVector3(HalfExtents.X, HalfExtents.Y, HalfExtents.Z),
			WallHeight,
			WallThickness);

		foreach (var spec in specs)
		{
			CreateWall(
				spec.Name,
				new Vector3(spec.Position.X, spec.Position.Y, spec.Position.Z),
				new Vector3(spec.Size.X, spec.Size.Y, spec.Size.Z));
		}
	}

	private void CreateWall(string wallName, Vector3 position, Vector3 boxSize)
	{
		var body = new StaticBody3D { Name = wallName, Position = position };
		if (WallPhysicsMaterial != null)
			body.PhysicsMaterialOverride = WallPhysicsMaterial;

		var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = boxSize } };
		body.AddChild(shape);

		if (ShowDebugWallMeshes)
		{
			var material = new StandardMaterial3D
			{
				AlbedoColor = new Color(1f, 0.15f, 0.15f, 0.3f),
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			};
			var mesh = new MeshInstance3D
			{
				Name = "DebugMesh",
				Mesh = new BoxMesh { Size = boxSize },
				MaterialOverride = material,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			body.AddChild(mesh);
		}

		AddChild(body);
	}
}
