using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public struct AnimResult
{
	private UIntPtr _nativePointer;

	internal static AnimResult CreateWithPointer(UIntPtr pointer)
	{
		AnimResult result = default(AnimResult);
		result._nativePointer = pointer;
		return result;
	}

	public Transformation GetEntitialOutTransform(sbyte boneIndex, Skeleton skeleton)
	{
		return skeleton.GetEntitialOutTransform(_nativePointer, boneIndex);
	}

	public void SetOutBoneDisplacement(sbyte boneIndex, Vec3 position, Skeleton skeleton)
	{
		skeleton.SetOutBoneDisplacement(_nativePointer, boneIndex, position);
	}

	public void SetOutQuat(sbyte boneIndex, Mat3 rotation, Skeleton skeleton)
	{
		skeleton.SetOutQuat(_nativePointer, boneIndex, rotation);
	}
}
