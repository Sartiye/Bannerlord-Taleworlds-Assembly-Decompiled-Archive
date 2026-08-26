using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalDeploymentPlan
{
	public const float HorizontalShipGap = 20f;

	public readonly Team Team;

	private readonly Mission _mission;

	private int _planCount;

	private bool _isRiverPlan;

	private bool _isRaidPlan;

	private Vec3 _meanPosition;

	private readonly NavalFormationDeploymentPlan[] _formationPlans;

	public bool IsRiverPlan => _isRiverPlan;

	public bool IsRaidPlan => _isRaidPlan;

	public int PlanCount => _planCount;

	public bool IsPlanMade { get; private set; }

	public float SpawnPathOffset { get; private set; }

	public float TargetOffset { get; private set; }

	public int TroopCount
	{
		get
		{
			int num = 0;
			NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
			foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
			{
				num += navalFormationDeploymentPlan.PlannedTroopCount;
			}
			return num;
		}
	}

	public int ShipCount
	{
		get
		{
			int num = 0;
			NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
			for (int i = 0; i < formationPlans.Length; i++)
			{
				if (formationPlans[i].HasShipObject)
				{
					num++;
				}
			}
			return num;
		}
	}

	public Vec3 MeanPosition => _meanPosition;

	public static NavalDeploymentPlan CreatePlan(Mission mission, Team team, bool isRiverPlan, bool isRaidPlan)
	{
		return new NavalDeploymentPlan(mission, team, isRiverPlan, isRaidPlan);
	}

	private NavalDeploymentPlan(Mission mission, Team team, bool isRiverPlan, bool isRaidPlan)
	{
		_mission = mission;
		_planCount = 0;
		Team = team;
		_formationPlans = new NavalFormationDeploymentPlan[11];
		_isRiverPlan = isRiverPlan;
		_isRaidPlan = isRaidPlan;
		IsPlanMade = false;
		SpawnPathOffset = 0f;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			FormationClass fClass = (FormationClass)i;
			_formationPlans[i] = new NavalFormationDeploymentPlan(fClass, _mission);
		}
		ClearAddedShips();
		ClearPlan();
	}

	public void MakeDeploymentPlan(float spawnPathOffset, float targetOffset, FormationSceneSpawnEntry[,] formationSceneSpawnEntries = null)
	{
		SpawnPathOffset = spawnPathOffset;
		TargetOffset = targetOffset;
		if (_mission.HasSpawnPath)
		{
			PlanNavalBattleDeploymentFromSpawnPath(spawnPathOffset, targetOffset);
		}
		else
		{
			PlanNavalBattleDeploymentFromSceneData(formationSceneSpawnEntries);
		}
		ComputeMeanPosition();
	}

	public void ClearPlan()
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		for (int i = 0; i < formationPlans.Length; i++)
		{
			formationPlans[i].Clear();
		}
		IsPlanMade = false;
	}

	public void ClearAddedShips()
	{
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		for (int i = 0; i < formationPlans.Length; i++)
		{
			formationPlans[i].SetShipOrigin(null);
		}
	}

	public void AddShip(FormationClass formationClass, IShipOrigin shipOrigin)
	{
		_formationPlans[(int)formationClass].SetShipOrigin(shipOrigin);
	}

	public bool RemoveShip(FormationClass formationIndex)
	{
		NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[(int)formationIndex];
		if (navalFormationDeploymentPlan.ShipObject != null)
		{
			navalFormationDeploymentPlan.SetShipOrigin(null);
			return true;
		}
		return false;
	}

	public NavalFormationDeploymentPlan GetFormationPlan(FormationClass fClass)
	{
		return _formationPlans[(int)fClass];
	}

	public bool GetFormationDeploymentFrame(FormationClass fClass, out MatrixFrame frame)
	{
		NavalFormationDeploymentPlan formationPlan = GetFormationPlan(fClass);
		if (formationPlan.HasFrame())
		{
			frame = formationPlan.GetFrame();
			return true;
		}
		frame = MatrixFrame.Identity;
		return false;
	}

	private void PlanNavalBattleDeploymentFromSpawnPath(float pathOffset, float targetOffset)
	{
		_mission.GetInitialSpawnPathData(Team.Side).GetSpawnPathFrameFacingTarget(pathOffset, targetOffset, _isRiverPlan, out var spawnPathPosition, out var spawnPathDirection);
		DeployShips(spawnPathPosition, spawnPathDirection);
		IsPlanMade = true;
		_planCount++;
	}

	private void PlanNavalBattleDeploymentFromSceneData(FormationSceneSpawnEntry[,] formationSceneSpawnEntries)
	{
		if (formationSceneSpawnEntries == null || formationSceneSpawnEntries.GetLength(0) != 2 || formationSceneSpawnEntries.GetLength(1) != _formationPlans.Length)
		{
			return;
		}
		int side = (int)Team.Side;
		for (int i = 0; i < _formationPlans.Length; i++)
		{
			NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[i];
			if (navalFormationDeploymentPlan.HasShipObject)
			{
				MatrixFrame globalFrame = formationSceneSpawnEntries[side, i].SpawnEntity.GetGlobalFrame();
				Vec2 deployPosition = globalFrame.origin.AsVec2;
				Vec2 deployDirection = globalFrame.rotation.f.AsVec2.Normalized();
				navalFormationDeploymentPlan.SetFrame(in deployPosition, in deployDirection);
			}
		}
		IsPlanMade = true;
		_planCount++;
	}

	private void DeployShips(Vec2 deployPosition, Vec2 deployDirection)
	{
		List<(int, NavalFormationDeploymentPlan)> list = new List<(int, NavalFormationDeploymentPlan)>();
		for (int i = 0; i < _formationPlans.Count(); i++)
		{
			NavalFormationDeploymentPlan navalFormationDeploymentPlan = _formationPlans[i];
			if (navalFormationDeploymentPlan.HasShipObject)
			{
				int totalCrewCapacity = navalFormationDeploymentPlan.ShipOrigin.TotalCrewCapacity;
				list.Add((totalCrewCapacity, navalFormationDeploymentPlan));
			}
		}
		list.Sort(((int crewCapacity, NavalFormationDeploymentPlan plan) x, (int crewCapacity, NavalFormationDeploymentPlan plan) y) => y.crewCapacity.CompareTo(x.crewCapacity));
		float num = 0f;
		float num2 = 0f;
		Vec2 vec = deployDirection.LeftVec().Normalized();
		Vec2 vec2 = -vec;
		int j = 0;
		if (list.Count % 2 != 0)
		{
			NavalFormationDeploymentPlan item = list[j].Item2;
			item.SetFrame(in deployPosition, in deployDirection);
			float num3 = item.ShipObject.DeploymentArea.x / 2f;
			num += num3;
			num2 += num3;
			j++;
		}
		for (; j < list.Count; j++)
		{
			NavalFormationDeploymentPlan item2 = list[j].Item2;
			float num4 = item2.ShipObject.DeploymentArea.x / 2f;
			if (j % 2 == 0)
			{
				num2 += 20f + num4;
				Vec2 deployPosition2 = deployPosition + vec2 * num2;
				item2.SetFrame(in deployPosition2, in deployDirection);
				num2 += num4;
			}
			else
			{
				num += 20f + num4;
				Vec2 deployPosition3 = deployPosition + vec * num;
				item2.SetFrame(in deployPosition3, in deployDirection);
				num += num4;
			}
		}
		list.Clear();
	}

	private void ComputeMeanPosition()
	{
		_meanPosition = Vec3.Zero;
		Vec2 zero = Vec2.Zero;
		int num = 0;
		NavalFormationDeploymentPlan[] formationPlans = _formationPlans;
		foreach (NavalFormationDeploymentPlan navalFormationDeploymentPlan in formationPlans)
		{
			if (navalFormationDeploymentPlan.HasFrame())
			{
				zero += navalFormationDeploymentPlan.GetPosition().AsVec2;
				num++;
			}
		}
		if (num > 0)
		{
			_meanPosition = new Vec2(zero.X / (float)num, zero.Y / (float)num).ToVec3();
		}
	}
}
