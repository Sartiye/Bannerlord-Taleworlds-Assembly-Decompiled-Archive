using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionFormationTargetSelectionHandler : MissionView
{
	public const float MaxDistanceForFocusCheck = 1000f;

	public const float MinDistanceForFocusCheck = 10f;

	public readonly float MaxDistanceToCenterForFocus = 70f * (Screen.RealScreenResolutionHeight / 1080f);

	private readonly List<(Formation, float)> _distanceCache;

	private readonly MBList<Formation> _focusedFormationCache;

	private Vec2 _centerOfScreen = new Vec2(Screen.RealScreenResolutionWidth / 2f, Screen.RealScreenResolutionHeight / 2f);

	private bool _isTargetingDisabled;

	private Camera ActiveCamera => base.MissionScreen.CustomCamera ?? base.MissionScreen.CombatCamera;

	public event Action<MBReadOnlyList<Formation>> OnFormationFocused;

	public MissionFormationTargetSelectionHandler()
	{
		_distanceCache = new List<(Formation, float)>();
		_focusedFormationCache = new MBList<Formation>();
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		_distanceCache.Clear();
		_focusedFormationCache.Clear();
		if (base.Mission?.Teams == null)
		{
			return;
		}
		if (!_isTargetingDisabled)
		{
			Vec3 position = ActiveCamera.Position;
			_centerOfScreen.x = Screen.RealScreenResolutionWidth / 2f;
			_centerOfScreen.y = Screen.RealScreenResolutionHeight / 2f;
			for (int i = 0; i < base.Mission.Teams.Count; i++)
			{
				Team team = base.Mission.Teams[i];
				if (team.IsPlayerAlly)
				{
					continue;
				}
				for (int j = 0; j < team.FormationsIncludingEmpty.Count; j++)
				{
					Formation formation = team.FormationsIncludingEmpty[j];
					if (formation.CountOfUnits > 0)
					{
						TryGetFormationDistanceToCenter(formation, position, out var isFormationFocusable, out var distanceToScreenCenter);
						if (isFormationFocusable)
						{
							_distanceCache.Add((formation, distanceToScreenCenter));
						}
					}
				}
			}
		}
		if (_distanceCache.Count == 0)
		{
			this.OnFormationFocused?.Invoke(null);
			return;
		}
		Formation formation2 = null;
		float num = MaxDistanceToCenterForFocus;
		for (int k = 0; k < _distanceCache.Count; k++)
		{
			(Formation, float) tuple = _distanceCache[k];
			if (tuple.Item2 == 0f)
			{
				_focusedFormationCache.Add(tuple.Item1);
			}
			else if (tuple.Item2 < num)
			{
				num = tuple.Item2;
				(formation2, _) = tuple;
			}
		}
		if (formation2 != null)
		{
			_focusedFormationCache.Add(formation2);
		}
		this.OnFormationFocused?.Invoke(_focusedFormationCache);
	}

	private void TryGetFormationDistanceToCenter(Formation formation, Vec3 cameraPosition, out bool isFormationFocusable, out float distanceToScreenCenter)
	{
		WorldPosition cachedMedianPosition = formation.CachedMedianPosition;
		float num = cachedMedianPosition.AsVec2.Distance(cameraPosition.AsVec2);
		float screenX = 0f;
		float screenY = 0f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(ActiveCamera, cachedMedianPosition.GetGroundVec3() + new Vec3(0f, 0f, 3f), ref screenX, ref screenY, ref w);
		bool flag = w <= 0f;
		if (num >= 1000f)
		{
			distanceToScreenCenter = 2.1474836E+09f;
			isFormationFocusable = false;
		}
		else if (num <= 10f)
		{
			isFormationFocusable = !flag;
			distanceToScreenCenter = 0f;
		}
		else if (flag)
		{
			isFormationFocusable = false;
			distanceToScreenCenter = 2.1474836E+09f;
		}
		else
		{
			isFormationFocusable = true;
			distanceToScreenCenter = new Vec2(screenX, screenY).Distance(_centerOfScreen);
		}
	}

	public void SetIsFormationTargetingDisabled(bool isDisabled)
	{
		if (_isTargetingDisabled != isDisabled)
		{
			_isTargetingDisabled = isDisabled;
			if (isDisabled)
			{
				_distanceCache.Clear();
				_focusedFormationCache.Clear();
				this.OnFormationFocused?.Invoke(null);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		_distanceCache.Clear();
		_focusedFormationCache.Clear();
		this.OnFormationFocused = null;
		base.OnRemoveBehavior();
	}
}
