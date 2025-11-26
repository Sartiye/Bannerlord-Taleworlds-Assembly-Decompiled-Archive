using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class HideoutModel : MBGameModel<HideoutModel>
{
	public abstract CampaignTime HideoutHiddenDuration { get; }

	public abstract int CanAttackHideoutStartTime { get; }

	public abstract int CanAttackHideoutEndTime { get; }

	public abstract float GetRogueryXpGainOnHideoutMissionEnd(bool isSucceeded);
}
