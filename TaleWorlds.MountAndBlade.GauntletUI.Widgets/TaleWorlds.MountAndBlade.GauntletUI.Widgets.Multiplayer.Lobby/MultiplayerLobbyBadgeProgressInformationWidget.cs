using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyBadgeProgressInformationWidget : Widget
{
	private int _shownBadgeCount;

	private ListPanel _activeBadgesList;

	public float CenterBadgeSize { get; set; } = 200f;


	public float OuterBadgeBaseSize { get; set; } = 175f;


	public float SizeDecayFromCenterPerElement { get; set; } = 25f;


	[Editor(false)]
	public int ShownBadgeCount
	{
		get
		{
			return _shownBadgeCount;
		}
		set
		{
			if (value != _shownBadgeCount)
			{
				_shownBadgeCount = value;
				OnPropertyChanged(value, "ShownBadgeCount");
				ArrangeChildrenSizes();
			}
		}
	}

	[Editor(false)]
	public ListPanel ActiveBadgesList
	{
		get
		{
			return _activeBadgesList;
		}
		set
		{
			if (value != _activeBadgesList)
			{
				if (_activeBadgesList != null)
				{
					_activeBadgesList.ItemAddEventHandlers.Remove(OnBadgeAdded);
				}
				_activeBadgesList = value;
				OnPropertyChanged(value, "ActiveBadgesList");
				if (value != null)
				{
					_activeBadgesList.ItemAddEventHandlers.Add(OnBadgeAdded);
				}
			}
		}
	}

	public MultiplayerLobbyBadgeProgressInformationWidget(UIContext context)
		: base(context)
	{
	}

	private void OnBadgeAdded(Widget parent, Widget child)
	{
		ArrangeChildrenSizes();
	}

	private void ArrangeChildrenSizes()
	{
		ActiveBadgesList.IsVisible = ShownBadgeCount > 0;
		int num = ShownBadgeCount / 2;
		int num2 = 0;
		List<Widget> allChildrenRecursive = ActiveBadgesList.GetAllChildrenRecursive();
		for (int i = 0; i < allChildrenRecursive.Count; i++)
		{
			if (allChildrenRecursive[i] is MultiplayerPlayerBadgeVisualWidget multiplayerPlayerBadgeVisualWidget)
			{
				float num3 = MathF.Abs(num2 - num);
				float num4 = OuterBadgeBaseSize - SizeDecayFromCenterPerElement * num3;
				if (num2 == num)
				{
					num4 = CenterBadgeSize;
				}
				multiplayerPlayerBadgeVisualWidget.SetForcedSize(num4, num4);
				num2++;
			}
		}
	}
}
