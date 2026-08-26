#nullable enable
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Typed root for a physical die visual. The Godot editor owns the calibration-node reference;
/// rolling code should not search an imported model tree at runtime.
/// </summary>
[GlobalClass]
public partial class DieVisualBody : RigidBody3D
{
	[Export] public DieFaceCalibration? Calibration { get; set; }
	[Export] public MeshInstance3D? BodyMesh { get; set; }

	public void ApplyBodyMaterial(Material? material)
	{
		if (material == null)
			return;
		if (BodyMesh == null)
		{
			GD.PushWarning($"{Name}: assign {nameof(BodyMesh)} to apply the dice-set body material.");
			return;
		}

		BodyMesh.MaterialOverride = material;
	}

	public IReadOnlyList<System.Numerics.Vector3> GetConvexHullPoints()
	{
		foreach (var child in GetChildren())
		{
			if (child is not CollisionShape3D { Shape: ConvexPolygonShape3D convex } collider)
				continue;

			var points = new System.Numerics.Vector3[convex.Points.Length];
			for (var i = 0; i < convex.Points.Length; i++)
			{
				var transformed = collider.Transform * convex.Points[i];
				points[i] = new System.Numerics.Vector3(
					transformed.X,
					transformed.Y,
					transformed.Z);
			}
			return points;
		}

		throw new InvalidOperationException(
			$"{Name}: expected a direct CollisionShape3D child using ConvexPolygonShape3D.");
	}
}
