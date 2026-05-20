using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class DieFaceCalibrationVerifier
{
	public sealed record FaceRank(int Face, float DotWithCamera);

	public static IReadOnlyList<FaceRank> RankFaces(
		IReadOnlyDictionary<int, Vector3> localFaceNormals,
		Quaternion bodyRotation,
		Vector3 cameraDirectionWorld)
	{
		cameraDirectionWorld = Vector3.Normalize(cameraDirectionWorld);
		var ranked = new List<FaceRank>(localFaceNormals.Count);
		foreach (var (face, localNormal) in localFaceNormals)
		{
			var world = Vector3.Normalize(Vector3.Transform(localNormal, bodyRotation));
			ranked.Add(new FaceRank(face, Vector3.Dot(world, cameraDirectionWorld)));
		}

		return ranked.OrderByDescending(r => r.DotWithCamera).ToList();
	}
}
