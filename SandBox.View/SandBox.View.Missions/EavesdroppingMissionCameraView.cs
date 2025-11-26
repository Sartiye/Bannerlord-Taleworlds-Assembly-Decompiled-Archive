using SandBox.Missions;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class EavesdroppingMissionCameraView : MissionView
{
	private enum CameraSwitchState
	{
		None,
		ReadyForFadeOut,
		FadeOutAndInStarted,
		WaitingForFadeInToEnd
	}

	private CameraSwitchState _cameraSwitchState;

	private EavesdroppingMissionLogic _eavesdroppingMissionLogic;

	private MissionCameraFadeView _missionCameraFadeView;

	protected virtual void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_cameraSwitchState = CameraSwitchState.None;
		foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
		{
			if (missionBehavior is EavesdroppingMissionLogic)
			{
				_eavesdroppingMissionLogic = missionBehavior as EavesdroppingMissionLogic;
			}
			if (missionBehavior is MissionCameraFadeView)
			{
				_missionCameraFadeView = missionBehavior as MissionCameraFadeView;
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_eavesdroppingMissionLogic == null)
		{
			return;
		}
		switch (_cameraSwitchState)
		{
		case CameraSwitchState.None:
			if ((_eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera == null) || (!_eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera != null))
			{
				if (_eavesdroppingMissionLogic.EavesdropStarted && base.MissionScreen.CustomCamera == null)
				{
					SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
				}
				_cameraSwitchState = CameraSwitchState.ReadyForFadeOut;
			}
			break;
		case CameraSwitchState.ReadyForFadeOut:
			_missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
			_cameraSwitchState = CameraSwitchState.FadeOutAndInStarted;
			break;
		case CameraSwitchState.FadeOutAndInStarted:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
			{
				base.MissionScreen.CustomCamera = ((base.MissionScreen.CustomCamera == null) ? _eavesdroppingMissionLogic.CurrentEavesdroppingCamera : null);
				if (base.MissionScreen.CustomCamera == null)
				{
					SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
				}
				_cameraSwitchState = CameraSwitchState.WaitingForFadeInToEnd;
			}
			break;
		case CameraSwitchState.WaitingForFadeInToEnd:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
			{
				_cameraSwitchState = CameraSwitchState.None;
			}
			break;
		}
	}
}
