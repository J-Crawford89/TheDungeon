using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

public sealed class DieFaceHullAlignmentTests
{
	[Fact]
	public void Align_D10ProjectsApproximateLabelDirectionsOntoPhysicalKiteFaces()
	{
		var points = CreateD10Points();
		var approximate = CreateD10ApproximateFaceOrientations();

		var result = DieFaceHullAlignment.Align(points, approximate);

		Assert.Equal(10, result.FaceNormals.Count);
		Assert.InRange(result.MinimumSourceAlignmentDot, 0.88f, 0.89f);
		Assert.Equal(10, result.FaceNormals.Values.Distinct().Count());
		foreach (var (face, physicalNormal) in result.FaceNormals)
		{
			var worldNormal = Vector3.Normalize(Vector3.Transform(
				physicalNormal,
				result.FaceUpOrientations[face]));
			Assert.True(Vector3.Dot(worldNormal, Vector3.UnitY) > 0.9999f);

			var secondBestDot = result.FaceNormals
				.Where(entry => entry.Key != face)
				.Max(entry => Vector3.Dot(
					Vector3.Normalize(Vector3.Transform(
						entry.Value,
						result.FaceUpOrientations[face])),
					Vector3.UnitY));
			Assert.True(
				secondBestDot < 0.75f,
				$"Face {face} remained ambiguous; neighboring face dot was {secondBestDot:F3}.");
		}
	}

	[Fact]
	public void Align_D10FaceOffsetsRemainSymmetriesOfTheCollider()
	{
		var points = CreateD10Points();
		var result = DieFaceHullAlignment.Align(
			points,
			CreateD10ApproximateFaceOrientations());
		var natural = result.FaceUpOrientations[1];

		foreach (var desired in result.FaceUpOrientations.Values)
		{
			var offset = Quaternion.Normalize(Quaternion.Inverse(natural) * desired);
			foreach (var point in points)
			{
				var transformed = Vector3.Transform(point, offset);
				var nearestDistance = points.Min(candidate => Vector3.Distance(candidate, transformed));
				Assert.True(
					nearestDistance < 0.002f,
					$"Mapped hull point missed the physical d10 symmetry by {nearestDistance:F4}.");
			}
		}
	}

	[Fact]
	public void Align_AlreadyPhysicalCubeCalibrationRemainsUnchanged()
	{
		var points = new List<Vector3>();
		foreach (var x in new[] { -1f, 1f })
		foreach (var y in new[] { -1f, 1f })
		foreach (var z in new[] { -1f, 1f })
			points.Add(new Vector3(x, y, z));
		var orientations = new Dictionary<int, Quaternion>
		{
			[1] = Quaternion.Identity,
			[2] = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI),
		};

		var result = DieFaceHullAlignment.Align(points, orientations);

		Assert.True(result.MinimumSourceAlignmentDot > 0.9999f);
		Assert.True(MathF.Abs(Quaternion.Dot(
			Quaternion.Normalize(orientations[1]),
			result.FaceUpOrientations[1])) > 0.9999f);
	}

	private static IReadOnlyList<Vector3> CreateD10Points() =>
	[
		new(-0.2534808f, -0.071887195f, -0.77986634f),
		new(-0.0001604557f, -0.68f, -0.0001526475f),
		new(-0.82f, -0.071887195f, -0.0001526475f),
		new(-0.6634808f, 0.07175416f, -0.48199648f),
		new(0.2533204f, 0.07175416f, -0.77986634f),
		new(0.66332036f, -0.071887195f, -0.48199648f),
		new(-0.2534808f, -0.071887195f, 0.77986634f),
		new(0.66332036f, -0.071887195f, 0.48184383f),
		new(-0.6634808f, 0.07175416f, 0.48184383f),
		new(-0.0001604557f, 0.68f, -0.0001526475f),
		new(0.8200001f, 0.07175416f, -0.0001526475f),
		new(0.2533204f, 0.07175416f, 0.77986634f),
	];

	private static IReadOnlyDictionary<int, Quaternion> CreateD10ApproximateFaceOrientations() =>
		new Dictionary<int, Quaternion>
		{
			[1] = new(0.33948f, 0f, 0.467255f, 0.816349f),
			[2] = new(0f, 0f, 0.816349f, 0.577559f),
			[3] = new(0.549291f, 0f, -0.178475f, 0.816349f),
			[4] = new(-0.776394f, 0f, 0.252266f, 0.577559f),
			[5] = new(0f, 0f, -0.577559f, 0.816349f),
			[6] = new(-0.479838f, 0f, -0.66044f, 0.577559f),
			[7] = new(-0.549291f, 0f, -0.178475f, 0.816349f),
			[8] = new(0.479838f, 0f, -0.66044f, 0.577559f),
			[9] = new(-0.33948f, 0f, 0.467255f, 0.816349f),
			[10] = new(0.776394f, 0f, 0.252266f, 0.577559f),
		};
}
