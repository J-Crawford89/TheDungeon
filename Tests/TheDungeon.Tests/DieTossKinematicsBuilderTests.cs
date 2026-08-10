using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

public sealed class DieTossKinematicsBuilderTests
{
	[Fact]
	public void Build_CouplesCubeRollRateToThrowSpeedAndRadius()
	{
		var points = CreateCubePoints();

		var result = DieTossKinematicsBuilder.Build(
			points,
			Quaternion.Identity,
			new Vector3(8f, 0f, 0f),
			rollCoupling: 1f,
			Vector3.UnitY,
			tumbleSpeed: 0f);

		var expectedRadius = MathF.Sqrt(2f) * 0.57735026f;
		Assert.Equal(expectedRadius, result.EffectiveRollingRadius, 4);
		Assert.Equal(8f / expectedRadius, result.NominalRollingAngularSpeed, 4);
		Assert.Equal(0f, result.AngularVelocity.X, 4);
		Assert.Equal(0f, result.AngularVelocity.Y, 4);
		Assert.Equal(-result.NominalRollingAngularSpeed, result.AngularVelocity.Z, 4);
		Assert.Equal(0f, result.InitialSurfaceSlipSpeed, 4);
	}

	[Fact]
	public void Build_PartialCouplingLeavesOnlyTheConfiguredReleaseSlip()
	{
		var result = DieTossKinematicsBuilder.Build(
			CreateCubePoints(),
			Quaternion.Identity,
			new Vector3(8f, 0f, 0f),
			rollCoupling: 0.85f,
			Vector3.UnitY,
			tumbleSpeed: 0f);

		Assert.Equal(1.2f, result.InitialSurfaceSlipSpeed, 3);
		Assert.Equal(
			result.NominalRollingAngularSpeed * 0.85f,
			result.AngularVelocity.Length(),
			4);
	}

	[Fact]
	public void Build_AddsOnlyTheRequestedBoundedTumble()
	{
		var withoutTumble = DieTossKinematicsBuilder.Build(
			CreateCubePoints(),
			Quaternion.Identity,
			new Vector3(7f, 0f, 1f),
			rollCoupling: 0.9f,
			Vector3.UnitY,
			tumbleSpeed: 0f);
		var withTumble = DieTossKinematicsBuilder.Build(
			CreateCubePoints(),
			Quaternion.Identity,
			new Vector3(7f, 0f, 1f),
			rollCoupling: 0.9f,
			Vector3.UnitY,
			tumbleSpeed: 1.25f);

		Assert.Equal(
			Vector3.UnitY * 1.25f,
			withTumble.AngularVelocity - withoutTumble.AngularVelocity);
	}

	[Fact]
	public void ComputeEffectiveRollingRadius_AccountsForStartOrientation()
	{
		var points = new List<Vector3>
		{
			new(-1f, -0.5f, -0.25f), new(-1f, -0.5f, 0.25f),
			new(-1f, 0.5f, -0.25f), new(-1f, 0.5f, 0.25f),
			new(1f, -0.5f, -0.25f), new(1f, -0.5f, 0.25f),
			new(1f, 0.5f, -0.25f), new(1f, 0.5f, 0.25f),
		};

		var identityRadius = DieTossKinematicsBuilder.ComputeEffectiveRollingRadius(
			points,
			Quaternion.Identity,
			Vector3.UnitZ);
		var rotatedRadius = DieTossKinematicsBuilder.ComputeEffectiveRollingRadius(
			points,
			Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.5f),
			Vector3.UnitZ);

		Assert.Equal(MathF.Sqrt(1.25f), identityRadius, 4);
		Assert.Equal(MathF.Sqrt(0.3125f), rotatedRadius, 4);
	}

	private static IReadOnlyList<Vector3> CreateCubePoints()
	{
		const float extent = 0.57735026f;
		var points = new List<Vector3>(8);
		foreach (var x in new[] { -extent, extent })
		foreach (var y in new[] { -extent, extent })
		foreach (var z in new[] { -extent, extent })
			points.Add(new Vector3(x, y, z));
		return points;
	}
}
