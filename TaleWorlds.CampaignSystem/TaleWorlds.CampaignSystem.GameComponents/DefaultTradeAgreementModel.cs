using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTradeAgreementModel : TradeAgreementModel
{
	private const float FirstDegreeNeighborScore = 1.6f;

	private const float SecondDegreeNeighborScore = 0.3f;

	private const float MaxPeaceDurationBonus = 20f;

	private const float RelationshipMultiplier = 0.25f;

	private const float MaxAssumedExposureBonus = 40f;

	private const int MaxReasonsInExplanation = 3;

	private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;

	private ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
	{
		get
		{
			if (_tradeAgreementsBehavior == null)
			{
				_tradeAgreementsBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			}
			return _tradeAgreementsBehavior;
		}
	}

	public override int GetInfluenceCostOfProposingTradeAgreement(Clan proposerClan)
	{
		return 200;
	}

	public override int GetMaximumTradeAgreementCount(Kingdom kingdom)
	{
		return 2;
	}

	public override bool CanMakeTradeAgreement(Kingdom kingdom, Kingdom other, bool checkOtherSideTradeSupport, out TextObject reason)
	{
		if (kingdom.IsAtWarWith(other))
		{
			reason = new TextObject("{=vo7kAlkR}The kingdoms are at war.");
			return false;
		}
		if (other.IsEliminated)
		{
			reason = new TextObject("{=ZeNt57yM}The kingdom is eliminated.");
			return false;
		}
		if (TradeAgreementsCampaignBehavior.HasTradeAgreement(kingdom, other))
		{
			reason = new TextObject("{=8HXcla1b}These kingdoms already have a trade agreement.");
			return false;
		}
		if (Kingdom.All.Count((Kingdom x) => x != kingdom && !x.IsEliminated && TradeAgreementsCampaignBehavior.HasTradeAgreement(kingdom, x)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(kingdom))
		{
			reason = new TextObject("{=DJ51OJWj}You already have maximum number of trade agreements.");
			return false;
		}
		if (Kingdom.All.Count((Kingdom x) => x != other && !x.IsEliminated && TradeAgreementsCampaignBehavior.HasTradeAgreement(other, x)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(kingdom))
		{
			reason = new TextObject("{=O6zpuLGa}{OTHER_KINGDOM} already has maximum number of trade agreements.");
			reason.SetTextVariable("OTHER_KINGDOM", other.Name);
			return false;
		}
		if (checkOtherSideTradeSupport && Campaign.Current.Models.TradeAgreementModel.GetScoreOfStartingTradeAgreement(kingdom, other, kingdom.RulingClan, out reason) < 50f)
		{
			return false;
		}
		reason = TextObject.GetEmpty();
		return true;
	}

	public override float GetScoreOfStartingTradeAgreement(Kingdom kingdom, Kingdom targetKingdom, Clan clan, out TextObject explanation)
	{
		ExplainedNumber explanation2 = new ExplainedNumber(0f, includeDescriptions: true);
		CampaignTime peaceDeclarationDate = Campaign.Current.FactionManager.GetStanceLinkInternal(kingdom, targetKingdom).PeaceDeclarationDate;
		float num = ((float)clan.Leader.GetRelation(targetKingdom.Leader) + (float)kingdom.Leader.GetRelation(targetKingdom.Leader) * 3f) * 0.25f;
		AddRelationshipEffectToTradeAgreementExplanationTooltip(num, ref explanation2);
		float num2 = MathF.Min((peaceDeclarationDate == CampaignTime.Zero) ? 0f : peaceDeclarationDate.ElapsedDaysUntilNow, 20f);
		AddRecentWarEffectToTradeAgreementExplanationTooltip(num2, peaceDeclarationDate, ref explanation2);
		float exposureScoreToOtherKingdom = GetExposureScoreToOtherKingdom(kingdom, targetKingdom);
		AddExposureEffectToTradeAgreementExplanationTooltip(exposureScoreToOtherKingdom, ref explanation2);
		float value = 15f + num + num2 + exposureScoreToOtherKingdom + kingdom.Leader.RandomFloatWithSeed((uint)CampaignTime.Now.ToDays, 0f, 5f);
		explanation = BuildExplanationForTradeAgreement(targetKingdom, explanation2);
		return MBMath.ClampFloat(value, 0f, 100f);
	}

	private void AddExposureEffectToTradeAgreementExplanationTooltip(float exposure, ref ExplainedNumber explanation)
	{
		if (exposure < 40f)
		{
			explanation.Add(40f - exposure, new TextObject("{=EapZFDGF}Limited shared borders"));
		}
	}

	private void AddRelationshipEffectToTradeAgreementExplanationTooltip(float relationshipScore, ref ExplainedNumber explanation)
	{
		if (relationshipScore < 0.25f * (float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit)
		{
			explanation.Add((float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit - relationshipScore, new TextObject("{=3YVDMg5X}Low relations between rulers"));
		}
	}

	private void AddRecentWarEffectToTradeAgreementExplanationTooltip(float warScore, CampaignTime peaceDeclarationDate, ref ExplainedNumber explanation)
	{
		if (warScore < 20f && peaceDeclarationDate != CampaignTime.Zero)
		{
			explanation.Add(20f - warScore, new TextObject("{=lDIz0nEY}Recent war"));
		}
	}

	private TextObject BuildExplanationForTradeAgreement(Kingdom other, ExplainedNumber tooltip)
	{
		TextObject textObject = new TextObject("{=fFuiV5EZ}{KINGDOM} will not agree to a trade agreement.{newline}{newline}Strongest Factors:{newline}{REASONS_BY_LINE}");
		textObject.SetTextVariable("KINGDOM", other.Name);
		textObject.SetTextVariable("REASONS_BY_LINE", GetTradeAgreementExplanation(tooltip));
		textObject.SetTextVariable("KINGDOM", other.Name);
		MBTextManager.SetTextVariable("newline", "\n");
		return textObject;
	}

	private TextObject GetTradeAgreementExplanation(ExplainedNumber explainedNumber)
	{
		List<TextObject> list = new List<TextObject>();
		foreach (var item2 in from x in explainedNumber.GetLines()
			orderby x.number descending
			select x)
		{
			string item = item2.name;
			TextObject textObject = new TextObject("{=!}{REASON}");
			textObject.SetTextVariable("REASON", item);
			list.Add(textObject);
			if (list.Count >= 3)
			{
				break;
			}
		}
		return GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}"));
	}

	public override CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom)
	{
		return CampaignTime.Years(1f);
	}

	private float GetExposureScoreToOtherKingdom(Kingdom kingdom1, Kingdom kingdom2)
	{
		HashSet<Settlement> hashSet = new HashSet<Settlement>();
		float num = 0f;
		float num2 = 0f;
		foreach (Town fief in kingdom1.Fiefs)
		{
			foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction != kingdom1 && !hashSet.Contains(neighborFortification))
				{
					if (neighborFortification.MapFaction == kingdom2)
					{
						num2 += 1.6f;
					}
					num += 1.6f;
					hashSet.Add(neighborFortification);
				}
			}
		}
		HashSet<Settlement> hashSet2 = new HashSet<Settlement>();
		foreach (Settlement item in hashSet)
		{
			foreach (Settlement neighborFortification2 in item.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification2.MapFaction != kingdom1 && !hashSet.Contains(neighborFortification2) && !hashSet2.Contains(neighborFortification2))
				{
					if (neighborFortification2.MapFaction == kingdom2)
					{
						num2 += 0.3f;
					}
					num += 0.3f;
					hashSet2.Add(neighborFortification2);
				}
			}
		}
		if (num2 < 0.3f)
		{
			return 0f;
		}
		return num;
	}
}
