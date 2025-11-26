using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultTeamDeploymentPlan
{
	public const float DeployZoneMinimumWidth = 100f;

	public const float DeployZoneForwardMargin = 10f;

	public const float DeployZoneExtraWidthPerTroop = 1.5f;

	public const string DefenderSiegeDeploymentFrameEntityTag = "defender_infantry";

	public const string AttackerSiegeDeploymentFrameEntityTag = "attacker_infantry";

	public readonly Team Team;

	private readonly Mission _mission;

	private readonly DefaultDeploymentPlan _initialPlan;

	private bool _spawnWithHorses;

	private readonly List<DefaultDeploymentPlan> _reinforcementPlans;

	private DefaultDeploymentPlan _currentReinforcementPlan;

	private readonly MBList<(string id, MBList<Vec2> points)> _deploymentBoundaries = new MBList<(string, MBList<Vec2>)>();

	private MatrixFrame _deploymentFrame;

	public bool SpawnWithHorses => _spawnWithHorses;

	public MBReadOnlyList<(string id, MBList<Vec2> points)> DeploymentBoundaries => _deploymentBoundaries;

	public DefaultTeamDeploymentPlan(Mission mission, Team team)
	{
		_mission = mission;
		Team = team;
		_spawnWithHorses = false;
		_initialPlan = DefaultDeploymentPlan.CreateInitialPlan(_mission, Team);
		_deploymentBoundaries.Clear();
		_reinforcementPlans = new List<DefaultDeploymentPlan>();
		_currentReinforcementPlan = _initialPlan;
		if (_mission.HasSpawnPath)
		{
			foreach (SpawnPathData item2 in _mission.GetReinforcementPathsDataOfSide(Team.Side))
			{
				DefaultDeploymentPlan item = DefaultDeploymentPlan.CreateReinforcementPlanWithSpawnPath(_mission, Team, item2);
				_reinforcementPlans.Add(item);
			}
			_currentReinforcementPlan = _reinforcementPlans[0];
		}
		else
		{
			DefaultDeploymentPlan defaultDeploymentPlan = DefaultDeploymentPlan.CreateReinforcementPlan(_mission, Team);
			_reinforcementPlans.Add(defaultDeploymentPlan);
			_currentReinforcementPlan = defaultDeploymentPlan;
		}
	}

	public void SetSpawnWithHorses(bool value)
	{
		_spawnWithHorses = value;
		_initialPlan.SetSpawnWithHorses(value);
		foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
		{
			reinforcementPlan.SetSpawnWithHorses(value);
		}
	}

	public void MakeDeploymentPlan(FormationSceneSpawnEntry[,] formationSceneSpawnEntries, bool isReinforcement = false, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				if (!reinforcementPlan.IsPlanMade)
				{
					reinforcementPlan.PlanBattleDeployment(formationSceneSpawnEntries);
				}
			}
			return;
		}
		if (!_initialPlan.IsPlanMade)
		{
			_initialPlan.PlanBattleDeployment(formationSceneSpawnEntries, spawnPathOffset, targetOffset);
		}
		PlanDeploymentZone();
	}

	public void UpdateReinforcementPlans()
	{
		if (_reinforcementPlans.Count <= 1)
		{
			return;
		}
		foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
		{
			reinforcementPlan.UpdateSafetyScore();
		}
		if (!_currentReinforcementPlan.IsSafeToDeploy)
		{
			_currentReinforcementPlan = _reinforcementPlans.MaxBy((DefaultDeploymentPlan plan) => plan.SafetyScore);
		}
	}

	public void ClearPlan(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.ClearPlan();
			}
			return;
		}
		_initialPlan.ClearPlan();
	}

	public void ClearAddedTroops(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.ClearAddedTroops();
			}
			return;
		}
		_initialPlan.ClearAddedTroops();
	}

	public void AddTroops(FormationClass formationClass, int footTroopCount, int mountedTroopCount, bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			foreach (DefaultDeploymentPlan reinforcementPlan in _reinforcementPlans)
			{
				reinforcementPlan.AddTroops(formationClass, footTroopCount, mountedTroopCount);
			}
			return;
		}
		_initialPlan.AddTroops(formationClass, footTroopCount, mountedTroopCount);
	}

	public bool IsFirstPlan(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.PlanCount == 1;
		}
		return _initialPlan.PlanCount == 1;
	}

	public bool IsPlanMade(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.IsPlanMade;
		}
		return _initialPlan.IsPlanMade;
	}

	public float GetSpawnPathOffset(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.SpawnPathOffset;
		}
		return _initialPlan.SpawnPathOffset;
	}

	public float GetTargetOffset(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.TargetOffset;
		}
		return _initialPlan.TargetOffset;
	}

	public int GetTroopCount(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.TroopCount;
		}
		return _initialPlan.TroopCount;
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
		if (isReinforcement)
		{
			return _currentReinforcementPlan.GetFormationPlan(fClass);
		}
		return _initialPlan.GetFormationPlan(fClass);
	}

	public Vec3 GetMeanPosition(bool isReinforcement = false)
	{
		if (isReinforcement)
		{
			return _currentReinforcementPlan.MeanPosition;
		}
		return _initialPlan.MeanPosition;
	}

	public bool IsInitialPlanSuitableForFormations((int, int)[] troopDataPerFormationClass)
	{
		return _initialPlan.IsPlanSuitableForFormations(troopDataPerFormationClass);
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

	public bool GetPathDeploymentBoundaryIntersection(in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		Vec2 position = startPosition.AsVec2;
		IsPositionInsideDeploymentBoundaries(in position, out (string, MBList<Vec2>) containingBoundaryTuple);
		intersection = WorldPosition.Invalid;
		NavigationPath navigationPath = new NavigationPath();
		if (Mission.Current.Scene.GetPathBetweenAIFaces(startPosition.GetNearestNavMesh(), endPosition.GetNearestNavMesh(), startPosition.AsVec2, endPosition.AsVec2, 0f, navigationPath, null) && navigationPath.Size > 0)
		{
			Vec2 vec = startPosition.AsVec2;
			(string, MBList<Vec2>) tuple = containingBoundaryTuple;
			Vec2 vec2 = Vec2.Invalid;
			for (int i = 0; i < navigationPath.Size; i++)
			{
				Vec2 position2 = navigationPath[i];
				if (IsPositionInsideDeploymentBoundaries(in position2, out (string, MBList<Vec2>) containingBoundaryTuple2))
				{
					vec = position2;
					tuple = containingBoundaryTuple2;
					continue;
				}
				vec2 = position2;
				break;
			}
			if (vec2.IsValid)
			{
				intersection = startPosition;
				intersection.SetVec2(vec);
				Vec2 rayDir = (vec2 - vec).Normalized();
				MBMath.IntersectRayWithPolygon(vec, rayDir, tuple.Item2, out var intersectionPoint);
				intersection.SetVec2(Mission.Current.Scene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref intersection, intersectionPoint).AsVec2);
			}
			else
			{
				intersection = endPosition;
			}
		}
		else
		{
			intersection = startPosition;
		}
		return intersection.IsValid;
	}

	private void PlanDeploymentZone()
	{
		if (_mission.HasSpawnPath || _mission.IsFieldBattle)
		{
			ComputeDeploymentZone();
		}
		else if (_mission.IsSiegeBattle)
		{
			SetDeploymentZoneFromMissionBoundaries();
			_deploymentFrame = _mission.Scene.FindWeakEntityWithTag((Team.Side == BattleSideEnum.Attacker) ? "attacker_infantry" : "defender_infantry").GetGlobalFrame();
		}
		else
		{
			_deploymentBoundaries.Clear();
		}
	}

	private void ComputeDeploymentZone()
	{
		_initialPlan.GetFormationDeploymentFrame(FormationClass.Infantry, out _deploymentFrame);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < 10; i++)
		{
			FormationClass fClass = (FormationClass)i;
			DefaultFormationDeploymentPlan formationPlan = _initialPlan.GetFormationPlan(fClass);
			if (formationPlan.HasFrame())
			{
				ref MatrixFrame deploymentFrame = ref _deploymentFrame;
				MatrixFrame m = formationPlan.GetFrame();
				MatrixFrame matrixFrame = deploymentFrame.TransformToLocal(in m);
				num = Math.Max(matrixFrame.origin.y, num);
				num2 = Math.Max(Math.Abs(matrixFrame.origin.x), num2);
			}
		}
		num += 10f;
		_deploymentFrame.Advance(num);
		_deploymentBoundaries.Clear();
		float val = 2f * num2 + 1.5f * (float)_initialPlan.TroopCount;
		val = Math.Max(val, 100f);
		foreach (KeyValuePair<string, ICollection<Vec2>> boundary in _mission.Boundaries)
		{
			string key = boundary.Key;
			MBList<Vec2> item = ComputeDeploymentBoundariesFromMissionBoundaries(boundary.Value, ref _deploymentFrame, val);
			_deploymentBoundaries.Add((key, item));
		}
	}

	private void SetDeploymentZoneFromMissionBoundaries()
	{
		_deploymentBoundaries.Clear();
		foreach (var deploymentBoundary in MBSceneUtilities.GetDeploymentBoundaries(Team.Side))
		{
			MBList<Vec2> boundary = new MBList<Vec2>(deploymentBoundary.boundaryPoints);
			MBSceneUtilities.RadialSortBoundary(ref boundary);
			MBSceneUtilities.FindConvexHull(ref boundary);
			_deploymentBoundaries.Add((deploymentBoundary.tag, boundary));
		}
	}

	private static MBList<Vec2> ComputeDeploymentBoundariesFromMissionBoundaries(ICollection<Vec2> missionBoundaries, ref MatrixFrame deploymentFrame, float desiredWidth)
	{
		MBList<Vec2> boundary = new MBList<Vec2>();
		float maxLength = desiredWidth / 2f;
		if (missionBoundaries.Count > 2)
		{
			Vec2 asVec = deploymentFrame.origin.AsVec2;
			Vec2 vec = deploymentFrame.rotation.s.AsVec2.Normalized();
			Vec2 vec2 = deploymentFrame.rotation.f.AsVec2.Normalized();
			MBList<Vec2> mBList = missionBoundaries.ToMBList();
			List<(Vec2, Vec2)> list = new List<(Vec2, Vec2)>();
			Vec2 vec3 = ClampRayToMissionBoundaries(mBList, asVec, vec, maxLength);
			AddDeploymentBoundaryPoint(boundary, vec3);
			Vec2 vec4 = ClampRayToMissionBoundaries(mBList, asVec, -vec, maxLength);
			AddDeploymentBoundaryPoint(boundary, vec4);
			if (MBMath.IntersectRayWithPolygon(vec3, -vec2, mBList, out var intersectionPoint) && (intersectionPoint - vec3).Length > 0.1f)
			{
				list.Add((intersectionPoint, vec3));
				AddDeploymentBoundaryPoint(boundary, intersectionPoint);
			}
			list.Add((vec3, vec4));
			if (MBMath.IntersectRayWithPolygon(vec4, -vec2, mBList, out var intersectionPoint2) && (intersectionPoint2 - vec4).Length > 0.1f)
			{
				list.Add((vec4, intersectionPoint2));
				AddDeploymentBoundaryPoint(boundary, intersectionPoint2);
			}
			foreach (Vec2 missionBoundary in missionBoundaries)
			{
				bool flag = true;
				foreach (var item in list)
				{
					Vec2 vec5 = missionBoundary - item.Item1;
					Vec2 vec6 = item.Item2 - item.Item1;
					if (vec6.x * vec5.y - vec6.y * vec5.x <= 0f)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					AddDeploymentBoundaryPoint(boundary, missionBoundary);
				}
			}
			MBSceneUtilities.RadialSortBoundary(ref boundary);
		}
		return boundary;
	}

	private static void AddDeploymentBoundaryPoint(MBList<Vec2> deploymentBoundaries, Vec2 point)
	{
		if (!deploymentBoundaries.Exists((Vec2 boundaryPoint) => boundaryPoint.Distance(point) <= 0.1f))
		{
			deploymentBoundaries.Add(point);
		}
	}

	private static Vec2 ClampRayToMissionBoundaries(MBList<Vec2> boundaries, Vec2 origin, Vec2 direction, float maxLength)
	{
		if (Mission.Current.IsPositionInsideBoundaries(origin))
		{
			Vec2 vec = origin + direction * maxLength;
			if (Mission.Current.IsPositionInsideBoundaries(vec))
			{
				return vec;
			}
		}
		if (MBMath.IntersectRayWithPolygon(origin, direction, boundaries, out var intersectionPoint))
		{
			return intersectionPoint;
		}
		return origin;
	}
}
