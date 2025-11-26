using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultHideoutModel : HideoutModel
{
	public override CampaignTime HideoutHiddenDuration => CampaignTime.Days(10f);

	public override int CanAttackHideoutStartTime => CampaignTime.SunSet + 1;

	public override int CanAttackHideoutEndTime => CampaignTime.SunRise;

	public override float GetRogueryXpGainOnHideoutMissionEnd(bool isSucceeded)
	{
		return isSucceeded ? MBRandom.RandomInt(700, 1000) : MBRandom.RandomInt(225, 400);
	}
}
