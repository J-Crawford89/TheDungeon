using System;
using System.Collections.Generic;
using System.Numerics;

public sealed record DieFaceHullAlignmentResult(
	IReadOnlyDictionary<int, Vector3> FaceNormals,
	IReadOnlyDictionary<int, Quaternion> FaceUpOrientations,
	float MinimumSourceAlignmentDot);

/// <summary>
/// Projects approximate label calibrations onto the actual supporting planes of a convex die.
/// The label calibration supplies the face identity and readable yaw; the collider supplies the
/// physically valid surface normal on which the die can rest.
/// </summary>
public static class DieFaceHullAlignment
{
	private const float MinimumNormalSeparationDot = 0.9999f;

	public static DieFaceHullAlignmentResult Align(
		IReadOnlyList<Vector3> hullPoints,
		IReadOnlyDictionary<int, Quaternion> approximateFaceUpOrientations)
	{
		if (hullPoints == null || hullPoints.Count < 4)
			throw new ArgumentException("A die requires at least four hull points.", nameof(hullPoints));
		if (approximateFaceUpOrientations == null || approximateFaceUpOrientations.Count == 0)
			throw new ArgumentException("At least one face calibration is required.", nameof(approximateFaceUpOrientations));

		var supportNormals = FindSupportPlaneNormals(hullPoints);
		if (supportNormals.Count < approximateFaceUpOrientations.Count)
			throw new ArgumentException(
				$"Hull exposes only {supportNormals.Count} support faces for " +
				$"{approximateFaceUpOrientations.Count} calibrations.",
				nameof(hullPoints));

		var faceValues = new List<int>(approximateFaceUpOrientations.Count);
		var approximateNormals = new List<Vector3>(approximateFaceUpOrientations.Count);
		var approximateOrientations = new Dictionary<int, Quaternion>(approximateFaceUpOrientations.Count);
		foreach (var (face, approximateOrientationValue) in approximateFaceUpOrientations)
		{
			var approximateOrientation = Quaternion.Normalize(approximateOrientationValue);
			faceValues.Add(face);
			approximateNormals.Add(Vector3.Normalize(Vector3.Transform(
				Vector3.UnitY,
				Quaternion.Inverse(approximateOrientation))));
			approximateOrientations[face] = approximateOrientation;
		}

		var supportAssignments = AssignSupportFaces(approximateNormals, supportNormals);
		var alignedNormals = new Dictionary<int, Vector3>(approximateFaceUpOrientations.Count);
		var minimumAlignmentDot = 1f;
		for (var row = 0; row < faceValues.Count; row++)
		{
			var physicalNormal = supportNormals[supportAssignments[row]];
			alignedNormals[faceValues[row]] = physicalNormal;
			minimumAlignmentDot = MathF.Min(
				minimumAlignmentDot,
				Vector3.Dot(approximateNormals[row], physicalNormal));
		}

		var symmetries = FindRotationalSymmetries(hullPoints, supportNormals);
		var referenceFace = -1;
		foreach (var face in approximateFaceUpOrientations.Keys)
		{
			referenceFace = face;
			break;
		}
		var referenceNormal = alignedNormals[referenceFace];
		var referenceApproximateNormal = Vector3.Normalize(Vector3.Transform(
			Vector3.UnitY,
			Quaternion.Inverse(approximateOrientations[referenceFace])));
		var referenceOrientation = Quaternion.Normalize(
			approximateOrientations[referenceFace] *
			RotationBetween(referenceNormal, referenceApproximateNormal));

		var alignedOrientations = new Dictionary<int, Quaternion>(approximateFaceUpOrientations.Count);
		foreach (var (face, physicalNormal) in alignedNormals)
		{
			var bestOrientation = default(Quaternion);
			var bestOrientationDot = float.NegativeInfinity;
			for (var symmetryIndex = 0; symmetryIndex < symmetries.Count; symmetryIndex++)
			{
				var symmetry = symmetries[symmetryIndex];
				if (Vector3.Dot(Vector3.Transform(physicalNormal, symmetry), referenceNormal) < 0.999f)
					continue;
				var candidate = Quaternion.Normalize(referenceOrientation * symmetry);
				var orientationDot = MathF.Abs(Quaternion.Dot(
					candidate,
					approximateOrientations[face]));
				if (orientationDot <= bestOrientationDot)
					continue;
				bestOrientationDot = orientationDot;
				bestOrientation = candidate;
			}

			if (bestOrientationDot < 0f)
				throw new ArgumentException(
					$"No rotational hull symmetry maps face {face} to the reference face " +
					$"({symmetries.Count} symmetries; physical normal {physicalNormal}; " +
					$"reference normal {referenceNormal}).",
					nameof(hullPoints));
			alignedOrientations[face] = bestOrientation;
		}

		return new DieFaceHullAlignmentResult(
			alignedNormals,
			alignedOrientations,
			minimumAlignmentDot);
	}

