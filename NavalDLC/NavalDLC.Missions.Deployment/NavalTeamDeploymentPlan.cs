using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalTeamDeploymentPlan : ITeamDeploymentPlan
{
	public const float DeployZoneMinimumWidth = 400f;

	public const float RiverSceneDeployZoneFixedWidth = 200f;

	public const float DeployZoneForwardMargin = 50f;

	public const float DeployZoneBackwardMargin = 100f;

	private Mission _mission;

	private readonly NavalDeploymentPlan _initialPlan;

	private readonly MBList<(string id, MBList<Vec2> points)> _deploymentBoundaries = new MBList<(string, MBList<Vec2>)>();

	private MatrixFrame _deploymentFrame;

	private float _deploymentWidth;

	private float _deploymentDepth;

	private MBList<Vec2> _meanBoundaryPositions;

	public Team Team { get; private set; }

	internal NavalTeamDeploymentPlan(Mission mission, Team team)
	{
		_mission = mission;
		Team = team;
		_deploymentFrame = MatrixFrame.Identity;
		_deploymentWidth = 0f;
		_deploymentDepth = 0f;
		_meanBoundaryPositions = new MBList<Vec2>();
		bool isRiverPlan = mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.NavalBattle && _mission.Scene.GetNavmeshFaceCountBetweenTwoIds(1, 1) > 0;
		bool isRaidPlan = mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.NavalRaid;
		_initialPlan = NavalDeploymentPlan.CreatePlan(_mission, team, isRiverPlan, isRaidPlan);
		_deploymentBoundaries.Clear();
	}

	public void MakeDeploymentPlan(float spawnPathOffset, float targetOffset = 0f, FormationSceneSpawnEntry[,] formationSpawnEntries = null, bool isReinforcement = false)
	{
		_initialPlan.MakeDeploymentPlan(spawnPathOffset, targetOffset, formationSpawnEntries);
		PlanDeploymentZone();
	}

	public void ClearPlan(bool isReinforcement = false)
	{
		_initialPlan.ClearPlan();
		_meanBoundaryPositions.Clear();
	}

	public void ClearAddedShips()
	{
		_initialPlan.ClearAddedShips();
	}

	internal void AddShip(FormationClass formationClass, IShipOrigin shipOrigin)
	{
		_initialPlan.AddShip(formationClass, shipOrigin);
	}

	internal bool RemoveShip(FormationClass formationIndex)
	{
		return _initialPlan.RemoveShip(formationIndex);
	}

	public int GetShipCount()
	{
		return _initialPlan.ShipCount;
	}

	public bool IsFirstPlan(bool isReinforcement = false)
	{
		return _initialPlan.PlanCount == 1;
	}

	public bool IsPlanMade(bool isReinforcement = false)
	{
		return _initialPlan.IsPlanMade;
	}

	public MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries()
	{
		return _deploymentBoundaries;
	}

	public float GetSpawnPathOffset(bool isReinforcement = false)
	{
		return _initialPlan.SpawnPathOffset;
	}

	public float GetTargetOffset(bool isReinforcement = false)
	{
		return _initialPlan.TargetOffset;
	}

	public MatrixFrame GetDeploymentFrame()
	{
		return _deploymentFrame;
	}

	public bool HasDeploymentBoundaries()
	{
		return !_deploymentBoundaries.IsEmpty();
	}

	public IFormationDeploymentPlan GetFormationPlan(FormationClass fClass, bool isReinforcement = false)
	{
		return _initialPlan.GetFormationPlan(fClass);
	}

	public Vec3 GetMeanPosition(bool isReinforcement = false)
	{
		return _initialPlan.MeanPosition;
	}

	public bool IsPositionInsideDeploymentBoundaries(in Vec2 position, out (string id, MBList<Vec2> points) containingBoundaryTuple)
	{
		bool result = false;
		containingBoundaryTuple = (id: "", points: null);
		foreach (var deploymentBoundary in _deploymentBoundaries)
		{
			MBList<Vec2> item = deploymentBoundary.points;
			if (MBSceneUtilities.IsPointInsideBoundaries(in position, item))
			{
				containingBoundaryTuple = deploymentBoundary;
				result = true;
				break;
			}
		}
		return result;
	}

	public Vec2 GetClosestDeploymentBoundaryPosition(in Vec2 position)
	{
		Vec2 result = position;
		float num = float.MaxValue;
		foreach (var deploymentBoundary in _deploymentBoundaries)
		{
			MBList<Vec2> item = deploymentBoundary.points;
			if (item.Count > 2)
			{
				Vec2 closestPoint;
				float num2 = MBSceneUtilities.FindClosestPointToBoundaries(in position, item, out closestPoint);
				if (num2 < num)
				{
					num = num2;
					result = closestPoint;
				}
			}
		}
		return result;
	}

	public Vec2 GetMeanBoundaryPosition(int boundaryIndex = 0)
	{
		return _meanBoundaryPositions[boundaryIndex];
	}

	private void PlanDeploymentZone()
	{
		Vec3 o = Vec3.Zero;
		Vec2 zero = Vec2.Zero;
		int num = 0;
		for (int i = 0; i < 10; i++)
		{
			FormationClass fClass = (FormationClass)i;
			NavalFormationDeploymentPlan formationPlan = _initialPlan.GetFormationPlan(fClass);
			if (formationPlan.HasFrame())
			{
				o += formationPlan.GetPosition();
				zero += formationPlan.GetDirection();
				num++;
			}
		}
		o /= (float)num;
		Vec3 direction = zero.ToVec3().NormalizedCopy();
		Mat3 rot = Mat3.CreateMat3WithForward(in direction);
		_deploymentFrame = new MatrixFrame(in rot, in o);
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		for (int j = 0; j < 10; j++)
		{
			FormationClass fClass2 = (FormationClass)j;
			IFormationDeploymentPlan formationPlan2 = GetFormationPlan(fClass2);
			float num6 = formationPlan2.PlannedDepth / 2f;
			float num7 = formationPlan2.PlannedWidth / 2f;
			if (formationPlan2.HasFrame())
			{
				ref MatrixFrame deploymentFrame = ref _deploymentFrame;
				MatrixFrame m = formationPlan2.GetFrame();
				MatrixFrame matrixFrame = deploymentFrame.TransformToLocal(in m);
				num2 = Math.Max(matrixFrame.origin.y + num6, num2);
				num3 = Math.Min(matrixFrame.origin.y - num6, num3);
				num4 = Math.Max(matrixFrame.origin.x + num7, num4);
				num5 = Math.Min(matrixFrame.origin.x - num7, num5);
			}
		}
		float val = num4 + TaleWorlds.Library.MathF.Abs(num5);
		float b = num2 + TaleWorlds.Library.MathF.Abs(num3);
		_deploymentFrame.Advance(num2 + 50f);
		_deploymentBoundaries.Clear();
		_meanBoundaryPositions.Clear();
		if (_initialPlan.IsRiverPlan)
		{
			_deploymentWidth = 200f;
		}
		else
		{
			_deploymentWidth = Math.Max(val, 400f);
		}
		_deploymentDepth = 50f + TaleWorlds.Library.MathF.Max(100f, b);
		foreach (KeyValuePair<string, ICollection<Vec2>> boundary in _mission.Boundaries)
		{
			string key = boundary.Key;
			ICollection<Vec2> value = boundary.Value;
			MBList<Vec2> mBList = ComputeDeploymentBoundariesFromMissionBoundaries(value);
			_deploymentBoundaries.Add((key, mBList));
			Vec2 item = new Vec2(mBList.Average((Vec2 v) => v.x), mBList.Average((Vec2 v) => v.y));
			_meanBoundaryPositions.Add(item);
		}
		_deploymentFrame.origin.z = _mission.Scene.GetWaterLevelAtPosition(_deploymentFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
	}

	private MBList<Vec2> ComputeDeploymentBoundariesFromMissionBoundaries(ICollection<Vec2> missionBoundaries)
	{
		MBList<Vec2> boundary = new MBList<Vec2>();
		if (missionBoundaries.Count > 2)
		{
			Vec2 asVec = _deploymentFrame.origin.AsVec2;
			Vec2 vec = _deploymentFrame.rotation.s.AsVec2.Normalized();
			Vec2 vec2 = _deploymentFrame.rotation.f.AsVec2.Normalized();
			Vec2 vec3 = asVec - _deploymentDepth / 2f * vec2;
			MBList<Vec2> mBList = new MBList<Vec2>();
			Vec2 vec4 = asVec - _deploymentWidth / 2f * vec;
			mBList.Add(vec4);
			Vec2 vec5 = vec4 - vec2 * _deploymentDepth;
			mBList.Add(vec5);
			Vec2 vec6 = vec5 + vec * _deploymentWidth;
			mBList.Add(vec6);
			Vec2 item = vec6 + vec2 * _deploymentDepth;
			mBList.Add(item);
			MBList<Vec2> mBList2 = missionBoundaries.ToMBList();
			foreach (Vec2 item2 in mBList)
			{
				Vec2 point = item2;
				if (MBSceneUtilities.IsPointInsideBoundaries(in point, mBList2))
				{
					AddDeploymentBoundaryPoint(boundary, point);
					continue;
				}
				Vec2 va = (vec3 - point).Normalized();
				if (MBMath.IntersectRayWithPolygon(rayDir: (Vec2.DotProduct(va, vec) >= 0f) ? vec : (-vec), rayOrigin: point, polygon: mBList2, intersectionPoint: out var intersectionPoint))
				{
					AddDeploymentBoundaryPoint(boundary, intersectionPoint);
				}
				Vec2 rayDir2 = ((Vec2.DotProduct(va, vec2) >= 0f) ? vec2 : (-vec2));
				if (MBMath.IntersectRayWithPolygon(point, rayDir2, mBList2, out var intersectionPoint2))
				{
					AddDeploymentBoundaryPoint(boundary, intersectionPoint2);
				}
			}
			foreach (Vec2 item3 in mBList2)
			{
				Vec2 point2 = item3;
				if (MBSceneUtilities.IsPointInsideBoundaries(in point2, mBList))
				{
					AddDeploymentBoundaryPoint(boundary, point2);
				}
			}
			MBSceneUtilities.RadialSortBoundary(ref boundary);
			MBSceneUtilities.FindConvexHull(ref boundary);
		}
		return boundary;
	}

	private void AddDeploymentBoundaryPoint(MBList<Vec2> deploymentBoundaries, Vec2 point)
	{
		if (!deploymentBoundaries.Exists((Vec2 boundaryPoint) => boundaryPoint.Distance(point) <= 0.1f))
		{
			deploymentBoundaries.Add(point);
		}
	}

	bool ITeamDeploymentPlan.IsPositionInsideDeploymentBoundaries(in Vec2 position, out (string id, MBList<Vec2> points) containingBoundaryTuple)
	{
		return IsPositionInsideDeploymentBoundaries(in position, out containingBoundaryTuple);
	}

	Vec2 ITeamDeploymentPlan.GetClosestDeploymentBoundaryPosition(in Vec2 position)
	{
		return GetClosestDeploymentBoundaryPosition(in position);
	}
}
