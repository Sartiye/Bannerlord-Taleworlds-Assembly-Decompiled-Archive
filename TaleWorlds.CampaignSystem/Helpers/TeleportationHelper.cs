using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Helpers;

public static class TeleportationHelper
{
	public static float GetHoursLeftForTeleportingHeroToReachItsDestination(Hero teleportingHero)
	{
		return Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>()?.GetHeroArrivalTimeToDestination(teleportingHero).RemainingHoursFromNow ?? 0f;
	}

	public static Hero GetTeleportingLeaderHero(MobileParty party, ITeleportationCampaignBehavior teleportationBehavior)
	{
		if (party != null && teleportationBehavior != null)
		{
			foreach (Hero item in Hero.MainHero.Clan.Heroes.Where((Hero x) => x.IsAlive && x.IsTraveling))
			{
				if (teleportationBehavior.GetTargetOfTeleportingHero(item, out var _, out var isPartyLeader, out var target) && isPartyLeader && target is MobileParty mobileParty && mobileParty == party)
				{
					return item;
				}
			}
		}
		return null;
	}
}
