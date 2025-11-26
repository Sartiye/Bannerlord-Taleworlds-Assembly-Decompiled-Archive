using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardShipsNavigatableGridWidget : NavigatableGridWidget
{
	private HorizontalAlignment _regularHorizontalAlignment;

	private HorizontalAlignment _overflowHorizontalAlignment;

	[Editor(false)]
	public HorizontalAlignment RegularHorizontalAlignment
	{
		get
		{
			return _regularHorizontalAlignment;
		}
		set
		{
			if (_regularHorizontalAlignment != value)
			{
				_regularHorizontalAlignment = value;
				OnPropertyChanged(Enum.GetName(typeof(HorizontalAlignment), value), "RegularHorizontalAlignment");
			}
		}
	}

	[Editor(false)]
	public HorizontalAlignment OverflowHorizontalAlignment
	{
		get
		{
			return _overflowHorizontalAlignment;
		}
		set
		{
			if (_overflowHorizontalAlignment != value)
			{
				_overflowHorizontalAlignment = value;
				OnPropertyChanged(Enum.GetName(typeof(HorizontalAlignment), value), "OverflowHorizontalAlignment");
			}
		}
	}

	public ScoreboardShipsNavigatableGridWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		ScrollablePanel parentPanel = base.ParentPanel;
		if (parentPanel != null && parentPanel.ActiveScrollbar?.IsVisible == true)
		{
			base.HorizontalAlignment = OverflowHorizontalAlignment;
		}
		else
		{
			base.HorizontalAlignment = RegularHorizontalAlignment;
		}
	}
}
