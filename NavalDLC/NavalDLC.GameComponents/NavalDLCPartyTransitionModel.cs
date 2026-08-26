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
		float distance;
		if (mobileParty.CurrentSettlement == null)
		{
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			CampaignVec2 toPoint = mobileParty.Anchor.GetInteractionPosition(mobileParty);
			distance = mapDistanceModel.GetDistance(mobileParty, in toPoint, MobileParty.NavigationType.Default, out var _);
		}
		else
		{
			MapDistanceModel mapDistanceModel2 = Campaign.Current.Models.MapDistanceModel;
			Settlement currentSettlement = mobileParty.CurrentSettlement;
			CampaignVec2 toPoint2 = mobileParty.Anchor.Position;
			distance = mapDistanceModel2.GetDistance(currentSettlement, in toPoint2, isFromPort: true, MobileParty.NavigationType.Naval);
		}
		float num = distance;
		if (num < 10f)
		{
			return CampaignTime.Zero;
		}
		return CampaignTime.Hours(GetAnchorReachDurationInHours(num));
	}

	public override CampaignTime GetTransitionTimeDisembarking(MobileParty mobileParty)
	{
		CampaignTime result = CampaignTime.Zero;
		if (!mobileParty.IsInRaftState)
		{
			result = CampaignTime.Hours(2f);
			if (mobileParty.HasPerk(NavalPerks.Shipmaster.Unflinching))
			{
				float num = NavalPerks.Shipmaster.Unflinching.PrimaryBonus * 100f;
				float num2 = (0f - num * 100f) / (100f + num);
				result = CampaignTime.Hours((float)result.ToHours * num2);
			}
		}
		return result;
	}

	public override CampaignTime GetFleetTravelTimeToSettlement(MobileParty mobileParty, Settlement targetSettlement)
	{
		AnchorPoint anchor = mobileParty.Anchor;
		if (anchor.Position.IsValid() || anchor.IsMovingToPoint)
		{
			float currentTravelTime = (anchor.IsMovingToPoint ? ((float)(anchor.ArrivalTime - CampaignTime.Now).ToHours) : 0f);
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			CampaignVec2 toPoint = (anchor.Position.IsValid() ? anchor.Position : anchor.TargetPosition);
			float distance = mapDistanceModel.GetDistance(targetSettlement, in toPoint, isFromPort: true, MobileParty.NavigationType.Naval);
			return CampaignTime.Hours(GetAnchorReachDurationInHours(distance, currentTravelTime));
		}
		CampaignTime result = CampaignTime.Hours(48f);
		if (mobileParty.HasPerk(NavalPerks.Shipmaster.ShoreMaster))
		{
			result = CampaignTime.Hours((float)result.ToHours * NavalPerks.Shipmaster.ShoreMaster.PrimaryBonus * -1f);
		}
		return result;
	}

	private float GetAnchorReachDurationInHours(float distance, float currentTravelTime = 0f)
	{
		distance = MathF.Pow(distance, 0.95f);
		return MBMath.ClampFloat(distance / 35f + currentTravelTime, 3f, 48f);
	}
}
