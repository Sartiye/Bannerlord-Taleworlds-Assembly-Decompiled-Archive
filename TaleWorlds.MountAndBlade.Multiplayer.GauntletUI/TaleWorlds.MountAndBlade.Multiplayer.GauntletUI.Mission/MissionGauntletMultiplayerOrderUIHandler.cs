using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerMissionOrderUIHandler))]
public class MissionGauntletMultiplayerOrderUIHandler : GauntletOrderUIHandler
{
	private IRoundComponent _roundComponent;

	private bool _isValid;

	private bool _shouldTick;

	private bool _shouldInitializeFormationInfo;

	public override bool IsDeployment => false;

	public override bool IsSiegeDeployment => false;

	public override bool IsValidForTick
	{
		get
		{
			if (_shouldTick && (!base.MissionScreen.IsRadialMenuActive || _dataSource.IsToggleOrderShown))
			{
				return !GameStateManager.Current.ActiveStateDisabledByUser;
			}
			return false;
		}
	}

	public MissionGauntletMultiplayerOrderUIHandler()
	{
		ViewOrderPriority = 19;
	}

	public override bool IsReady()
	{
		return true;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsPerFormation).GetValue(out int value);
		_shouldTick = value > 0;
	}

	public override void OnMissionScreenTick(float dt)
	{
		if (IsValidForTick)
		{
			if (!_isInitialized)
			{
				Team team = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (team != null && (team == base.Mission.AttackerTeam || team == base.Mission.DefenderTeam))
				{
					InitializeInADisgustingManner();
				}
			}
			if (!_isValid)
			{
				Team team2 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (team2 != null && (team2 == base.Mission.AttackerTeam || team2 == base.Mission.DefenderTeam))
				{
					ValidateInADisgustingManner();
				}
				return;
			}
			if (_shouldInitializeFormationInfo)
			{
				Team team3 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (_dataSource != null && team3 != null)
				{
					_dataSource.AfterInitialize();
					_shouldInitializeFormationInfo = false;
				}
			}
		}
		base.OnMissionScreenTick(dt);
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
		_siegeDeploymentHandler = null;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		_roundComponent = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>()?.RoundComponent;
		if (_roundComponent != null)
		{
			_roundComponent.OnRoundStarted += OnRoundStarted;
			_roundComponent.OnPreparationEnded += OnPreparationEnded;
		}
	}

	private void OnRoundStarted()
	{
		_dataSource?.AfterInitialize();
	}

	private void OnPreparationEnded()
	{
		_shouldInitializeFormationInfo = true;
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.OrderType:
			if (_gauntletLayer != null && _movie != null)
			{
				_gauntletLayer.ReleaseMovie(_movie);
				string movieName = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
				_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
			}
			break;
		case ManagedOptions.ManagedOptionsType.OrderLayoutType:
			_dataSource?.OnOrderLayoutTypeChanged();
			break;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		Clear();
		_orderTroopPlacer = null;
		MissionPeer.OnTeamChanged -= TeamChange;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (_roundComponent != null)
		{
			_roundComponent.OnRoundStarted -= OnRoundStarted;
			_roundComponent.OnPreparationEnded -= OnPreparationEnded;
		}
		base.OnMissionScreenFinalize();
	}

	protected override void OnTransferFinished()
	{
	}

	protected override void SetLayerEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			if (_dataSource == null || _dataSource.ActiveTargetState == 0)
			{
				_orderTroopPlacer.SuspendTroopPlacer = false;
			}
			base.MissionScreen.SetOrderFlagVisibility(value: true);
			Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: true));
		}
		else
		{
			_orderTroopPlacer.SuspendTroopPlacer = true;
			base.MissionScreen.SetOrderFlagVisibility(value: false);
			base.MissionScreen.UnregisterRadialMenuObject(this);
			Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: false));
		}
	}

	public void InitializeInADisgustingManner()
	{
		base.AfterStart();
		_orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
		base.MissionScreen.OrderFlag = _orderTroopPlacer.OrderFlag;
		base.MissionScreen.SetOrderFlagVisibility(value: false);
		MissionPeer.OnTeamChanged += TeamChange;
		_isInitialized = true;
	}

	public void ValidateInADisgustingManner()
	{
		_dataSource = new MissionOrderVM(base.Mission.PlayerTeam.PlayerOrderController, isDeployment: false, isMultiplayer: true);
		_dataSource.SetDeploymentParemeters(base.MissionScreen.CombatCamera, IsSiegeDeployment ? _siegeDeploymentHandler.PlayerDeploymentPoints.ToList() : new List<DeploymentPoint>());
		_dataSource.SetCallbacks(new MissionOrderCallbacks
		{
			ToggleMissionInputs = base.ToggleScreenRotation,
			RefreshVisuals = RefreshVisuals,
			GetVisualOrderExecutionParameters = base.GetVisualOrderExecutionParameters,
			SetSuspendTroopPlacer = SetSuspendTroopPlacer,
			OnActivateToggleOrder = base.OnActivateToggleOrder,
			OnDeactivateToggleOrder = base.OnDeactivateToggleOrder,
			OnTransferTroopsFinished = OnTransferFinished,
			OnBeforeOrder = base.OnBeforeOrder
		});
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("ToggleEscapeMenu"));
		_dataSource.TroopController.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.TroopController.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.TroopController.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		GameKeyContext category = HotKeyManager.GetCategory("MissionOrderHotkeyCategory");
		_dataSource.SetOrderIndexKey(0, category.GetGameKey(69));
		_dataSource.SetOrderIndexKey(1, category.GetGameKey(70));
		_dataSource.SetOrderIndexKey(2, category.GetGameKey(71));
		_dataSource.SetOrderIndexKey(3, category.GetGameKey(72));
		_dataSource.SetOrderIndexKey(4, category.GetGameKey(73));
		_dataSource.SetOrderIndexKey(5, category.GetGameKey(74));
		_dataSource.SetOrderIndexKey(6, category.GetGameKey(75));
		_dataSource.SetOrderIndexKey(7, category.GetGameKey(76));
		_dataSource.SetOrderIndexKey(8, category.GetGameKey(77));
		_dataSource.SetReturnKey(category.GetGameKey(77));
		_gauntletLayer = new GauntletLayer("MultiplayerOrder", ViewOrderPriority);
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_order");
		string movieName = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
		_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
		_dataSource.InputRestrictions = _gauntletLayer.InputRestrictions;
		base.MissionScreen.AddLayer(_gauntletLayer);
		_dataSource.AfterInitialize();
		_isValid = true;
	}

	private void RefreshVisuals()
	{
	}

	private void Clear()
	{
		if (_gauntletLayer != null)
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
		}
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
		}
		_gauntletLayer = null;
		_dataSource = null;
		_movie = null;
		if (_isValid)
		{
			_spriteCategory.Unload();
		}
	}

	private void TeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine)
		{
			Clear();
			_isValid = false;
		}
	}
}
