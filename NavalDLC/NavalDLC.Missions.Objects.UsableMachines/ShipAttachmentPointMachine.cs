using System.Collections.Generic;
using NavalDLC.Missions.AI.UsableMachineAIs;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipAttachmentPointMachine : UsableMachine
{
	[EditableScriptComponentVariable(true, "")]
	public int RelatedShipNavmeshOffset;

	private GameEntity _focusObject;

	private MBList<GameEntity> _rampPhysicsList;

	private ActionIndexCache _actionForJumpingOff = ActionIndexCache.act_escape_jump;

	public MissionShip OwnerShip { get; private set; }

	public ShipAttachmentMachine.ShipAttachment CurrentAttachment { get; private set; }

	public Vec3 HookAttachLocalPosition { get; private set; }

	public GameEntity ConnectionClipPlaneEntity { get; private set; }

	public GameEntity RampBarrier { get; private set; }

	internal MBReadOnlyList<GameEntity> RampPhysicsList => _rampPhysicsList;

	public GameEntity RampVisualEntity { get; private set; }

	public ShipAttachmentMachine LinkedAttachmentMachine { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		WeakGameEntity parent = base.GameEntity.Parent;
		while (OwnerShip == null && parent.IsValid)
		{
			OwnerShip = parent.GetFirstScriptOfType<MissionShip>();
			parent = parent.Parent;
		}
		if (base.GameEntity.Parent.GetScriptCountOfTypeRecursive<ShipAttachmentMachine>() == 1)
		{
			LinkedAttachmentMachine = base.GameEntity.Parent.GetFirstScriptOfTypeRecursive<ShipAttachmentMachine>();
		}
		int childCount = base.GameEntity.ChildCount;
		WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
		for (int i = 0; i < childCount; i++)
		{
			WeakGameEntity child = base.GameEntity.GetChild(i);
			if (child.Name == "hook_attach_point")
			{
				HookAttachLocalPosition = child.GetFrame().origin + 0.5f * child.GetFrame().rotation.u.NormalizedCopy();
				weakGameEntity = child;
			}
			else if (child.Name == "focus_object")
			{
				_focusObject = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
		}
		if (weakGameEntity != WeakGameEntity.Invalid)
		{
			weakGameEntity.Remove(78);
		}
		ConnectionClipPlaneEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTagRecursive("connection_point"));
		RampBarrier = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("connection_barrier"));
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenWithTagRecursive(list, "step_capsule");
		_rampPhysicsList = new MBList<GameEntity>();
		foreach (WeakGameEntity item in list)
		{
			if (item.GetVisibilityExcludeParents())
			{
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
				gameEntity.SetVisibilityExcludeParents(visible: false);
				_rampPhysicsList.Add(gameEntity);
			}
		}
		RampVisualEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTagRecursive("bridge_target"));
		RampVisualEntity.SetVisibilityExcludeParents(visible: false);
		SetScriptComponentToTick(GetTickRequirement());
		EnemyRangeToStopUsing = 5f;
		IsDisabledForAttackerAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip != null && OwnerShip.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
		IsDisabledForDefenderAIDueToEnemyInRange = new QueryData<bool>(() => OwnerShip != null && OwnerShip.ShipOrder != null && OwnerShip.ShipOrder.IsEnemyOnShip, 1f);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	public override void OnDeploymentFinished()
	{
		base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
	}

	protected override void OnTick(float dt)
	{
		bool flag = !OwnerShip.BeingAbandoned && (LinkedAttachmentMachine?.CurrentAttachment != null || CurrentAttachment == null || (base.PilotAgent == null && (CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected || OwnerShip.IsDisconnectionBlocked())));
		base.PilotStandingPoint.SetIsDeactivatedSynched(flag);
		if (_focusObject.GetVisibilityExcludeParents() == flag)
		{
			_focusObject.SetVisibilityExcludeParents(!flag);
		}
		if (base.PilotAgent == null)
		{
			return;
		}
		if (OwnerShip.BeingAbandoned)
		{
			WorldFrame userFrameForAgent = base.PilotStandingPoint.GetUserFrameForAgent(base.PilotAgent);
			Vec3 targetDirection = userFrameForAgent.Rotation.f;
			targetDirection.Normalize();
			if (base.PilotAgent.GetCurrentAction(0) != _actionForJumpingOff && base.PilotAgent.Frame.origin.AsVec2.DistanceSquared(userFrameForAgent.Origin.AsVec2) <= 0.3f && Vec3.DotProduct(base.PilotAgent.Frame.rotation.f.NormalizedCopy(), targetDirection) > 0.95f)
			{
				Agent pilotAgent = base.PilotAgent;
				if (pilotAgent.Formation != null)
				{
					((IDetachment)this).RemoveAgent(pilotAgent);
					pilotAgent.Formation.AttachUnit(pilotAgent);
				}
				else
				{
					base.PilotAgent.StopUsingGameObject();
				}
				Vec3 vec = pilotAgent.Position + targetDirection * 10f;
				pilotAgent.GetComponent<AgentNavalComponent>().SetupAgentToAbandonShip();
				pilotAgent.SetActionChannel(0, in _actionForJumpingOff, ignorePriority: false, (AnimFlags)0uL);
				Vec2 targetPosition = vec.AsVec2;
				pilotAgent.SetTargetPositionAndDirection(in targetPosition, in targetDirection);
				pilotAgent.ClearTargetFrame();
			}
		}
		else
		{
			if (CurrentAttachment == null || CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				return;
			}
			if (base.PilotAgent.SetActionChannel(1, in ActionIndexCache.act_ship_connection_break, ignorePriority: false, (AnimFlags)0uL))
			{
				if (base.PilotAgent.GetCurrentActionProgress(1) > 0.99f)
				{
					CurrentAttachment.AttachmentSource.DisconnectAttachment();
					base.PilotAgent.StopUsingGameObject();
				}
			}
			else
			{
				base.PilotAgent.StopUsingGameObject();
			}
		}
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public override bool ShouldAutoLeaveDetachmentWhenDisabled(BattleSideEnum sideEnum)
	{
		return false;
	}

	public void AssignConnection(ShipAttachmentMachine.ShipAttachment shipAttachment)
	{
		CurrentAttachment = shipAttachment;
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=PUbT3s7W}{KEY} Cut Loose");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if ((CurrentAttachment != null && CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected) || (LinkedAttachmentMachine?.CurrentAttachment != null && LinkedAttachmentMachine.CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected))
		{
			return new TextObject("{=kCMGJl1W}Bridge");
		}
		return new TextObject("{=7zCPG8TR}Hook");
	}

	public bool IsShipAttachmentMachinePointBridgeWithEnemy()
	{
		if (CurrentAttachment != null)
		{
			Team team = CurrentAttachment?.AttachmentSource?.OwnerShip?.Team;
			Team team2 = CurrentAttachment?.AttachmentTarget?.OwnerShip?.Team;
			if (team != null && team2 != null && team.IsEnemyOf(team2))
			{
				return CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected;
			}
			return false;
		}
		return false;
	}

	public bool IsShipAttachmentPointBridged()
	{
		if (CurrentAttachment != null)
		{
			if (CurrentAttachment.State != ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected)
			{
				return CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown;
			}
			return true;
		}
		return false;
	}

	public bool IsShipAttachmentPointConnectedToEnemy()
	{
		if (CurrentAttachment != null && (CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.RopesPulling || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeThrown || CurrentAttachment.State == ShipAttachmentMachine.ShipAttachment.ShipAttachmentState.BridgeConnected) && CurrentAttachment.AttachmentSource.OwnerShip.Team != null && CurrentAttachment.AttachmentTarget.OwnerShip.Team != null && CurrentAttachment.AttachmentSource.OwnerShip.Team.IsEnemyOf(CurrentAttachment.AttachmentTarget.OwnerShip.Team))
		{
			Formation formation = CurrentAttachment.AttachmentSource.OwnerShip.Formation;
			if (formation == null)
			{
				return false;
			}
			return formation.CountOfUnits > 0;
		}
		return false;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipAttachmentPointAI(this);
	}

	protected override bool OnCheckForProblems()
	{
		return true;
	}

	public void SetJumpOffAction(ActionIndexCache action)
	{
		_actionForJumpingOff = action;
	}
}
