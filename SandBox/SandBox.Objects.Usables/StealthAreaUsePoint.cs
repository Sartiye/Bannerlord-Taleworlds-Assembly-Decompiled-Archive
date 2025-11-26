using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class StealthAreaUsePoint : UsableMissionObject
{
	private bool _isEnabled;

	public string ActionStringId;

	public string DescriptionStringId;

	private bool _isAlreadyUsed;

	protected override void OnInit()
	{
		base.OnInit();
		_isAlreadyUsed = false;
		ActionMessage = GameTexts.FindText(string.IsNullOrEmpty(ActionStringId) ? "str_call_troops" : ActionStringId);
		ActionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		DescriptionMessage = GameTexts.FindText(string.IsNullOrEmpty(DescriptionStringId) ? "str_call_troops_description" : DescriptionStringId);
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return DescriptionMessage;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		if (userAgent.IsMainAgent)
		{
			Vec3 position = userAgent.Position;
			SoundManager.StartOneShotEvent("event:/mission/combat/pickup_arrows", in position);
			_isAlreadyUsed = true;
			userAgent.StopUsingGameObject();
		}
		DisableAgentAIs();
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public void DisableAgentAIs()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.IsActive() && agent.IsAIControlled)
			{
				agent.SetIsAIPaused(isPaused: true);
				WorldPosition position = new WorldPosition(Mission.Current.Scene, agent.Position);
				agent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
			}
		}
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (!agent.IsMainAgent)
		{
			if (!_isAlreadyUsed)
			{
				return !_isEnabled;
			}
			return true;
		}
		return false;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		if (userAgent.IsMainAgent && !_isAlreadyUsed && _isEnabled)
		{
			return !IsInCombat();
		}
		return false;
	}

	private bool IsInCombat()
	{
		bool result = false;
		foreach (Agent allAgent in Mission.Current.AllAgents)
		{
			if (allAgent.IsActive() && allAgent.AIStateFlags.HasFlag(Agent.AIStateFlag.Alarmed))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void EnableStealthAreaUsePoint()
	{
		_isEnabled = true;
		Vec3 position = base.GameEntity.GlobalPosition;
		SoundManager.StartOneShotEvent("event:/ui/notification/quest_update", in position);
	}
}
