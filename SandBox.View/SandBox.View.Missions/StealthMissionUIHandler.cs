using SandBox.Objects.Usables;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class StealthMissionUIHandler : MissionView
{
	private MissionCameraFadeView _cameraFadeViewController;

	private bool _isInitialized;

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
		}
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		base.OnObjectUsed(userAgent, usedObject);
		if (_isInitialized && usedObject is StealthAreaUsePoint)
		{
			CameraFadeInFadeOut(0.5f, 0.5f, 1f);
		}
	}

	private void InitializeView()
	{
		_cameraFadeViewController = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
		_isInitialized = true;
	}

	private void CameraFadeInFadeOut(float fadeOutTime, float blackTime, float fadeInTime)
	{
		if (_cameraFadeViewController.FadeState == MissionCameraFadeView.CameraFadeState.White)
		{
			_cameraFadeViewController.BeginFadeOutAndIn(fadeOutTime, blackTime, fadeInTime);
		}
	}
}
