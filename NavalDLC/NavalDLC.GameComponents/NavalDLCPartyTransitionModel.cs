using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCPartyTransitionModel : PartyTransitionModel
{
	private const float MinHoursToMoveAnchor = 3f;

	private const float MaxHoursToMoveAnchor = 48f;

	private const float AnchorMoveSpeedPerHour = 35f;

	private const float DisembarkHours = 2f;

	private const float InstantEmbarkDistanceThresholdForAI = 10f;

	public override CampaignTime GetTransitionTimeForEmbarking(MobileParty mobileParty)
	{
		if (!mobileParty.Anchor.IsValid)
		{
			return CampaignTime.Hours(48f);
		}
		float num = float.MaxValue;
		if (mobileParty.CurrentSettlement != null)
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			CampaignVec2 toPoint = mobileParty.Anchor.Position;
			num = mapDistanceModel.GetDistance(currentSettlement, in toPoint, isFromPort: true, MobileParty.NavigationType.Naval);
		}
		else if (mobileParty.EndPositionForNavigationTransition.IsValid())
		{
			MapDistanceModel mapDistanceModel2 = Campaign.Current.Models.MapDistanceModel;
			CampaignVec2 toPoint = mobileParty.Anchor.Position;
			CampaignVec2 toPoint2 = mobileParty.EndPositionForNavigationTransition;
			num = mapDistanceModel2.GetDistance(in toPoint, in toPoint2, MobileParty.NavigationType.Naval, out var _);
		}
		if (num < 10f)
		{
			return CampaignTime.Zero;
		}
		return CampaignTime.Hours(GetAnchorReachDurationInHours(num));
	}

	public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
	{
		if (mobileParty.IsInNavalAutoTravel)
		{
			return CampaignTime.Zero;
		}
		ExplainedNumber stat = new ExplainedNumber(2f);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.Unflinching, mobileParty, isPrimaryBonus: true, ref stat);
		return CampaignTime.Hours(stat.ResultNumber);
	}

	public override CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement)
	{
		AnchorPoint anchor = mobileParty.Anchor;
		float num = 0f;
		float distance = 0f;
		if (anchor.IsMovingToPoint)
		{
			num = (float)(anchor.ArrivalTime - CampaignTime.Now).ToHours;
			if (!anchor.IsTargetingSettlement(targetSettlement))
			{
				MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
				CampaignVec2 toPoint = anchor.GetTargetPosition();
				distance = mapDistanceModel.GetDistance(targetSettlement, in toPoint, isFromPort: true, MobileParty.NavigationType.Naval);
			}
		}
		else
		{
			if (!anchor.Position.IsValid())
			{
				return CampaignTime.Hours(48f);
			}
			MapDistanceModel mapDistanceModel2 = Campaign.Current.Models.MapDistanceModel;
			CampaignVec2 toPoint = anchor.Position;
			distance = mapDistanceModel2.GetDistance(targetSettlement, in toPoint, isFromPort: true, MobileParty.NavigationType.Naval);
		}
		ExplainedNumber stat = new ExplainedNumber(GetAnchorReachDurationInHours(distance));
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.ShoreMaster, mobileParty, isPrimaryBonus: true, ref stat);
		return CampaignTime.Hours(MBMath.ClampFloat(stat.ResultNumber + num, 3f, 48f));
	}

	private float GetAnchorReachDurationInHours(float distance)
	{
		distance = MathF.Pow(distance, 0.95f);
		return MBMath.ClampFloat(distance / 35f, 3f, 48f);
	}
}
