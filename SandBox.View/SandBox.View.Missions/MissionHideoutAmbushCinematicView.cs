using SandBox.Missions.MissionLogics.Hideout;
using SandBox.Objects.Cinematics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionHideoutAmbushCinematicView : MissionView
{
	private enum HideoutAmbushCinematicState
	{
		None,
		FirstFadeOut,
		ChangeToCustomCamera,
		FirstFadeIn,
		SendArrow,
		Wait,
		SecondFadeOut,
		ChangeBackToDefaultCamera,
		SecondFadeIn,
		Ending,
		Ended
	}

	private const string CameraTag = "hideout_ambush_cutscene_camera";

	private const string ArrowBarrelTag = "hideout_ambush_cutscene_arrow_barrel";

	private const string ArrowPathTag = "hideout_ambush_cutscene_arrow_path";

	private Camera _camera;

	private GameEntity _cameraEntity;

	private GameEntity _arrowPath;

	private HideoutAmbushMissionController _hideoutAmbushMissionController;

	private MissionCameraFadeView _missionCameraFadeView;

	private HideoutAmbushCinematicState _currentHideoutAmbushCinematicState;

	private Timer _timer;

	protected virtual void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
	{
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_cameraEntity = base.Mission.Scene.FindEntityWithTag("hideout_ambush_cutscene_camera");
		_arrowPath = base.Mission.Scene.FindEntityWithTag("hideout_ambush_cutscene_arrow_path");
		_hideoutAmbushMissionController = base.Mission.GetMissionBehavior<HideoutAmbushMissionController>();
		_missionCameraFadeView = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
		Vec3 dofParams = Vec3.Invalid;
		_camera = Camera.CreateCamera();
		_cameraEntity.GetCameraParamsFromCameraScript(_camera, ref dofParams);
		_camera.SetFovVertical(_camera.GetFovVertical(), Screen.AspectRatio, _camera.Near, _camera.Far);
		_arrowPath.SetVisibilityExcludeParents(visible: false);
		_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.None;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		switch (_currentHideoutAmbushCinematicState)
		{
		case HideoutAmbushCinematicState.None:
		{
			HideoutAmbushMissionController hideoutAmbushMissionController = _hideoutAmbushMissionController;
			if (hideoutAmbushMissionController != null && hideoutAmbushMissionController.IsReadyForCallTroopsCinematic)
			{
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.FirstFadeOut;
				SetPlayerMovementEnabled(isPlayerMovementEnabled: false);
			}
			break;
		}
		case HideoutAmbushCinematicState.FirstFadeOut:
			_missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
			_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.ChangeToCustomCamera;
			break;
		case HideoutAmbushCinematicState.ChangeToCustomCamera:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
			{
				base.MissionScreen.CustomCamera = _camera;
				Agent.Main.AgentVisuals.SetVisible(value: false);
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.FirstFadeIn;
			}
			break;
		case HideoutAmbushCinematicState.FirstFadeIn:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
			{
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.SendArrow;
			}
			break;
		case HideoutAmbushCinematicState.SendArrow:
			_arrowPath.SetVisibilityExcludeParents(visible: true);
			_timer = new Timer(base.Mission.CurrentTime, 5f);
			_arrowPath.GetFirstScriptOfType<CinematicBurningArrow>().StartMovement();
			_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.Wait;
			break;
		case HideoutAmbushCinematicState.Wait:
			if (_timer.Check(base.Mission.CurrentTime))
			{
				_timer = null;
				_arrowPath.SetVisibilityExcludeParents(visible: false);
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.SecondFadeOut;
			}
			break;
		case HideoutAmbushCinematicState.SecondFadeOut:
			_missionCameraFadeView.BeginFadeOutAndIn(0.5f, 0.5f, 0.5f);
			_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.ChangeBackToDefaultCamera;
			break;
		case HideoutAmbushCinematicState.ChangeBackToDefaultCamera:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.Black)
			{
				base.MissionScreen.CustomCamera = null;
				Agent.Main.AgentVisuals.SetVisible(value: true);
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.SecondFadeIn;
			}
			break;
		case HideoutAmbushCinematicState.SecondFadeIn:
			if (_missionCameraFadeView.FadeState == MissionCameraFadeView.CameraFadeState.White)
			{
				_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.Ending;
			}
			break;
		case HideoutAmbushCinematicState.Ending:
			SetPlayerMovementEnabled(isPlayerMovementEnabled: true);
			_hideoutAmbushMissionController.OnAgentsShouldBeEnabled();
			_currentHideoutAmbushCinematicState = HideoutAmbushCinematicState.Ended;
			break;
		}
	}
}
