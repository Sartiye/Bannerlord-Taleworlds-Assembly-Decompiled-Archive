using System;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherCircleLoadingAnimWidget : Widget
{
	public enum VisualState
	{
		FadeIn,
		Animating,
		FadeOut
	}

	private VisualState _visualState;

	private float _stayStartTime;

	private float _currentAngle;

	private bool _initialized;

	private float _totalTime;

	private Widget[] _cachedChildren;

	public float NumOfCirclesInASecond { get; set; } = 0.5f;


	public float NormalAlpha { get; set; } = 0.5f;


	public float FullAlpha { get; set; } = 1f;


	public float CircleRadius { get; set; } = 50f;


	public float StaySeconds { get; set; } = 2f;


	public bool IsMovementEnabled { get; set; } = true;


	public bool IsReverse { get; set; }

	public float FadeInSeconds { get; set; } = 0.2f;


	public float FadeOutSeconds { get; set; } = 0.2f;


	private float CurrentAlpha => _cachedChildren.FirstOrDefault()?.AlphaFactor ?? 0f;

	public LauncherCircleLoadingAnimWidget(UIContext context)
		: base(context)
	{
		_cachedChildren = new Widget[0];
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		_totalTime += dt;
		_cachedChildren = base.Children.ToArray();
		if (!_initialized)
		{
			_visualState = VisualState.FadeIn;
			this.SetGlobalAlphaRecursively(0f);
			_initialized = true;
		}
		if (IsMovementEnabled && base.IsVisible)
		{
			UpdateMovementValues(dt);
			UpdateAlphaValues(dt);
		}
	}

	private void UpdateMovementValues(float dt)
	{
		if (IsMovementEnabled)
		{
			float num = 360f / (float)_cachedChildren.Length;
			float num2 = _currentAngle;
			for (int i = 0; i < _cachedChildren.Length; i++)
			{
				float num3 = TaleWorlds.Library.MathF.Cos(num2 * (System.MathF.PI / 180f)) * CircleRadius;
				float num4 = TaleWorlds.Library.MathF.Sin(num2 * (System.MathF.PI / 180f)) * CircleRadius;
				_cachedChildren[i].PositionXOffset = (IsReverse ? num4 : num3);
				_cachedChildren[i].PositionYOffset = (IsReverse ? num3 : num4);
				num2 += num;
				num2 %= 360f;
			}
			_currentAngle += dt * 360f * NumOfCirclesInASecond;
			_currentAngle %= 360f;
		}
	}

	private void UpdateAlphaValues(float dt)
	{
		float alphaFactor = 1f;
		if (_visualState == VisualState.FadeIn)
		{
			alphaFactor = Mathf.Lerp(CurrentAlpha, 1f, dt / FadeInSeconds);
			if (CurrentAlpha >= 0.9f)
			{
				_visualState = VisualState.Animating;
				_stayStartTime = _totalTime;
			}
		}
		else if (_visualState == VisualState.Animating)
		{
			alphaFactor = 1f;
			if (StaySeconds != -1f && _totalTime - _stayStartTime > StaySeconds)
			{
				_visualState = VisualState.FadeOut;
			}
		}
		else if (_visualState == VisualState.FadeOut)
		{
			alphaFactor = Mathf.Lerp(CurrentAlpha, 0f, dt / FadeOutSeconds);
			if (CurrentAlpha <= 0.01f && _totalTime - (_stayStartTime + StaySeconds + FadeOutSeconds) > 3f)
			{
				_visualState = VisualState.FadeIn;
			}
		}
		else
		{
			Debug.FailedAssert("This visual state is not enabled", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Launcher.Library\\CustomWidgets\\LauncherCircleLoadingAnimWidget.cs", "UpdateAlphaValues", 119);
		}
		this.SetGlobalAlphaRecursively(alphaFactor);
	}
}
