using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultAllianceModel : AllianceModel
{
	private const int _thresholdForCallToWarWallet = 100000;

	private const float SharedWarsEffect = 25f;

	private const int MaxRelationshipEffect = 20;

	private const int PotentialAllyBonus = 5;

	private const int TooPowerfulEffect = 20;

	private static readonly TextObject _sharedWarsText = new TextObject("{=Pg7bxzcY}Effect of shared wars");

	private static readonly TextObject _unsharedWarsText = new TextObject("{=9YFVXAZ3}Unshared wars");

	private static readonly TextObject _lackOfCommonEnemiesText = new TextObject("{=ugMAk9nb}Lack of common enemies");

	private static readonly TextObject _relationText = new TextObject("{=3YVDMg5X}Low relations between rulers");

	private static readonly TextObject _traitLevelText = new TextObject("{=iUURpauf}Effect of trait level");

	private static readonly TextObject _receivedTributeText = new TextObject("{=pV1LM0aE}Receiving tribute");

	private static readonly TextObject _paidTributeText = new TextObject("{=lyxa5jbH}Effect of tribute paying to the declared");

	private static readonly TextObject _threatenedText = new TextObject("{=92m8jTWP}Feels threatened");

	private static readonly TextObject _townsText = new TextObject("{=WaYxP7bX}Effect of having less than 3 towns");

	private static readonly TextObject _warWithTheirAllyText = new TextObject("{=EOkS8gn8}Effect of having an ally that we are at war with");

	private static readonly TextObject _allyWithTheirEnemyText = new TextObject("{=LhrU9cu3}Effect of having a ally that they are at war with");

	private static readonly TextObject _conflictingAllianceText = new TextObject("{=IeGgrMlx}Conflicting alliances");

	private const int MaxReasonsInExplanation = 3;

	public override CampaignTime MaxDurationOfAlliance => CampaignTime.Days(84f);

	public override CampaignTime MaxDurationOfWarParticipation => CampaignTime.Days(42f);

	public override int MaxNumberOfAlliances => 2;

	public override CampaignTime DurationForOffers => CampaignTime.Hours(24f);

	public override int GetCallToWarCost(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		int callToWarCostForCalledKingdom = GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
		int callToWarBudgetOfCallingKingdom = GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		if (callingKingdom == Clan.PlayerClan.Kingdom && callToWarBudgetOfCallingKingdom < 0)
		{
			return callToWarCostForCalledKingdom;
		}
		return (callToWarCostForCalledKingdom + callToWarBudgetOfCallingKingdom) / 2;
	}

	public override ExplainedNumber GetScoreOfStartingAlliance(Kingdom kingdomDeclaresAlliance, Kingdom kingdomDeclaredAlliance, IFaction evaluatingFaction, out TextObject explanationText, bool includeDescription = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescription);
		ExplainedNumber explanation = new ExplainedNumber(0f, includeDescription);
		int num = kingdomDeclaresAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction);
		int num2 = kingdomDeclaredAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction);
		if (num > 0 && num2 > 0)
		{
			int num3 = kingdomDeclaredAlliance.FactionsAtWarWith.Count((IFaction x) => x.IsKingdomFaction && kingdomDeclaresAlliance.FactionsAtWarWith.Contains(x));
			float num4 = (float)num3 / (float)num * 25f * 2f;
			result.Add(num4, _sharedWarsText);
			float num5 = (float)(num2 - num3) / (float)num2 * -25f;
			result.Add(num5, _unsharedWarsText);
			AddSharedWarsEffectToExplanationTooltip(num3, num4, num5, num2, num, ref explanation);
		}
		else
		{
			AddNoWarsEffectToExplanationTooltip(ref explanation);
		}
		int num6 = MBMath.ClampInt(kingdomDeclaredAlliance.Leader.GetRelation(kingdomDeclaresAlliance.Leader), -20, 20);
		result.Add(num6, _relationText);
		AddLowRelationEffectToExplanationTooltip(num6, ref explanation);
		int traitLevel = kingdomDeclaredAlliance.Leader.GetTraitLevel(DefaultTraits.Honor);
		result.Add(traitLevel * 10, _traitLevelText);
		AddHonorEffectToExplanationTooltip(traitLevel, kingdomDeclaredAlliance.Leader, ref explanation);
		int dailyTributeToPay = kingdomDeclaresAlliance.GetStanceWith(kingdomDeclaredAlliance).GetDailyTributeToPay(kingdomDeclaredAlliance);
		if (dailyTributeToPay > 0)
		{
			int num7 = 10000;
			float num8 = MBMath.Map(dailyTributeToPay, 0f, num7, 0f, 20f);
			AddTributeEffectToExplanationTooltip(num8, ref explanation);
			result.Add(0f - num8, _receivedTributeText);
		}
		int dailyTributeToPay2 = kingdomDeclaredAlliance.GetStanceWith(kingdomDeclaresAlliance).GetDailyTributeToPay(kingdomDeclaresAlliance);
		if (dailyTributeToPay2 > 0)
		{
			int num9 = 10000;
			result.Add(MBMath.Map(dailyTributeToPay2, 0f, num9, 0f, 20f), _paidTributeText);
		}
		if ((float)kingdomDeclaredAlliance.Fiefs.Count / (float)(Campaign.Current.AllTowns.Count + Campaign.Current.AllCastles.Count) > 0.3f)
		{
			AddTooPowerfulEffectToExplanationTooltip(ref explanation);
			result.Add(-20f, _threatenedText);
		}
		if (kingdomDeclaresAlliance.Fiefs.Count < 3)
		{
			result.Add(10f, _townsText);
		}
		int num10 = 0;
		foreach (Kingdom alliedKingdom in kingdomDeclaredAlliance.AlliedKingdoms)
		{
			if (alliedKingdom.IsAtWarWith(kingdomDeclaresAlliance))
			{
				num10 -= 5;
			}
		}
		if (num10 < 0)
		{
			result.Add(num10, _warWithTheirAllyText);
		}
		int num11 = 0;
		foreach (Kingdom alliedKingdom2 in kingdomDeclaresAlliance.AlliedKingdoms)
		{
			if (alliedKingdom2.IsAtWarWith(kingdomDeclaredAlliance))
			{
				num11 -= 5;
			}
		}
		if (num11 < 0)
		{
			result.Add(num11, _allyWithTheirEnemyText);
		}
		AddConflictingAlliancesEffectToExplanationTooltip(num11, num10, ref explanation);
		explanationText = BuildExplanationForAlliance(kingdomDeclaresAlliance, explanation);
		return result;
	}

	private void AddHonorEffectToExplanationTooltip(int honor, Hero ruler, ref ExplainedNumber explanation)
	{
		if (honor <= 0)
		{
			TextObject textObject = new TextObject("{=Gnkeh9HW}{RULER.NAME}{.o} honor");
			textObject.SetCharacterProperties("RULER", ruler.CharacterObject);
			explanation.Add(-honor * 10, textObject);
		}
	}

	private void AddConflictingAlliancesEffectToExplanationTooltip(int enemyAllyEffectOnOurSide, int enemyAllyEffectOnTheirSide, ref ExplainedNumber explanation)
	{
		if (enemyAllyEffectOnOurSide + enemyAllyEffectOnTheirSide < 0)
		{
			explanation.Add(-enemyAllyEffectOnOurSide - enemyAllyEffectOnTheirSide, _conflictingAllianceText);
		}
	}

	private void AddSharedWarsEffectToExplanationTooltip(int numberOfSharedWars, float sharedWarsEffect, float unsharedWarsEffect, int numberOfWarsOfDeclaredKingdom, int numberOfWarsOfDeclaringKingdom, ref ExplainedNumber explanation)
	{
		if (numberOfSharedWars < numberOfWarsOfDeclaredKingdom || numberOfSharedWars < numberOfWarsOfDeclaringKingdom)
		{
			if (numberOfSharedWars < numberOfWarsOfDeclaringKingdom)
			{
				unsharedWarsEffect -= 50f - sharedWarsEffect;
			}
			explanation.Add(0f - unsharedWarsEffect, _unsharedWarsText);
		}
	}

	private void AddNoWarsEffectToExplanationTooltip(ref ExplainedNumber explanation)
	{
		explanation.Add(50f, _lackOfCommonEnemiesText);
	}

	private void AddTributeEffectToExplanationTooltip(float tributeEffect, ref ExplainedNumber explanation)
	{
		if (tributeEffect > 0f)
		{
			explanation.Add(tributeEffect, _receivedTributeText);
		}
	}

	private void AddTooPowerfulEffectToExplanationTooltip(ref ExplainedNumber explanation)
	{
		explanation.Add(20f, _threatenedText);
	}

	private void AddLowRelationEffectToExplanationTooltip(int relationshipEffect, ref ExplainedNumber explanation)
	{
		if (relationshipEffect < 20)
		{
			explanation.Add(20 - relationshipEffect, _relationText);
		}
	}

	private TextObject BuildExplanationForAlliance(Kingdom other, ExplainedNumber tooltip)
	{
		TextObject textObject = TextObject.GetEmpty();
		if (tooltip.IncludeDescriptions)
		{
			textObject = new TextObject("{=eLJ4O0Yl}{KINGDOM} is not ready for an alliance.{newline}{newline}Strongest Factors:{newline}{REASONS_BY_LINE}");
			textObject.SetTextVariable("REASONS_BY_LINE", GetAllianceExplanation(tooltip));
			textObject.SetTextVariable("KINGDOM", other.Name);
			MBTextManager.SetTextVariable("newline", "\n");
		}
		return textObject;
	}

	private TextObject GetAllianceExplanation(ExplainedNumber explainedNumber)
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

	public override int GetInfluenceCostOfProposingStartingAlliance(Clan proposingClan)
	{
		return 200;
	}

	public override float GetScoreOfCallingToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
	{
		float num = 60f;
		reason = TextObject.GetEmpty();
		int callToWarBudgetOfCallingKingdom = GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		if (callToWarBudgetOfCallingKingdom < 0 || callingKingdom.CallToWarWallet < -100000 || (float)callToWarBudgetOfCallingKingdom * 1.5f < (float)callToWarCost)
		{
			return -100f;
		}
		if (callToWarCost == 0)
		{
			return 100f;
		}
		float num2 = (float)callToWarBudgetOfCallingKingdom / (float)callToWarCost;
		return num * num2;
	}

	public override float GetScoreOfJoiningWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
	{
		float num = 70f;
		reason = TextObject.GetEmpty();
		int callToWarCostForCalledKingdom = GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
		int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		if (callToWarCostForCalledKingdom == 0)
		{
			return 100f;
		}
		float value = (float)callToWarCost / (float)callToWarCostForCalledKingdom;
		value = MathF.Clamp(value, 1E-05f, 2f);
		return num * value;
	}

	public override int GetInfluenceCostOfCallingToWar(Clan proposingClan)
	{
		return 200;
	}

	private int GetCallToWarCostForCalledKingdom(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		TextObject reason;
		float scoreOfDeclaringWar = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(calledKingdom, kingdomToCallToWarAgainst, calledKingdom.RulingClan, out reason);
		float num = Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(kingdomToCallToWarAgainst) - scoreOfDeclaringWar;
		if (num <= 0f)
		{
			return 0;
		}
		float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(calledKingdom);
		float num2 = num / (valueOfSettlementsForFaction + 1f);
		double num3 = (double)calledKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
		return (int)((double)num2 * num3 * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
	}

	private int GetCallToWarBudgetOfCallingKingdom(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		float currentTotalStrength = callingKingdom.CurrentTotalStrength;
		float currentTotalStrength2 = calledKingdom.CurrentTotalStrength;
		float currentTotalStrength3 = kingdomToCallToWarAgainst.CurrentTotalStrength;
		double num = (double)callingKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
		float num2 = currentTotalStrength - currentTotalStrength3;
		if (num2 == 0f)
		{
			return int.MinValue;
		}
		return (int)((double)MathF.Clamp(0f - currentTotalStrength2 / num2, float.MinValue, 1f) * num * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
	}
}
