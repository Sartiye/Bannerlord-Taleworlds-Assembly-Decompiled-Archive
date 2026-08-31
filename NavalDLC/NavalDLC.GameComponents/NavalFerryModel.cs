using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalFerryModel : FerryModel
{
	public override int MaximumFerryCapacityForPassengers => 50;

	public override ExplainedNumber GetFerryCost(Settlement departureVillage, bool includeReason = false)
	{
		ExplainedNumber result = new ExplainedNumber(1000f, includeReason);
		if (departureVillage.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			result.AddFactor(1f, new TextObject("{=qZvPOkb4}Departing from enemy village."));
		}
		if (departureVillage.FerryTarget.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			result.AddFactor(1f, new TextObject("{=gROUTRVu}Arriving at enemy village."));
		}
		float num = MobileParty.MainParty.MemberRoster.TotalManCount + MobileParty.MainParty.PrisonRoster.TotalManCount;
		int maximumFerryCapacityForPassengers = Campaign.Current.Models.FerryModel.MaximumFerryCapacityForPassengers;
		float value = num / (float)maximumFerryCapacityForPassengers;
		result.AddFactor(value, new TextObject("{=wwS4BHsv}Number of passenger affect."));
		return result;
	}
}
