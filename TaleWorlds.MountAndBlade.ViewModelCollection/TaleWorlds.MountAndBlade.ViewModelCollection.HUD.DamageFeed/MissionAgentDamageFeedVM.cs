using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.DamageFeed;

public class MissionAgentDamageFeedVM : ViewModel
{
	private readonly TextObject _takenDamageText;

	private MBBindingList<MissionAgentDamageFeedItemVM> _feedList;

	[DataSourceProperty]
	public MBBindingList<MissionAgentDamageFeedItemVM> FeedList
	{
		get
		{
			return _feedList;
		}
		set
		{
			if (value != _feedList)
			{
				_feedList = value;
				OnPropertyChangedWithValue(value, "FeedList");
			}
		}
	}

	public MissionAgentDamageFeedVM()
	{
		_takenDamageText = new TextObject("{=meFS5F4V}-{DAMAGE}");
		FeedList = new MBBindingList<MissionAgentDamageFeedItemVM>();
		CombatLogManager.OnGenerateCombatLog += CombatLogManagerOnPrintCombatLog;
	}

	public override void OnFinalize()
	{
		CombatLogManager.OnGenerateCombatLog -= CombatLogManagerOnPrintCombatLog;
		base.OnFinalize();
	}

	private void CombatLogManagerOnPrintCombatLog(CombatLogData logData)
	{
		int num = 0;
		if (logData.IsVictimAgentMine)
		{
			num = logData.TotalDamage;
		}
		else if (logData.IsFriendlyFire)
		{
			num = logData.ReflectedDamage;
		}
		if (num > 0)
		{
			_takenDamageText.SetTextVariable("DAMAGE", logData.TotalDamage);
			MissionAgentDamageFeedItemVM item = new MissionAgentDamageFeedItemVM(_takenDamageText.ToString(), RemoveItem);
			FeedList.Add(item);
		}
	}

	private void RemoveItem(MissionAgentDamageFeedItemVM item)
	{
		FeedList.Remove(item);
	}
}
