using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalShipTargetSelectionHandler : MissionView
{
	public const float MaxDistanceForFocusCheck = 1000f;

	public const float MinDistanceForFocusCheck = 10f;

	public readonly float MaxDistanceToCenterForFocus = 70f * (Screen.RealScreenResolutionHeight / 1080f);

	private readonly List<(MissionShip, float)> _distanceCache = new List<(MissionShip, float)>();

	private readonly MBList<MissionShip> _focusedShipsCache = new MBList<MissionShip>();

	private readonly MBList<MissionShip> _enemyShipsCache = new MBList<MissionShip>();

	private Vec2 _centerOfScreen = new Vec2(Screen.RealScreenResolutionWidth / 2f, Screen.RealScreenResolutionHeight / 2f);

	private bool _isTargetingDisabled;

	private Camera ActiveCamera => base.MissionScreen.CustomCamera ?? base.MissionScreen.CombatCamera;

	public event Action<MBReadOnlyList<MissionShip>> OnShipsFocused;

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		_distanceCache.Clear();
		_focusedShipsCache.Clear();
		_enemyShipsCache.Clear();
		NavalShipsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null)
		{
			return;
		}
		if (!_isTargetingDisabled)
		{
			missionBehavior.FillTeamShips(TeamSideEnum.EnemyTeam, _enemyShipsCache);
			Vec3 position = ActiveCamera.Position;
			_centerOfScreen.x = Screen.RealScreenResolutionWidth / 2f;
			_centerOfScreen.y = Screen.RealScreenResolutionHeight / 2f;
			for (int i = 0; i < _enemyShipsCache.Count; i++)
			{
				MissionShip missionShip = _enemyShipsCache[i];
				float shipDistanceToCenter = GetShipDistanceToCenter(missionShip, position);
				_distanceCache.Add((missionShip, shipDistanceToCenter));
			}
		}
		if (_distanceCache.Count == 0)
		{
			this.OnShipsFocused?.Invoke(null);
			return;
		}
		MissionShip missionShip2 = null;
		float num = MaxDistanceToCenterForFocus;
		for (int j = 0; j < _distanceCache.Count; j++)
		{
			(MissionShip, float) tuple = _distanceCache[j];
			if (tuple.Item2 == 0f)
			{
				_focusedShipsCache.Add(tuple.Item1);
			}
			else if (tuple.Item2 < num)
			{
				num = tuple.Item2;
				(missionShip2, _) = tuple;
			}
		}
		if (missionShip2 != null)
		{
			_focusedShipsCache.Add(missionShip2);
		}
		this.OnShipsFocused?.Invoke(_focusedShipsCache);
	}

	private float GetShipDistanceToCenter(MissionShip ship, Vec3 cameraPosition)
	{
		Vec3 origin = ship.GlobalFrame.origin;
		float num = origin.AsVec2.Distance(cameraPosition.AsVec2);
		if (num >= 1000f)
		{
			return 2.1474836E+09f;
		}
		if (num <= 10f)
		{
			return 0f;
		}
		float screenX = 0f;
		float screenY = 0f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(ActiveCamera, origin + Vec3.Up * 3f, ref screenX, ref screenY, ref w);
		if (w <= 0f)
		{
			return 2.1474836E+09f;
		}
		return new Vec2(screenX, screenY).Distance(_centerOfScreen);
	}

	public void SetIsFormationTargetingDisabled(bool isDisabled)
	{
		if (_isTargetingDisabled != isDisabled)
		{
			_isTargetingDisabled = isDisabled;
			if (isDisabled)
			{
				_distanceCache.Clear();
				_enemyShipsCache.Clear();
				_focusedShipsCache.Clear();
				this.OnShipsFocused?.Invoke(null);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		_distanceCache.Clear();
		_focusedShipsCache.Clear();
		this.OnShipsFocused = null;
		base.OnRemoveBehavior();
	}
}
