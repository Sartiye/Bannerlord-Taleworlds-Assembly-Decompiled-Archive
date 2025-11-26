using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;

public class GameNotificationWidget : BrushWidget
{
	private bool _textWidgetAlignmentDirty = true;

	private int _notificationId;

	private RichTextWidget _textWidget;

	private ImageIdentifierWidget _announcerImageIdentifier;

	private float _totalTime;

	private float _totalDt;

	public float RampUpInSeconds { get; set; }

	public float RampDownInSeconds { get; set; }

	public ImageIdentifierWidget AnnouncerImageIdentifier
	{
		get
		{
			return _announcerImageIdentifier;
		}
		set
		{
			if (_announcerImageIdentifier != value)
			{
				_announcerImageIdentifier = value;
				OnPropertyChanged(value, "AnnouncerImageIdentifier");
			}
		}
	}

	public int NotificationId
	{
		get
		{
			return _notificationId;
		}
		set
		{
			if (_notificationId != value)
			{
				_notificationId = value;
				OnPropertyChanged(value, "NotificationId");
				_textWidgetAlignmentDirty = true;
			}
		}
	}

	public float TotalTime
	{
		get
		{
			return _totalTime;
		}
		set
		{
			if (_totalTime != value)
			{
				_totalTime = value;
			}
		}
	}

	public RichTextWidget TextWidget
	{
		get
		{
			return _textWidget;
		}
		set
		{
			if (_textWidget != value)
			{
				_textWidget = value;
				OnPropertyChanged(value, "TextWidget");
			}
		}
	}

	public float TotalDt
	{
		get
		{
			return _totalDt;
		}
		set
		{
			if (_totalDt != value)
			{
				_totalDt = value;
				OnPropertyChanged(value, "TotalDt");
			}
		}
	}

	public GameNotificationWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_textWidgetAlignmentDirty)
		{
			ImageIdentifierWidget announcerImageIdentifier = AnnouncerImageIdentifier;
			if (announcerImageIdentifier != null && announcerImageIdentifier.IsVisible)
			{
				TextWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
			}
			else
			{
				TextWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
			}
		}
		if (base.IsVisible && TotalTime > 0f && TotalDt <= TotalTime)
		{
			if (TotalDt <= RampUpInSeconds)
			{
				float alphaFactor = Mathf.Lerp(0f, 1f, TotalDt / RampUpInSeconds);
				this.SetGlobalAlphaRecursively(alphaFactor);
			}
			else if (TotalDt < TotalTime - RampDownInSeconds)
			{
				this.SetGlobalAlphaRecursively(1f);
			}
			else
			{
				float alphaFactor2 = Mathf.Lerp(1f, 0f, 1f - (TotalTime - TotalDt) / RampDownInSeconds);
				this.SetGlobalAlphaRecursively(alphaFactor2);
			}
		}
	}
}
