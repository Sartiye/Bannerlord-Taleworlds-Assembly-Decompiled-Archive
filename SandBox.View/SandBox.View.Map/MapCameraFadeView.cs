using TaleWorlds.Library;

namespace SandBox.View.Map;

public class MapCameraFadeView : MapView
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

	protected internal override void OnInitialize()
	{
		_stateDuration = 0f;
		FadeState = CameraFadeState.White;
		FadeAlpha = 0f;
	}

	protected internal override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (base.MapScreen.IsActive)
		{
			UpdateFadeState(dt);
		}
	}

	protected internal override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		if (base.MapScreen.IsActive)
		{
			UpdateFadeState(dt);
		}
	}

	protected internal override void OnMenuModeTick(float dt)
	{
		base.OnMenuModeTick(dt);
		if (base.MapScreen.IsActive)
		{
			UpdateFadeState(dt);
		}
	}

	protected internal override void OnFinalize()
	{
		base.OnFinalize();
		_autoFadeIn = false;
	}

	private void UpdateFadeState(float dt)
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
		if (base.MapScreen.IsActive && FadeState == CameraFadeState.White)
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
		if (base.MapScreen.IsActive && FadeState == CameraFadeState.White)
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
		if (base.MapScreen.IsActive && FadeState == CameraFadeState.Black && !_autoFadeIn)
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
