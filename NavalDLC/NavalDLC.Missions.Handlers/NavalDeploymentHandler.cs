using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.Deployment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Handlers;

public class NavalDeploymentHandler : DeploymentHandler
{
	private NavalMissionDeploymentPlanningLogic _navalDeploymentPlan;

	private NavalShipsLogic _navalShipsLogic;

	public NavalDeploymentHandler(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		base.Mission.GetDeploymentPlan<NavalMissionDeploymentPlanningLogic>(out _navalDeploymentPlan);
	}

	public override void AfterStart()
	{
		base.AfterStart();
	}

	public override void AutoDeployTeamUsingDeploymentPlan(Team team)
	{
		_navalDeploymentPlan.RemakeDeploymentPlan(base.Mission.PlayerTeam);
		List<Formation> list = team.FormationsIncludingEmpty.ToList();
		if (list.Count > 0)
		{
			bool isTeleportingShips = _navalShipsLogic.IsTeleportingShips;
			_navalShipsLogic.SetTeleportShips(value: true);
			MBQueue<(MissionShip, Oriented2DArea)> mBQueue = new MBQueue<(MissionShip, Oriented2DArea)>();
			foreach (Formation item4 in list)
			{
				FormationClass formationIndex = item4.FormationIndex;
				ShipAssignment shipAssignment = _navalShipsLogic.GetShipAssignment(team.TeamSide, formationIndex);
				IFormationDeploymentPlan formationPlan = _navalDeploymentPlan.GetFormationPlan(team, formationIndex);
				MissionShip missionShip = shipAssignment.MissionShip;
				if (missionShip != null && formationPlan != null && formationPlan.HasFrame())
				{
					MatrixFrame frame = formationPlan.GetFrame();
					Vec2 globalCenter = frame.origin.AsVec2;
					Vec2 globalForward = frame.rotation.f.AsVec2.Normalized();
					Vec2 localDimensions = missionShip.MissionShipObject.DeploymentArea;
					Oriented2DArea item = new Oriented2DArea(in globalCenter, in globalForward, in localDimensions);
					mBQueue.Enqueue((missionShip, item));
				}
			}
			int num = 0;
			int num2 = mBQueue.Count * 5;
			while (!mBQueue.IsEmpty() && num < num2)
			{
				var (missionShip2, area) = mBQueue.Dequeue();
				if (_navalShipsLogic.IsAreaFreeOfShipCollision(in area, 1f, missionShip2.Index))
				{
					ShipOrder shipOrder = missionShip2.ShipOrder;
					Vec2 globalCenter2 = area.GlobalCenter;
					Vec2 localDimensions = area.GlobalForward;
					shipOrder.SetShipMovementOrder(globalCenter2, in localDimensions);
				}
				else
				{
					mBQueue.Enqueue((missionShip2, area));
				}
				num++;
			}
			while (!mBQueue.IsEmpty())
			{
				(MissionShip, Oriented2DArea) tuple2 = mBQueue.Dequeue();
				MissionShip item2 = tuple2.Item1;
				Oriented2DArea item3 = tuple2.Item2;
				ShipOrder shipOrder2 = item2.ShipOrder;
				Vec2 globalCenter3 = item3.GlobalCenter;
				Vec2 localDimensions = item3.GlobalForward;
				shipOrder2.SetShipMovementOrder(globalCenter3, in localDimensions);
			}
			if ((team.IsPlayerTeam ? team.PlayerOrderController : team.MasterOrderController) is NavalOrderController navalOrderController)
			{
				navalOrderController.SelectAllFormations();
				navalOrderController.SetOrder(OrderType.AIControlOff);
				navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: false);
				navalOrderController.SetOrder(OrderType.Mount);
				navalOrderController.SetOrder(OrderType.FireAtWill);
				navalOrderController.SetOrder(OrderType.StandYourGround);
				navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
				navalOrderController.ClearSelectedFormations();
				Formation formation = team.FormationsIncludingEmpty.FirstOrDefault((Formation x) => NavalDLCHelpers.IsPlayerCaptainOfFormationShip(x));
				if (formation != null)
				{
					navalOrderController.SelectFormation(formation);
					navalOrderController.SetOrder(OrderType.Mount);
					navalOrderController.SetFormationUpdateEnabledAfterSetOrder(value: true);
					navalOrderController.ClearSelectedFormations();
				}
			}
			else
			{
				Debug.FailedAssert("Team order controller is not of type naval order controller", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\MissionLogics\\NavalDeploymentHandler.cs", "AutoDeployTeamUsingDeploymentPlan", 148);
			}
			_navalShipsLogic.SetTeleportShips(isTeleportingShips);
		}
		if (team.IsPlayerTeam && _deploymentMissionController is NavalDeploymentMissionController navalDeploymentMissionController)
		{
			navalDeploymentMissionController.OnPlayerShipsUpdated();
		}
	}

	public override void ForceUpdateAllUnits()
	{
	}
}
