using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct SpawnPathData
{
	public enum SnapMethod
	{
		DontSnap,
		SnapToTerrain,
		SnapToWaterLevel
	}

	public const float SpawnPathEpsilon = 0.01f;

	public static readonly SpawnPathData Invalid = new SpawnPathData(null, null, 0f, isInverted: false, SnapMethod.DontSnap);

	public readonly Scene Scene;

	public readonly Path Path;

	public readonly bool IsInverted;

	public readonly float PivotRatio;

	public readonly SnapMethod SnapType;

	public bool IsValid
	{
		get
		{
			if (Scene != null && Path != null)
			{
				return Path.NumberOfPoints > 1;
			}
			return false;
		}
	}

	public SpawnPathData Invert()
	{
		return new SpawnPathData(Scene, Path, MathF.Max(1f - PivotRatio, 0f), !IsInverted, SnapType);
	}

	public float ClampPathOffset(float pathOffsetRatio)
	{
		return MathF.Clamp(PivotRatio + pathOffsetRatio, 0f, 1f) - PivotRatio;
	}

	public float GetOffsetOverflow(float pathOffset)
	{
		float num = PivotRatio + pathOffset;
		if (num < 0f)
		{
			return num;
		}
		if (num > 1f)
		{
			return num - 1f;
		}
		return 0f;
	}

	public void GetSpawnPathFrameFacingTarget(float baseOffset, float targetOffset, bool useTangentDirection, out Vec2 spawnPathPosition, out Vec2 spawnPathDirection, bool decideDirectionDynamically = false, float dynamicDistancePercentage = 0.2f)
	{
		if (baseOffset == targetOffset)
		{
			GetSpawnPathFrameFacingPivot(baseOffset, useTangentDirection, out spawnPathPosition, out spawnPathDirection);
			return;
		}
		baseOffset = ClampPathOffset(baseOffset);
		MatrixFrame spawnFrame = GetSpawnFrame(baseOffset);
		spawnPathPosition = spawnFrame.origin.AsVec2;
		targetOffset = ClampPathOffset(targetOffset);
		MatrixFrame spawnFrame2 = GetSpawnFrame(targetOffset);
		if (decideDirectionDynamically && !useTangentDirection)
		{
			WorldPosition point = new WorldPosition(Scene, spawnFrame.origin);
			WorldPosition point2 = new WorldPosition(Scene, spawnFrame2.origin);
			if (Scene.GetPathDistanceBetweenPositions(ref point, ref point2, 0.1f, out var pathDistance))
			{
				float length = (spawnFrame2.origin - spawnFrame.origin).Length;
				useTangentDirection = pathDistance >= length * (1f + dynamicDistancePercentage);
			}
		}
		if (useTangentDirection)
		{
			int num = ((targetOffset >= baseOffset) ? 1 : (-1));
			float pathOffset = MathF.Clamp(baseOffset + (float)num * 0.01f, -1f, 1f);
			spawnFrame2 = GetSpawnFrame(pathOffset);
		}
		spawnPathDirection = (spawnFrame2.origin.AsVec2 - spawnPathPosition).Normalized();
	}

	public void GetSpawnPathFrameFacingPivot(float pathOffset, bool useTangentDirection, out Vec2 spawnPathPosition, out Vec2 spawnPathDirection)
	{
		pathOffset = ClampPathOffset(pathOffset);
		spawnPathPosition = GetSpawnFrame(pathOffset).origin.AsVec2;
		float num = 0f;
		if (useTangentDirection || num == pathOffset)
		{
			int num2 = ((!(pathOffset >= 0f)) ? 1 : (-1));
			num = MathF.Clamp(pathOffset + (float)num2 * 0.01f, -1f, 1f);
		}
		spawnPathDirection = (GetSpawnFrame(num).origin.AsVec2 - spawnPathPosition).Normalized();
	}

	public void GetSpawnPathFrameFacingTangentDirection(float baseOffset, int tangentDirection, out Vec2 spawnPathPosition, out Vec2 spawnPathDirection)
	{
		baseOffset = ClampPathOffset(baseOffset);
		spawnPathPosition = GetSpawnFrame(baseOffset).origin.AsVec2;
		tangentDirection = ((tangentDirection >= 0) ? 1 : (-1));
		float pathOffsetRatio = MathF.Clamp(baseOffset + (float)tangentDirection * 0.01f, -1f, 1f);
		pathOffsetRatio = ClampPathOffset(pathOffsetRatio);
		spawnPathDirection = (GetSpawnFrame(pathOffsetRatio).origin.AsVec2 - spawnPathPosition).Normalized();
	}

	private SpawnPathData(Scene scene = null, Path path = null, float pivotRatio = 0f, bool isInverted = false, SnapMethod snapType = SnapMethod.DontSnap)
	{
		Scene = scene;
		Path = path;
		PivotRatio = MathF.Clamp(pivotRatio, 0.01f, 0.99f);
		IsInverted = isInverted;
		SnapType = snapType;
	}

	private MatrixFrame GetSpawnFrame(float pathOffset)
	{
		MatrixFrame result = MatrixFrame.Identity;
		if (IsValid)
		{
			pathOffset = MathF.Clamp(PivotRatio + pathOffset, 0f, 1f);
			pathOffset = (IsInverted ? (1f - pathOffset) : pathOffset);
			float distance = Path.GetTotalLength() * pathOffset;
			bool searchForward = (IsInverted ? true : false);
			result = Path.GetNearestFrameWithValidAlphaForDistance(distance, searchForward);
			result.rotation.f = (IsInverted ? (-result.rotation.f) : result.rotation.f);
			result.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			if (SnapType != 0)
			{
				if (SnapType == SnapMethod.SnapToTerrain)
				{
					result.origin.z = Scene.GetTerrainHeight(result.origin.AsVec2);
				}
				else if (SnapType == SnapMethod.SnapToWaterLevel)
				{
					result.origin.z = Scene.GetWaterLevel();
				}
			}
		}
		return result;
	}

	public static SpawnPathData Create(Scene scene, Path path, float pivotRatio = 0f, bool isInverted = false, SnapMethod snapType = SnapMethod.DontSnap)
	{
		return new SpawnPathData(scene, path, pivotRatio, isInverted, snapType);
	}
}
