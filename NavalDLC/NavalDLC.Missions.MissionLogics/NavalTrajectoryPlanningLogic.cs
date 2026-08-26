using System.Collections.Generic;
using System.Linq;
using NavalDLC.DWA;
using NavalDLC.Missions.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalTrajectoryPlanningLogic : MissionLogic
{
	public const string StaticObstacleTag = "naval_static_obstacle";

	private NavalShipsLogic _navalShipsLogic;

	private DWASimulator _simulator;

	private DWASimulatorParameters _simulatorParameters;

	public override void OnBehaviorInitialize()
	{
		_simulator = new DWASimulator();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipSpawnedEvent += OnShipSpawned;
		_navalShipsLogic.ShipRemovedEvent += OnShipRemoved;
		_simulatorParameters = DWASimulatorParameters.Create();
	}

	public override void OnDeploymentFinished()
	{
		Initialize();
	}

	public override void OnMissionStateFinalized()
	{
		_navalShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
		_navalShipsLogic.ShipRemovedEvent -= OnShipRemoved;
		if (_simulator.IsInitialized)
		{
			_simulator.Clear();
		}
		_simulator = null;
	}

	public override void OnMissionTick(float dt)
	{
		if (base.Mission.IsDeploymentFinished)
		{
			_simulator.Tick(dt);
		}
	}

	public void ForceReinitialize()
	{
		Initialize();
	}

	public void OnShipSpawned(MissionShip ship)
	{
		if (base.Mission.IsDeploymentFinished)
		{
			AddShipAux(ship);
		}
	}

	public void OnShipRemoved(MissionShip ship)
	{
		if (base.Mission.IsDeploymentFinished)
		{
			RemoveShipAux(ship);
		}
	}

	private void Initialize()
	{
		_simulator.SetParameters(in _simulatorParameters);
		if (_simulator.IsInitialized)
		{
			_simulator.Clear();
		}
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			AddShipAux(allShip);
		}
		List<GameEntity> staticObstacles = base.Mission.Scene.FindEntitiesWithTag("naval_static_obstacle").ToList();
		AddStaticObstacles(staticObstacles);
		_simulator.Initialize();
	}

	private void AddStaticObstacles(IReadOnlyList<GameEntity> staticObstacles)
	{
		if (staticObstacles.Count == 0)
		{
			return;
		}
		MBList<Vec3> mBList = new MBList<Vec3>
		{
			Vec3.Zero,
			Vec3.Zero,
			Vec3.Zero,
			Vec3.Zero
		};
		MatrixFrame[] array = null;
		foreach (GameEntity staticObstacle in staticObstacles)
		{
			Path pathWithName = Mission.Current.Scene.GetPathWithName(staticObstacle.Name);
			if (pathWithName != null)
			{
				int numberOfPoints = pathWithName.NumberOfPoints;
				if (array == null || array.Length < numberOfPoints)
				{
					array = new MatrixFrame[numberOfPoints];
				}
				pathWithName.GetPoints(array);
				Vec2 nextDir2 = (array[1].origin - array[0].origin).AsVec2;
				if (nextDir2.Normalize() < 1E-05f)
				{
					nextDir2 = Vec2.Zero;
				}
				Vec2 vec = ComputeOffset(in Vec2.Zero, hasPrev: false, in nextDir2, hasNext: true, 1);
				Vec2 vec2 = ComputeOffset(in Vec2.Zero, hasPrev: false, in nextDir2, hasNext: true, -1);
				for (int i = 0; i < numberOfPoints - 1; i++)
				{
					Vec3 origin = array[i].origin;
					Vec3 origin2 = array[i + 1].origin;
					Vec2 nextDir3 = Vec2.Zero;
					if (i + 2 < numberOfPoints)
					{
						nextDir3 = (array[i + 2].origin - array[i + 1].origin).AsVec2;
						if (nextDir3.Normalize() < 1E-05f)
						{
							nextDir3 = Vec2.Zero;
						}
					}
					bool hasPrev2 = nextDir2.LengthSquared > 1E-05f;
					bool hasNext2 = nextDir3.LengthSquared > 1E-05f;
					Vec2 vec3 = ComputeOffset(in nextDir2, hasPrev2, in nextDir3, hasNext2, 1);
					Vec2 vec4 = ComputeOffset(in nextDir2, hasPrev2, in nextDir3, hasNext2, -1);
					Vec3 value = origin + vec.ToVec3();
					Vec3 value2 = origin + vec2.ToVec3();
					Vec3 value3 = origin2 + vec3.ToVec3();
					Vec3 value4 = origin2 + vec4.ToVec3();
					mBList[0] = value2;
					mBList[1] = value;
					mBList[2] = value3;
					mBList[3] = value4;
					_simulator.AddObstacle(mBList);
					vec = vec3;
					vec2 = vec4;
					nextDir2 = nextDir3;
				}
				continue;
			}
			IOrderedEnumerable<GameEntity> orderedEnumerable = from entity in staticObstacle.GetChildren()
				orderby entity.Name
				select entity;
			MBList<Vec3> boundary = new MBList<Vec3>();
			foreach (GameEntity item in orderedEnumerable)
			{
				Vec3 origin3 = item.GetGlobalFrame().origin;
				boundary.Add(origin3);
			}
			MBSceneUtilities.RadialSortBoundary(ref boundary);
			_simulator.AddObstacle(boundary);
		}
		static Vec2 ComputeOffset(in Vec2 prevDir, bool hasPrev, in Vec2 nextDir, bool hasNext, int sideSign)
		{
			if (!hasPrev && !hasNext)
			{
				return Vec2.Zero;
			}
			if (hasPrev && !hasNext)
			{
				Vec2 vec5 = prevDir.RightVec() * sideSign;
				if (vec5.LengthSquared > 1E-05f)
				{
					vec5 = vec5.Normalized();
				}
				return vec5 * 8f;
			}
			if (!hasPrev && hasNext)
			{
				Vec2 vec6 = nextDir.RightVec() * sideSign;
				if (vec6.LengthSquared > 1E-05f)
				{
					vec6 = vec6.Normalized();
				}
				return vec6 * 8f;
			}
			Vec2 vec7 = prevDir.RightVec() * sideSign;
			Vec2 vec8 = nextDir.RightVec() * sideSign;
			bool flag = vec7.LengthSquared > 1E-05f;
			bool flag2 = vec8.LengthSquared > 1E-05f;
			if (!flag && !flag2)
			{
				return Vec2.Zero;
			}
			if (!flag)
			{
				vec8 = vec8.Normalized();
				return vec8 * 8f;
			}
			if (!flag2)
			{
				vec7 = vec7.Normalized();
				return vec7 * 8f;
			}
			Vec2 vec9 = vec7 + vec8;
			float lengthSquared = vec9.LengthSquared;
			if (lengthSquared <= 1E-05f)
			{
				return vec8.Normalized() * 8f;
			}
			vec9 /= MathF.Sqrt(lengthSquared);
			Vec2 vec10 = vec8.Normalized();
			float num = MathF.Abs(Vec2.DotProduct(vec9, vec10));
			if (num <= 1E-05f)
			{
				return vec10 * 8f;
			}
			float num2 = 8f / num;
			float num3 = 32f;
			if (num2 > num3)
			{
				num2 = num3;
			}
			else if (num2 < 0f - num3)
			{
				num2 = 0f - num3;
			}
			return vec9 * num2;
		}
	}

	private void AddShipAux(MissionShip ship)
	{
		IDWAAgentDelegate agentDelegate = ship.CreateDWAAgent(in _simulator.Parameters);
		_simulator.AddAgent(agentDelegate);
	}

	private void RemoveShipAux(MissionShip ship)
	{
		_simulator.RemoveAgent(ship.DWAAgentId);
	}
}
