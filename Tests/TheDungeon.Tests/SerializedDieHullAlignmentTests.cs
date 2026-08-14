using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Xunit;

public sealed class SerializedDieHullAlignmentTests
{
	[Theory]
	[InlineData("d3_visual.tscn")]
	[InlineData("d4_visual.tscn")]
	[InlineData("d6_visual.tscn")]
	[InlineData("d8_visual.tscn")]
	[InlineData("d10_visual.tscn")]
	[InlineData("d10_percentile_visual.tscn")]
	[InlineData("d12_visual.tscn")]
	[InlineData("d20_visual.tscn")]
	public void Align_CurrentSerializedDieCalibrationUsesPhysicalHullSymmetries(string sceneFile)
	{
		var sceneText = File.ReadAllText(Path.Combine(
			FindRepositoryRoot(),
			"Scenes",
			"Components",
			"Dice",
			sceneFile));
		var points = ParsePoints(sceneText);
		var approximateOrientations = ParseFaceOrientations(sceneText);

		var result = DieFaceHullAlignment.Align(points, approximateOrientations);

		Assert.Equal(approximateOrientations.Count, result.FaceNormals.Count);
		foreach (var (face, physicalNormal) in result.FaceNormals)
		{
			var worldNormal = Vector3.Normalize(Vector3.Transform(
				physicalNormal,
				result.FaceUpOrientations[face]));
			Assert.True(
				Vector3.Dot(worldNormal, Vector3.UnitY) > 0.999f,
				$"{sceneFile} face {face} was not aligned to a physical support face.");
		}

		var center = points.Aggregate(Vector3.Zero, (sum, point) => sum + point) / points.Count;
		var scale = points.Max(point => Vector3.Distance(center, point));
		foreach (var natural in result.FaceUpOrientations)
		{
			foreach (var desired in result.FaceUpOrientations)
			{
				var offset = Quaternion.Normalize(Quaternion.Inverse(natural.Value) * desired.Value);
				foreach (var point in points)
				{
					var transformed = center + Vector3.Transform(point - center, offset);
					var nearestDistance = points.Min(candidate => Vector3.Distance(candidate, transformed));
					Assert.True(
						nearestDistance <= MathF.Max(0.002f, scale * 0.005f),
						$"{sceneFile} face offset {natural.Key}->{desired.Key} is not a collider symmetry.");
				}
			}
		}
	}

	private static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot")))
			directory = directory.Parent;
		return directory?.FullName ?? throw new DirectoryNotFoundException(
			"Could not locate the Godot repository root from the test output directory.");
	}

	private static IReadOnlyList<Vector3> ParsePoints(string sceneText)
	{
		var match = Regex.Match(
			sceneText,
			@"points = PackedVector3Array\((?<values>[^\)]*)\)");
		Assert.True(match.Success, "Scene has no ConvexPolygonShape3D points.");
		var values = match.Groups["values"].Value
			.Split(',')
			.Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
			.ToArray();
		Assert.True(values.Length >= 12 && values.Length % 3 == 0);
		var points = new List<Vector3>(values.Length / 3);
		for (var i = 0; i < values.Length; i += 3)
			points.Add(new Vector3(values[i], values[i + 1], values[i + 2]) * 1.01f);
		return points;
	}

	private static IReadOnlyDictionary<int, Quaternion> ParseFaceOrientations(string sceneText)
	{
		var result = new Dictionary<int, Quaternion>();
		var resources = Regex.Matches(
			sceneText,
			@"\[sub_resource[^\]]*\](?<body>.*?)(?=\r?\n\[)",
			RegexOptions.Singleline);
		foreach (Match resource in resources)
		{
			var body = resource.Groups["body"].Value;
			var orientationMatch = Regex.Match(
				body,
				@"FaceOrientation = Quaternion\((?<values>[^\)]*)\)");
			if (!orientationMatch.Success)
				continue;
			var faceMatch = Regex.Match(body, @"(?:^|\r?\n)Face = (?<face>-?\d+)");
			var face = faceMatch.Success
				? int.Parse(faceMatch.Groups["face"].Value, CultureInfo.InvariantCulture)
				: 1;
			var values = orientationMatch.Groups["values"].Value
				.Split(',')
				.Select(value => float.Parse(value.Trim(), CultureInfo.InvariantCulture))
				.ToArray();
			Assert.Equal(4, values.Length);
			result[face] = new Quaternion(values[0], values[1], values[2], values[3]);
		}
		Assert.NotEmpty(result);
		return result;
	}
}
