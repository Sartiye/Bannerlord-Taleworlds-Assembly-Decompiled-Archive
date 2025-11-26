using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[DefaultView]
public class MissionGauntletCameraFadeView : MissionView
{
	private GauntletLayer _layer;

	private BindingListFloatItem _dataSource;

	private MissionCameraFadeView _controller;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new BindingListFloatItem(0f);
		_layer = new GauntletLayer("MissionCameraFade", 100000);
		_layer.LoadMovie("CameraFade", _dataSource);
		base.MissionScreen.AddLayer(_layer);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_controller = base.Mission.GetMissionBehavior<MissionCameraFadeView>();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_dataSource != null && _controller != null)
		{
			_dataSource.Item = _controller.FadeAlpha;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_layer);
		_controller = null;
		_dataSource = null;
		_layer = null;
	}
}