	private static IReadOnlyList<int> AssignSupportFaces(
		IReadOnlyList<Vector3> approximateNormals,
		IReadOnlyList<Vector3> supportNormals)
	{
		var rowCount = approximateNormals.Count;
		var columnCount = supportNormals.Count;
		var rowPotentials = new float[rowCount + 1];
		var columnPotentials = new float[columnCount + 1];
		var matchedRowByColumn = new int[columnCount + 1];
		var previousColumn = new int[columnCount + 1];
		for (var row = 1; row <= rowCount; row++)
		{
			matchedRowByColumn[0] = row;
			var minimumReducedCost = new float[columnCount + 1];
			Array.Fill(minimumReducedCost, float.PositiveInfinity);
			var usedColumns = new bool[columnCount + 1];
			var currentColumn = 0;
			do
			{
				usedColumns[currentColumn] = true;
				var currentRow = matchedRowByColumn[currentColumn];
				var delta = float.PositiveInfinity;
				var nextColumn = 0;
				for (var column = 1; column <= columnCount; column++)
				{
					if (usedColumns[column])
						continue;
					var cost = -Vector3.Dot(
						approximateNormals[currentRow - 1],
						supportNormals[column - 1]);
					var reducedCost = cost -
						rowPotentials[currentRow] -
						columnPotentials[column];
					if (reducedCost < minimumReducedCost[column])
					{
						minimumReducedCost[column] = reducedCost;
						previousColumn[column] = currentColumn;
					}
					if (minimumReducedCost[column] >= delta)
						continue;
					delta = minimumReducedCost[column];
					nextColumn = column;
				}
				for (var column = 0; column <= columnCount; column++)
				{
					if (usedColumns[column])
					{
						rowPotentials[matchedRowByColumn[column]] += delta;
						columnPotentials[column] -= delta;
					}
					else
						minimumReducedCost[column] -= delta;
				}
				currentColumn = nextColumn;
			}
			while (matchedRowByColumn[currentColumn] != 0);
			do
			{
				var priorColumn = previousColumn[currentColumn];
				matchedRowByColumn[currentColumn] = matchedRowByColumn[priorColumn];
				currentColumn = priorColumn;
			}
			while (currentColumn != 0);
		}
		var assignment = new int[rowCount];
		for (var column = 1; column <= columnCount; column++)
		{
			var matchedRow = matchedRowByColumn[column];
			if (matchedRow > 0)
				assignment[matchedRow - 1] = column - 1;
		}
		return assignment;
	}
	private static IReadOnlyList<Vector3> FindSupportPlaneNormals(
		IReadOnlyList<Vector3> hullPoints)
	{
		var center = Vector3.Zero;
		for (var i = 0; i < hullPoints.Count; i++)
			center += hullPoints[i];
		center /= hullPoints.Count;

		var scale = 0f;
		for (var i = 0; i < hullPoints.Count; i++)
			scale = MathF.Max(scale, Vector3.Distance(center, hullPoints[i]));
		var planeTolerance = MathF.Max(0.0001f, scale * 0.001f);
		var normals = new List<Vector3>();
		var offsets = new List<float>();
		for (var a = 0; a < hullPoints.Count - 2; a++)
		for (var b = a + 1; b < hullPoints.Count - 1; b++)
		for (var c = b + 1; c < hullPoints.Count; c++)
		{
			var cross = Vector3.Cross(
				hullPoints[b] - hullPoints[a],
				hullPoints[c] - hullPoints[a]);
			if (cross.LengthSquared() < 0.00000001f)
				continue;

			var normal = Vector3.Normalize(cross);
			if (Vector3.Dot(normal, center - hullPoints[a]) > 0f)
				normal = -normal;
			var supportsHull = true;
			for (var pointIndex = 0; pointIndex < hullPoints.Count; pointIndex++)
			{
				if (Vector3.Dot(normal, hullPoints[pointIndex] - hullPoints[a]) <= planeTolerance)
					continue;
				supportsHull = false;
				break;
			}
			if (!supportsHull)
				continue;

			var offset = Vector3.Dot(normal, hullPoints[a]);
			var duplicate = false;
			for (var normalIndex = 0; normalIndex < normals.Count; normalIndex++)
			{
				if (Vector3.Dot(normal, normals[normalIndex]) < MinimumNormalSeparationDot ||
					MathF.Abs(offset - offsets[normalIndex]) > planeTolerance)
					continue;
				duplicate = true;
				break;
			}
			if (duplicate)
				continue;

			normals.Add(normal);
			offsets.Add(offset);
		}

		return normals;
	}

