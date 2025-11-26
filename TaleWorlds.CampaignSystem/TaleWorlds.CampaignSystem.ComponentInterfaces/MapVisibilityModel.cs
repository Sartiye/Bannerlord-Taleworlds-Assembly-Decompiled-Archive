using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MapVisibilityModel : MBGameModel<MapVisibilityModel>
{
	public abstract float MaximumSeeingRange();

	public abstract float GetPartySpottingRangeBase(MobileParty party);

	public abstract ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false);

	public abstract float GetPartyRelativeInspectionRange(IMapPoint party);

	public abstract float GetPartySpottingDifficulty(MobileParty spotterParty, MobileParty party);

	public abstract float GetHideoutSpottingDistance();
}
