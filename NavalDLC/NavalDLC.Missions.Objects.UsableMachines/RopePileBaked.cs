using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class RopePileBaked : ScriptComponentBehavior
{
	public const float HookLength = 0.5f;

	private const int NumberOfPoints = 64;

	private const int PaddedNumberOfPoints = 72;

	private const int NumberOfDataPerFrame = 12;

	private Mesh _ropeMesh;

	private BoundingBox _localUpdatedBoundingBox;

	private BoundingBox _ropePileBaseBoundingBox;

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		_ropeMesh = base.GameEntity.GetFirstMesh();
	}

	protected override void OnInit()
	{
		base.OnInit();
		_ropeMesh = base.GameEntity.GetFirstMesh();
		_ropeMesh.SetupAdditionalBoneBuffer(7);
		_ropePileBaseBoundingBox = base.GameEntity.GetLocalBoundingBox();
		_localUpdatedBoundingBox = _ropePileBaseBoundingBox;
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
	}

	protected override void OnBoundingBoxValidate()
	{
		BoundingBox boundingBox = default(BoundingBox);
		boundingBox.BeginRelaxation();
		if (base.GameEntity.ChildCount > 0)
		{
			boundingBox = base.GameEntity.ComputeBoundingBoxIncludeChildren();
		}
		boundingBox.RelaxWithBoundingBox(_localUpdatedBoundingBox);
		boundingBox.RecomputeRadius();
		base.GameEntity.RelaxLocalBoundingBox(in boundingBox);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.None;
	}

	public MatrixFrame UpdateRopeMeshVisualAccordingToTargetPoint(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, in Vec3 globalVelocity, float time)
	{
		return ComputeFreeFallPoints(in sourceGlobalPosition, in targetGlobalPosition, in globalVelocity, time);
	}

	public Vec3 UpdateRopeMeshVisualAccordingToTargetPointLinear(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		return ComputeFreeFallPointsLinear(in sourceGlobalPosition, in targetGlobalPosition);
	}

	public Vec3 UpdateRopeMeshVisualAccordingToTargetPointLinearWithoutBoundingBoxUpdate(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		return ComputeFreeFallPointsLinearWithoutBoundingBoxUpdate(in sourceGlobalPosition, in targetGlobalPosition);
	}

	private Vec3 GetPositionAtProjectileCurveProgress(in Vec3 globalVelocity, in Vec3 sourceGlobalPosition, float time, int progressInterval)
	{
		if (progressInterval < 64)
		{
			time *= (float)progressInterval / 63f;
			return sourceGlobalPosition + globalVelocity * time + 0.5f * MBGlobals.GravitationalAcceleration * time * time;
		}
		return Vec3.Zero;
	}

	private Vec3 ComputeFreeFallPointsLinearWithoutBoundingBoxUpdate(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		Vec3 v = targetGlobalPosition - (targetGlobalPosition - sourceGlobalPosition).NormalizedCopy() * 0.5f;
		Vec3 s = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in sourceGlobalPosition);
		Vec3 u = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in v);
		Vec3 f = new Vec3(2f);
		Mat3 rot = new Mat3(in s, in f, in u);
		MatrixFrame frame = new MatrixFrame(in rot, in u);
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		f = new Vec3(s.z, u.z, 0f, 1f);
		Vec3 f2 = new Vec3(0f, 0f, 0f, 1f);
		Vec3 u2 = new Vec3(0f, 0f, 0f, 1f);
		rot = new Mat3(in f, in f2, in u2);
		Vec3 o = new Vec3(0f, 0f, 0f, 1f);
		frame = new MatrixFrame(in rot, in o);
		_ropeMesh.SetAdditionalBoneFrame(1, in frame);
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		vectorArgument.z = 1f - TaleWorlds.Library.MathF.Max((vectorArgument.x - sourceGlobalPosition.Distance(v)) / vectorArgument.x, 0f);
		_ropeMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
		return v;
	}

	private Vec3 ComputeFreeFallPointsLinear(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 v = targetGlobalPosition - (targetGlobalPosition - sourceGlobalPosition).NormalizedCopy() * 0.5f;
		Vec3 s = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in sourceGlobalPosition);
		Vec3 u = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in v);
		Vec3 f = new Vec3(2f);
		Mat3 rot = new Mat3(in s, in f, in u);
		MatrixFrame frame = new MatrixFrame(in rot, in u);
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		f = globalFrame.TransformToLocal(in v);
		BoundingBox candidateLocalBoundingBox = new BoundingBox(in f);
		f = globalFrame.TransformToLocal(in sourceGlobalPosition);
		candidateLocalBoundingBox.RelaxMinMaxWithPointAndRadius(in f, 1f);
		f = new Vec3(s.z, u.z, 0f, 1f);
		Vec3 f2 = new Vec3(0f, 0f, 0f, 1f);
		Vec3 u2 = new Vec3(0f, 0f, 0f, 1f);
		rot = new Mat3(in f, in f2, in u2);
		Vec3 o = new Vec3(0f, 0f, 0f, 1f);
		frame = new MatrixFrame(in rot, in o);
		_ropeMesh.SetAdditionalBoneFrame(1, in frame);
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		vectorArgument.z = 1f - TaleWorlds.Library.MathF.Max((vectorArgument.x - sourceGlobalPosition.Distance(v)) / vectorArgument.x, 0f);
		_ropeMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
		UpdateRopeLocalBoundingBox(in candidateLocalBoundingBox);
		return v;
	}

	private void UpdateRopeLocalBoundingBox(in BoundingBox candidateLocalBoundingBox)
	{
		BoundingBox boundingBox = base.GameEntity.GetLocalBoundingBox();
		if (BoundingBox.ArrangeWithAnotherBoundingBox(ref boundingBox, candidateLocalBoundingBox, 10f))
		{
			_localUpdatedBoundingBox = boundingBox;
			base.GameEntity.SetBoundingboxDirty();
			base.GameEntity.Root.GetFirstScriptOfType<MissionShip>()?.InvalidateLocalBoundingBoxCache();
		}
	}

	public void SetRopeBoundingBoxToInitialState()
	{
		base.GameEntity.SetManualLocalBoundingBox(in _ropePileBaseBoundingBox);
		WeakGameEntity parent = base.GameEntity.Parent;
		if (parent.IsValid)
		{
			parent.SetBoundingboxDirty();
		}
	}

	private MatrixFrame ComputeFreeFallPoints(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, in Vec3 globalVelocity, float time)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		MatrixFrame identity = MatrixFrame.Identity;
		Vec3 vec = globalVelocity + MBGlobals.GravitationalAcceleration * time;
		time -= 0.5f / vec.Length;
		identity.origin = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, 63);
		identity.rotation.f = vec.NormalizedCopy();
		identity.rotation.s = Vec3.CrossProduct(identity.rotation.f, identity.rotation.u).NormalizedCopy();
		identity.rotation.u = Vec3.CrossProduct(identity.rotation.s, identity.rotation.f);
		identity.rotation.RotateAboutSide(-System.MathF.PI / 2f);
		Vec3 s = globalFrame.TransformToLocalNonOrthogonal(in sourceGlobalPosition);
		Vec3 u = globalFrame.TransformToLocalNonOrthogonal(in identity.origin);
		Vec3 f = new Vec3(64f);
		Mat3 rot = new Mat3(in s, in f, in u);
		MatrixFrame frame = new MatrixFrame(in rot, in u);
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		f = globalFrame.TransformToLocal(in identity.origin);
		BoundingBox candidateLocalBoundingBox = new BoundingBox(in f);
		f = globalFrame.TransformToLocal(in sourceGlobalPosition);
		candidateLocalBoundingBox.RelaxMinMaxWithPointAndRadius(in f, 1f);
		for (int i = 0; i < 72; i += 12)
		{
			Vec3 v = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i);
			Vec3 point = globalFrame.TransformToLocal(in v);
			if (i < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point);
			}
			Vec3 v2 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 1);
			Vec3 point2 = globalFrame.TransformToLocal(in v2);
			if (i + 1 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point2);
			}
			Vec3 v3 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 2);
			Vec3 v4 = globalFrame.TransformToLocal(in v3);
			if (i + 2 < 64)
			{
				f = globalFrame.TransformToLocal(in v4);
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in f);
			}
			Vec3 v5 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 3);
			Vec3 point3 = globalFrame.TransformToLocal(in v5);
			if (i + 3 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point3);
			}
			Vec3 v6 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 4);
			Vec3 point4 = globalFrame.TransformToLocal(in v6);
			if (i + 4 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point4);
			}
			Vec3 v7 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 5);
			Vec3 point5 = globalFrame.TransformToLocal(in v7);
			if (i + 5 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point5);
			}
			Vec3 v8 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 6);
			Vec3 point6 = globalFrame.TransformToLocal(in v8);
			if (i + 6 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point6);
			}
			Vec3 v9 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 7);
			Vec3 point7 = globalFrame.TransformToLocal(in v9);
			if (i + 7 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point7);
			}
			Vec3 v10 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 8);
			Vec3 point8 = globalFrame.TransformToLocal(in v10);
			if (i + 8 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point8);
			}
			Vec3 v11 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 9);
			Vec3 point9 = globalFrame.TransformToLocal(in v11);
			if (i + 9 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point9);
			}
			Vec3 v12 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 10);
			Vec3 point10 = globalFrame.TransformToLocal(in v12);
			if (i + 10 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point10);
			}
			Vec3 v13 = GetPositionAtProjectileCurveProgress(in globalVelocity, in sourceGlobalPosition, time, i + 11);
			Vec3 point11 = globalFrame.TransformToLocal(in v13);
			if (i + 11 < 64)
			{
				candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point11);
			}
			f = new Vec3(point.z, point2.z, v4.z, 1f);
			Vec3 f2 = new Vec3(point3.z, point4.z, point5.z, 1f);
			Vec3 u2 = new Vec3(point6.z, point7.z, point8.z, 1f);
			rot = new Mat3(in f, in f2, in u2);
			Vec3 o = new Vec3(point9.z, point10.z, point11.z, 1f);
			MatrixFrame frame2 = new MatrixFrame(in rot, in o);
			_ropeMesh.SetAdditionalBoneFrame(i / 12 + 1, in frame2);
		}
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		vectorArgument.z = 1f - TaleWorlds.Library.MathF.Max((vectorArgument.x - sourceGlobalPosition.Distance(identity.origin)) / vectorArgument.x, 0f);
		_ropeMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
		UpdateRopeLocalBoundingBox(in candidateLocalBoundingBox);
		return identity;
	}
}
