using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultKingdomDecisionPermissionModel : KingdomDecisionPermissionModel
{
	public override bool IsPolicyDecisionAllowed(PolicyObject policy)
	{
		return true;
	}

	public override bool IsWarDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		return true;
	}

	public override bool IsPeaceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		if (!Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(kingdom1, kingdom2))
		{
			IAllianceCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			if (campaignBehavior == null || !campaignBehavior.IsAtWarByCallToWarAgreement(kingdom1, kingdom2))
			{
				if (!Campaign.Current.Models.DiplomacyModel.IsPeaceSuitable(kingdom1, kingdom2))
				{
					reason = new TextObject("{=JkQ7fmcX}The enemy is not open to negotiations.");
					return false;
				}
				return true;
			}
		}
		reason = new TextObject("{=eNPupZOp}These kingdoms can not declare peace at this time.");
		return false;
	}

	public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement)
	{
		return true;
	}

	public override bool IsExpulsionDecisionAllowed(Clan expelledClan)
	{
		return true;
	}

	public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom)
	{
		return true;
	}

	public override bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
	{
		reason = null;
		return true;
	}
}
