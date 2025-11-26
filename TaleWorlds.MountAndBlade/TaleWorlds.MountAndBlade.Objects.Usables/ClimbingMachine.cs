using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class ClimbingMachine : UsableMachine
{
	private const float ClimbingEndDisplacement = 0.3f;

	private const float ClimbingSpeed = 1.4f;

	private const float EndingAnimationTriggerDifference = 1.8f;

	private const string ClimbingLoopActionName = "act_climb_net";

	private const string ClimbingEndActionName = "act_climb_net_ending";

	private const string ClimbingEndContinueActionName = "act_climb_net_ending_continue";

	private ActionIndexCache _climbingLoop;

	private ActionIndexCache _climbingEnd;

	private ActionIndexCache _climbingEndContinue;

	private GameEntity _climbEndingPoint;

	private float _usageDuration;

	public override float SinkingReferenceOffset => base.GameEntity.GetGlobalScale().z * -1.5f;

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=fEQAPJ2e}{KEY} Use");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=mUIew6a6}Climbing Net");
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_climbingLoop = ActionIndexCache.Create("act_climb_net");
		_climbingEnd = ActionIndexCache.Create("act_climb_net_ending");
		_climbingEndContinue = ActionIndexCache.Create("act_climb_net_ending_continue");
		_climbEndingPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("climb_end"));
		base.PilotStandingPoint.AutoEquipWeaponsOnUseStopped = true;
		SetScriptComponentToTick(GetTickRequirement());
	}

	private void OnUseAction(Agent userAgent)
	{
		userAgent.SetForceAttachedEntity(base.GameEntity);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	public override void OnDeploymentFinished()
	{
		base.PilotStandingPoint.AddComponent(new ResetGravityExclusionAndEntityAttachmentOnStopUsageComponent(OnUseAction));
		base.PilotStandingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
		base.PilotStandingPoint.SetAreUserPositionsUpdatedInTheMachineTick(value: true);
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (base.PilotStandingPoint.HasUser)
		{
			Agent userAgent = base.PilotStandingPoint.UserAgent;
			ActionIndexCache currentAction = userAgent.GetCurrentAction(0);
			_usageDuration += dt;
			if (currentAction == _climbingLoop || currentAction == _climbingEnd)
			{
				Vec3 vec = base.PilotStandingPoint.GameEntity.GetGlobalFrame().origin + base.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.u.NormalizedCopy() * _usageDuration * 1.4f;
				if (currentAction == _climbingEnd)
				{
					vec += new Vec3(base.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized() * (Math.Min(userAgent.GetCurrentActionProgress(0) * 1f, 1f) * 0.3f));
				}
				userAgent.SetTargetZ(vec.z);
				Vec2 targetPosition = vec.AsVec2;
				Vec3 targetDirection = base.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.f.NormalizedCopy();
				userAgent.SetTargetPositionAndDirection(in targetPosition, in targetDirection);
				Vec3 targetUp = base.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.u.NormalizedCopy();
				userAgent.SetTargetUp(in targetUp);
				float num = Vec3.DotProduct(targetUp, _climbEndingPoint.GlobalPosition - vec);
				if (currentAction == _climbingLoop && num < 1.8f && !userAgent.SetActionChannel(0, in _climbingEnd, ignorePriority: false, (AnimFlags)0uL))
				{
					userAgent.StopUsingGameObject();
					_usageDuration = 0f;
				}
			}
			else if (currentAction == _climbingEndContinue)
			{
				userAgent.ClearTargetFrame();
				userAgent.SetTargetZ(_climbEndingPoint.GlobalPosition.z);
				userAgent.SetTargetUp(in Vec3.Zero);
				if (userAgent.GetCurrentActionProgress(0) > 0.95f)
				{
					userAgent.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: true);
					userAgent.StopUsingGameObject(isSuccessful: false);
					_usageDuration = 0f;
				}
			}
			else if (userAgent.SetActionChannel(0, in _climbingLoop, ignorePriority: false, (AnimFlags)0uL))
			{
				userAgent.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
			}
			else
			{
				userAgent.StopUsingGameObject();
				_usageDuration = 0f;
			}
		}
		else
		{
			_usageDuration = 0f;
		}
	}
}
