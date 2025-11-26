using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

[DefaultView]
public class MissionCameraFadeView : MissionView
{
	public enum CameraFadeState
	{
		White,
		FadingOut,
		Black,
		FadingIn
	}

	private bool _autoFadeIn;

	private float _fadeInTime = 0.5f;

	private float _blackTime = 0.25f;

	private float _fadeOutTime = 0.5f;

	private float _stateDuration;

	public float FadeAlpha { get; private set; }

	public CameraFadeState FadeState { get; private set; }

	public bool IsCameraFading
	{
		get
		{
			if (FadeState != CameraFadeState.FadingIn)
			{
				return FadeState == CameraFadeState.FadingOut;
			}
			return true;
		}
	}

	public bool HasCameraFadeOut => FadeState == CameraFadeState.Black;

	public bool HasCameraFadeIn => FadeState == CameraFadeState.White;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_stateDuration = 0f;
		FadeState = CameraFadeState.White;
		FadeAlpha = 0f;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.Mission != null && base.MissionScreen.IsMissionTickable && !MBCommon.IsPaused)
		{
			UpdateFadeState(dt);
		}
	}

	public override bool OnEscape()
	{
		return IsCameraFading;
	}

	public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
	{
		return !IsCameraFading;
	}

	protected void UpdateFadeState(float dt)
	{
		if (IsCameraFading)
		{
			_stateDuration -= dt;
			if (FadeState == CameraFadeState.FadingOut)
			{
				FadeAlpha = MathF.Min(1f - _stateDuration / _fadeOutTime, 1f);
				if (_stateDuration < 0f)
				{
					_stateDuration = _blackTime;
					FadeState = CameraFadeState.Black;
				}
			}
			else if (FadeState == CameraFadeState.FadingIn)
			{
				FadeAlpha = MathF.Max(_stateDuration / _fadeInTime, 0f);
				if (_stateDuration < 0f)
				{
					_stateDuration = 0f;
					FadeState = CameraFadeState.White;
				}
			}
		}
		else if (HasCameraFadeOut && _autoFadeIn)
		{
			_stateDuration -= dt;
			if (_stateDuration < 0f)
			{
				_stateDuration = _fadeInTime;
				FadeState = CameraFadeState.FadingIn;
				_autoFadeIn = false;
			}
		}
	}

	public void BeginFadeOutAndIn(float fadeOutTime, float blackTime, float fadeInTime)
	{
		if (base.Mission != null && base.MissionScreen.IsMissionTickable && FadeState == CameraFadeState.White)
		{
			_autoFadeIn = true;
			_fadeOutTime = MathF.Max(fadeOutTime, 1E-05f);
			_blackTime = MathF.Max(blackTime, 1E-05f);
			_fadeInTime = MathF.Max(fadeInTime, 1E-05f);
			_stateDuration = fadeOutTime;
			FadeAlpha = 0f;
			FadeState = CameraFadeState.FadingOut;
		}
	}

	public void BeginFadeOut(float fadeOutTime)
	{
		if (base.Mission != null && base.MissionScreen.IsMissionTickable && FadeState == CameraFadeState.White)
		{
			_autoFadeIn = false;
			_fadeOutTime = MathF.Max(fadeOutTime, 1E-05f);
			_blackTime = 0f;
			_fadeInTime = 0f;
			_stateDuration = fadeOutTime;
			FadeAlpha = 0f;
			FadeState = CameraFadeState.FadingOut;
		}
	}

	public void BeginFadeIn(float fadeInTime)
	{
		if (base.Mission != null && base.MissionScreen.IsMissionTickable && FadeState == CameraFadeState.Black && !_autoFadeIn)
		{
			_fadeOutTime = 0f;
			_blackTime = 0f;
			_fadeInTime = MathF.Max(fadeInTime, 1E-05f);
			_stateDuration = fadeInTime;
			FadeAlpha = 1f;
			FadeState = CameraFadeState.FadingIn;
		}
	}
}
