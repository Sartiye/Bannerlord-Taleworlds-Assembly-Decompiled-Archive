using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

public class StealthBox : ScriptComponentBehavior
{
	public static event Action<StealthBox> OnBoxInitialized;

	public static event Action<StealthBox> OnBoxRemoved;

	protected internal override void OnInit()
	{
		base.OnInit();
		MetaMesh metaMesh = base.GameEntity.GetMetaMesh(0);
		base.GameEntity.RemoveMultiMesh(metaMesh);
		StealthBox.OnBoxInitialized?.Invoke(this);
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		StealthBox.OnBoxRemoved?.Invoke(this);
	}

	public bool IsPointInside(Vec3 point)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 scaleVector = globalFrame.rotation.GetScaleVector();
		if (globalFrame.origin.DistanceSquared(point) > scaleVector.LengthSquared)
		{
			return false;
		}
		ref Mat3 rotation = ref globalFrame.rotation;
		Vec3 scaleAmountXYZ = new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z);
		rotation.ApplyScaleLocal(in scaleAmountXYZ);
		point = globalFrame.TransformToLocal(in point);
		if (TaleWorlds.Library.MathF.Abs(point.x) <= scaleVector.x / 2f && TaleWorlds.Library.MathF.Abs(point.y) <= scaleVector.y / 2f)
		{
			return TaleWorlds.Library.MathF.Abs(point.z) <= scaleVector.z / 2f;
		}
		return false;
	}

	public bool IsAgentInside(Agent agent)
	{
		return IsPointInside(agent.Position);
	}
}
