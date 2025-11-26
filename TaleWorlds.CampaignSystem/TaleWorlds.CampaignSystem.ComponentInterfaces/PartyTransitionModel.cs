using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyTransitionModel : MBGameModel<PartyTransitionModel>
{
	public abstract CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty);

	public abstract CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty);

	public abstract CampaignTime GetFleetTravelTimeToPoint(MobileParty owner, CampaignVec2 target);
}
