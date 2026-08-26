using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalDLCSettlementGarrisonModel : SettlementGarrisonModel
{
	public override int GetMaximumDailyAutoRecruitmentCount(Town town)
	{
		return base.BaseModel.GetMaximumDailyAutoRecruitmentCount(town);
	}

	public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
	{
		return base.BaseModel.CalculateBaseGarrisonChange(settlement, includeDescriptions);
	}

	public override int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float idealGarrisonStrengthPerWalledCenter = 0f)
	{
		return base.BaseModel.FindNumberOfTroopsToTakeFromGarrison(mobileParty, settlement, idealGarrisonStrengthPerWalledCenter);
	}

	public override int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement)
	{
		return base.BaseModel.FindNumberOfTroopsToLeaveToGarrison(mobileParty, settlement);
	}

	public override float GetMaximumDailyRepairAmount(Settlement settlement)
	{
		return base.BaseModel.GetMaximumDailyRepairAmount(settlement);
	}
}
