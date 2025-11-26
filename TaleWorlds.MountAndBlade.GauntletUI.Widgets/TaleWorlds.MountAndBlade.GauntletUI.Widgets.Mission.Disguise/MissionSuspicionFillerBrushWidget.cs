using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Disguise;

public class MissionSuspicionFillerBrushWidget : BrushWidget
{
	private float _returnToDefaultTimer;

	private float _colorChangeDuration;

	private float _currentSuspicionRatio;

	public float ColorChangeDuration
	{
		get
		{
			return _colorChangeDuration;
		}
		set
		{
			if (value != _colorChangeDuration)
			{
				_colorChangeDuration = value;
				OnPropertyChanged(value, "ColorChangeDuration");
			}
		}
	}

	public float CurrentSuspicionRatio
	{
		get
		{
			return _currentSuspicionRatio;
		}
		set
		{
			if (value != _currentSuspicionRatio)
			{
				SetState((value > _currentSuspicionRatio) ? "Increasing" : "Decreasing");
				base.EventManager.AddLateUpdateAction(this, ReturnToDefaultTick, 1);
				_currentSuspicionRatio = value;
				OnPropertyChanged(value, "CurrentSuspicionRatio");
				_returnToDefaultTimer = 0f;
			}
		}
	}

	public MissionSuspicionFillerBrushWidget(UIContext context)
		: base(context)
	{
		ColorChangeDuration = 0.2f;
	}

	private void ReturnToDefaultTick(float dt)
	{
		if (_returnToDefaultTimer >= ColorChangeDuration)
		{
			_returnToDefaultTimer = 0f;
			SetState("Default");
		}
		else
		{
			base.EventManager.AddLateUpdateAction(this, ReturnToDefaultTick, 1);
		}
	}
}
