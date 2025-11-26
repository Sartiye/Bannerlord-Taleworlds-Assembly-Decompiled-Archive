using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerHUDExtensionUIHandler))]
public class MissionGauntletMultiplayerHUDExtension : MissionView
{
	private MissionMultiplayerHUDExtensionVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _mpMissionCategory;

	private MissionLobbyComponent _lobbyComponent;

	public MissionGauntletMultiplayerHUDExtension()
	{
		ViewOrderPriority = 2;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_mpMissionCategory = UIResourceManager.LoadSpriteCategory("ui_mpmission");
		_dataSource = new MissionMultiplayerHUDExtensionVM(base.Mission);
		_gauntletLayer = new GauntletLayer("HUDExtension", ViewOrderPriority);
		_gauntletLayer.LoadMovie("HUDExtension", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.MissionScreen.OnSpectateAgentFocusIn += _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut += _dataSource.OnSpectatedAgentFocusOut;
		Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent);
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
	}

	public override void OnMissionScreenFinalize()
	{
		_lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
		base.MissionScreen.OnSpectateAgentFocusIn -= _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut -= _dataSource.OnSpectatedAgentFocusOut;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_mpMissionCategory?.Unload();
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent);
		base.OnMissionScreenFinalize();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.Tick(dt);
	}

	private void OnMissionPlayerToggledOrderViewEvent(MissionPlayerToggledOrderViewEvent eventObj)
	{
		_dataSource.IsOrderActive = eventObj.IsOrderEnabled;
	}

	private void OnPostMatchEnded()
	{
		_dataSource.ShowHud = false;
	}
}
