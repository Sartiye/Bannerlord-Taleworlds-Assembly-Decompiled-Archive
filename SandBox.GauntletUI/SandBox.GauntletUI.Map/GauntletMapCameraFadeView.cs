using SandBox.View.Map;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapCameraFadeView))]
public class GauntletMapCameraFadeView : MapCameraFadeView
{
	private GauntletLayer _layer;

	private BindingListFloatItem _dataSource;

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_dataSource = new BindingListFloatItem(0f);
		_layer = new GauntletLayer("MapCameraFade", 100000);
		_layer.LoadMovie("CameraFade", _dataSource);
		base.MapScreen.AddLayer(_layer);
	}

	private void Tick(float dt)
	{
		if (_dataSource != null)
		{
			_dataSource.Item = base.FadeAlpha;
		}
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		Tick(dt);
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		Tick(dt);
	}

	protected override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		Tick(dt);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		base.MapScreen.RemoveLayer(_layer);
		_dataSource = null;
		_layer = null;
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		if (_layer != null)
		{
			ScreenManager.SetSuspendLayer(_layer, isSuspended: true);
		}
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		if (_layer != null)
		{
			ScreenManager.SetSuspendLayer(_layer, isSuspended: false);
		}
	}
}