	private static IReadOnlyList<Quaternion> FindRotationalSymmetries(
		IReadOnlyList<Vector3> hullPoints,
		IReadOnlyList<Vector3> supportNormals)
	{
		var referenceSecondary = 1;
		while (referenceSecondary < supportNormals.Count &&
			MathF.Abs(Vector3.Dot(supportNormals[0], supportNormals[referenceSecondary])) > 0.95f)
			referenceSecondary++;
		if (referenceSecondary >= supportNormals.Count)
			throw new ArgumentException("Hull does not expose two usable support directions.", nameof(hullPoints));

		var referenceDot = Vector3.Dot(supportNormals[0], supportNormals[referenceSecondary]);
		var symmetries = new List<Quaternion>();
		for (var primary = 0; primary < supportNormals.Count; primary++)
		for (var secondary = 0; secondary < supportNormals.Count; secondary++)
		{
			if (primary == secondary ||
				MathF.Abs(Vector3.Dot(supportNormals[primary], supportNormals[secondary]) - referenceDot) > 0.002f)
				continue;
			var rotation = RotationMappingPairs(
				supportNormals[0],
				supportNormals[referenceSecondary],
				supportNormals[primary],
				supportNormals[secondary]);
			if (!MapsHullOntoItself(hullPoints, rotation))
				continue;

			var duplicate = false;
			for (var i = 0; i < symmetries.Count; i++)
			{
				if (MathF.Abs(Quaternion.Dot(rotation, symmetries[i])) < 0.9999f)
					continue;
				duplicate = true;
				break;
			}
			if (!duplicate)
				symmetries.Add(rotation);
		}

		if (symmetries.Count == 0)
			throw new ArgumentException("No rotational symmetries could be derived from the hull.", nameof(hullPoints));
		return symmetries;
	}

	private static Quaternion RotationMappingPairs(
		Vector3 fromPrimary,
		Vector3 fromSecondary,
		Vector3 toPrimary,
		Vector3 toSecondary)
	{
		var primaryRotation = RotationBetween(fromPrimary, toPrimary);
		var rotatedSecondary = Vector3.Transform(fromSecondary, primaryRotation);
		var axis = Vector3.Normalize(toPrimary);
		var fromTangent = Vector3.Normalize(
			rotatedSecondary - axis * Vector3.Dot(rotatedSecondary, axis));
		var toTangent = Vector3.Normalize(
			toSecondary - axis * Vector3.Dot(toSecondary, axis));
		var signedAngle = MathF.Atan2(
			Vector3.Dot(axis, Vector3.Cross(fromTangent, toTangent)),
			Math.Clamp(Vector3.Dot(fromTangent, toTangent), -1f, 1f));
		var twist = Quaternion.CreateFromAxisAngle(axis, signedAngle);
		var inverseTwist = Quaternion.Inverse(twist);
		var candidates = new[]
		{
			Quaternion.Normalize(twist * primaryRotation),
			Quaternion.Normalize(primaryRotation * twist),
			Quaternion.Normalize(inverseTwist * primaryRotation),
			Quaternion.Normalize(primaryRotation * inverseTwist)
		};
		var best = candidates[0];
		var bestScore = float.NegativeInfinity;
		for (var i = 0; i < candidates.Length; i++)
		{
			var candidate = candidates[i];
			var score = Vector3.Dot(Vector3.Transform(fromPrimary, candidate), toPrimary) +
				Vector3.Dot(Vector3.Transform(fromSecondary, candidate), toSecondary);
			if (score <= bestScore)
				continue;
			best = candidate;
			bestScore = score;
		}
		return best;
	}

	private static bool MapsHullOntoItself(
		IReadOnlyList<Vector3> hullPoints,
		Quaternion rotation)
	{
		var center = Vector3.Zero;
		for (var i = 0; i < hullPoints.Count; i++)
			center += hullPoints[i];
		center /= hullPoints.Count;
		var scale = 0f;
		for (var i = 0; i < hullPoints.Count; i++)
			scale = MathF.Max(scale, Vector3.Distance(center, hullPoints[i]));
		var toleranceSquared = MathF.Pow(MathF.Max(0.002f, scale * 0.005f), 2f);
		for (var i = 0; i < hullPoints.Count; i++)
		{
			var transformed = center + Vector3.Transform(hullPoints[i] - center, rotation);
			var matched = false;
			for (var candidate = 0; candidate < hullPoints.Count; candidate++)
			{
				if (Vector3.DistanceSquared(transformed, hullPoints[candidate]) > toleranceSquared)
					continue;
				matched = true;
				break;
			}
			if (!matched)
				return false;
		}
		return true;
	}
	private static Quaternion RotationBetween(Vector3 from, Vector3 to)
	{
		from = Vector3.Normalize(from);
		to = Vector3.Normalize(to);
		var dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);
		if (dot > 0.9999f)
			return Quaternion.Identity;
		if (dot < -0.9999f)
		{
			var axis = MathF.Abs(from.X) < 0.9f ? Vector3.UnitX : Vector3.UnitZ;
			axis = Vector3.Normalize(Vector3.Cross(from, axis));
			return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
		}

		var cross = Vector3.Cross(from, to);
		return Quaternion.Normalize(new Quaternion(cross.X, cross.Y, cross.Z, 1f + dot));
	}
}
