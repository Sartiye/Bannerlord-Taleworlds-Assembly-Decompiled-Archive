using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultClanFinanceModel : ClanFinanceModel
{
	private enum TransactionType
	{
		Income = 1,
		Both = 0,
		Expense = -1
	}

	public enum AssetIncomeType
	{
		Workshop,
		Caravan,
		Taxes,
		TributesEarned
	}

	private static readonly string _townTaxStr = "str_finance_town_tax";

	private static readonly string _partyIncomeStr = "str_finance_party_income";

	private static readonly string _caravanIncomeStr = "str_finance_caravan_income";

	private static readonly string _convoyIncomeStr = "str_finance_convoy_income";

	private static readonly string _projectsIncomeStr = "str_finance_projects_income";

	private static readonly string _partyExpensesStr = "str_finance_party_expenses";

	private static readonly string _shopIncomeStr = "str_finance_shop_income";

	private static readonly string _shopExpenseStr = "str_finance_shop_expense";

	private static readonly string _mercenaryStr = "str_finance_mercenary";

	private static readonly string _mercenaryExpensesStr = "str_finance_mercenary_expenses";

	private static readonly string _tributeExpensesStr = "str_finance_tribute_expenses";

	private static readonly string _tributeIncomeStr = "str_finance_tribute_income";

	private static readonly string _tributeIncomes = "str_finance_tribute_incomes";

	private static readonly string _callToWarExpenses = "str_finance_call_to_war_expenses";

	private static readonly string _callToWarIncomes = "str_finance_call_to_war_incomes";

	private static readonly string _settlementIncome = "str_finance_settlement_income";

	private static readonly string _mainPartywageStr = "str_finance_main_party_wage";

	private static readonly string _caravanAndPartyIncome = "str_finance_caravan_and_party_income";

	private static readonly string _garrisonAndPartyExpenses = "str_finance_garrison_and_party_expenses";

	private static readonly string _debtStr = "str_finance_debt";

	private static readonly string _kingdomSupport = "str_finance_kingdom_support";

	private static readonly string _kingdomBudgetStr = "str_finance_kingdom_budget";

	private static readonly string _tariffTaxStr = "str_finance_tariff_tax";

	private static readonly string _autoRecruitmentStr = "str_finance_auto_recruitment";

	private static readonly string _alley = "str_finance_alley";

	private const int PartyGoldIncomeThreshold = 10000;

	private const int payGarrisonWagesTreshold = 8000;

	private const int payClanPartiesTreshold = 4000;

	private const int payLeaderPartyWageTreshold = 2000;

	public override int PartyGoldLowerThreshold => 5000;

	public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		ExplainedNumber goldChange = new ExplainedNumber(0f, includeDescriptions);
		CalculateClanIncomeInternal(clan, ref goldChange, applyWithdrawals, includeDetails);
		CalculateClanExpensesInternal(clan, ref goldChange, applyWithdrawals, includeDetails);
		return goldChange;
	}

	public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		ExplainedNumber goldChange = new ExplainedNumber(0f, includeDescriptions);
		CalculateClanIncomeInternal(clan, ref goldChange, applyWithdrawals, includeDetails);
		return goldChange;
	}

	private void CalculateClanIncomeInternal(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false, bool includeDetails = false)
	{
		if (!clan.IsEliminated)
		{
			if (clan.Kingdom?.RulingClan == clan)
			{
				AddRulingClanIncome(clan, ref goldChange, applyWithdrawals, includeDetails);
			}
			if (clan != Clan.PlayerClan && (!clan.MapFaction.IsKingdomFaction || clan.IsUnderMercenaryService) && clan.Fiefs.Count == 0)
			{
				int num = clan.Tier * (80 + (clan.IsUnderMercenaryService ? 40 : 0));
				goldChange.Add(num);
			}
			AddMercenaryIncome(clan, ref goldChange, applyWithdrawals);
			AddSettlementIncome(clan, ref goldChange, applyWithdrawals, includeDetails);
			CalculateHeroIncomeFromWorkshops(clan.Leader, ref goldChange, applyWithdrawals);
			AddIncomeFromParties(clan, ref goldChange, applyWithdrawals, includeDetails);
			if (clan == Clan.PlayerClan)
			{
				AddPlayerClanIncomeFromOwnedAlleys(ref goldChange);
			}
			if (!clan.IsUnderMercenaryService)
			{
				AddIncomeFromTribute(clan, ref goldChange, applyWithdrawals, includeDetails);
				AddIncomeFromCallToWarAgrements(clan, ref goldChange, applyWithdrawals);
			}
			if (clan.Gold < 30000 && clan.Kingdom != null && clan.Leader != Hero.MainHero && !clan.IsUnderMercenaryService)
			{
				AddIncomeFromKingdomBudget(clan, ref goldChange, applyWithdrawals);
			}
			Hero leader = clan.Leader;
			if (leader != null && leader.GetPerkValue(DefaultPerks.Trade.SpringOfGold))
			{
				int num2 = MathF.Min(1000, MathF.Round((float)clan.Leader.Gold * DefaultPerks.Trade.SpringOfGold.PrimaryBonus));
				goldChange.Add(num2, DefaultPerks.Trade.SpringOfGold.Name);
			}
		}
	}

	public void CalculateClanExpensesInternal(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false, bool includeDetails = false)
	{
		AddExpensesFromPartiesAndGarrisons(clan, ref goldChange, applyWithdrawals, includeDetails);
		if (!clan.IsUnderMercenaryService)
		{
			AddExpensesForHiredMercenaries(clan, ref goldChange, applyWithdrawals);
			AddExpensesForTributes(clan, ref goldChange, applyWithdrawals);
		}
		AddExpensesForAutoRecruitment(clan, ref goldChange, applyWithdrawals);
		if (clan.Gold > 100000 && clan.Kingdom != null && clan.Leader != Hero.MainHero && !clan.IsUnderMercenaryService)
		{
			int num = (int)(((float)clan.Gold - 100000f) * 0.01f);
			if (applyWithdrawals)
			{
				clan.Kingdom.KingdomBudgetWallet += num;
			}
			goldChange.Add(-num, Game.Current.GameTextManager.FindText(_kingdomBudgetStr));
		}
		if (clan.DebtToKingdom > 0)
		{
			AddPaymentForDebts(clan, ref goldChange, applyWithdrawals);
		}
		if (Clan.PlayerClan == clan)
		{
			AddPlayerExpenseForWorkshops(ref goldChange);
		}
		if (!clan.IsUnderMercenaryService)
		{
			AddExpensesForCallToWarAgreements(clan, ref goldChange, applyWithdrawals);
		}
	}

	private void AddPlayerExpenseForWorkshops(ref ExplainedNumber goldChange)
	{
		int num = 0;
		foreach (Workshop ownedWorkshop in Hero.MainHero.OwnedWorkshops)
		{
			if (ownedWorkshop.Capital < Campaign.Current.Models.WorkshopModel.CapitalLowLimit)
			{
				num -= ownedWorkshop.Expense;
			}
		}
		goldChange.Add(num, Game.Current.GameTextManager.FindText(_shopExpenseStr));
	}

	public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
	{
		ExplainedNumber goldChange = new ExplainedNumber(0f, includeDescriptions);
		CalculateClanExpensesInternal(clan, ref goldChange, applyWithdrawals, includeDetails);
		return goldChange;
	}

	private void AddPaymentForDebts(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		if (clan.Kingdom != null && clan.DebtToKingdom > 0)
		{
			int num = clan.DebtToKingdom;
			if (applyWithdrawals)
			{
				num = MathF.Min(num, (int)((float)clan.Gold + goldChange.ResultNumber));
				clan.DebtToKingdom -= num;
			}
			goldChange.Add(-num, Game.Current.GameTextManager.FindText(_debtStr));
		}
	}

	private void AddRulingClanIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions);
		int num = 0;
		int num2 = 0;
		bool flag = clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax);
		float num3 = 0f;
		foreach (Town fief in clan.Fiefs)
		{
			num += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(fief).ResultNumber;
			num2++;
		}
		if (flag)
		{
			foreach (Village village in clan.Kingdom.Villages)
			{
				if (!village.IsOwnerUnassigned && village.Settlement.OwnerClan != clan && village.VillageState != Village.VillageStates.Looted && village.VillageState != Village.VillageStates.BeingRaided)
				{
					int num4 = (int)((float)village.TradeTaxAccumulated / RevenueSmoothenFraction());
					num3 += (float)num4 * 0.05f;
				}
			}
			if (num3 > 1E-05f)
			{
				explainedNumber.Add((int)num3, DefaultPolicies.LandTax.Name);
			}
		}
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.RulingClan == clan)
		{
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.WarTax))
			{
				int num5 = (int)((float)num * 0.05f);
				explainedNumber.Add(num5, DefaultPolicies.WarTax.Name);
			}
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.DebasementOfTheCurrency))
			{
				explainedNumber.Add(num2 * 100, DefaultPolicies.DebasementOfTheCurrency.Name);
			}
		}
		int num6 = 0;
		int num7 = 0;
		foreach (Settlement settlement in clan.Settlements)
		{
			if (!settlement.IsTown)
			{
				continue;
			}
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.RoadTolls))
			{
				int num8 = settlement.Town.TradeTaxAccumulated / 30;
				if (applyWithdrawals)
				{
					settlement.Town.TradeTaxAccumulated -= num8;
				}
				num6 += num8;
			}
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.StateMonopolies))
			{
				num7 += (int)((float)settlement.Town.Workshops.Sum((Workshop t) => t.ProfitMade) * 0.05f);
			}
			if (num6 > 0)
			{
				explainedNumber.Add(num6, DefaultPolicies.RoadTolls.Name);
			}
			if (num7 > 0)
			{
				explainedNumber.Add(num7, DefaultPolicies.StateMonopolies.Name);
			}
		}
		if (!explainedNumber.ResultNumber.ApproximatelyEqualsTo(0f))
		{
			if (!includeDetails)
			{
				goldChange.Add(explainedNumber.ResultNumber, GameTexts.FindText("str_policies"));
			}
			else
			{
				goldChange.AddFromExplainedNumber(explainedNumber, GameTexts.FindText("str_policies"));
			}
		}
	}

	private void AddExpensesForHiredMercenaries(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		Kingdom kingdom = clan.Kingdom;
		if (kingdom == null)
		{
			return;
		}
		float num = CalculateShareFactor(clan);
		if (kingdom.MercenaryWallet < 0)
		{
			int num2 = (int)((float)(-kingdom.MercenaryWallet) * num);
			ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num2, Game.Current.GameTextManager.FindText(_mercenaryExpensesStr));
			if (applyWithdrawals)
			{
				kingdom.MercenaryWallet += num2;
			}
		}
	}

	private void AddExpensesForTributes(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		Kingdom kingdom = clan.Kingdom;
		if (kingdom == null)
		{
			return;
		}
		float num = CalculateShareFactor(clan);
		if (kingdom.TributeWallet < 0)
		{
			int num2 = (int)((float)(-kingdom.TributeWallet) * num);
			ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num2, Game.Current.GameTextManager.FindText(_tributeExpensesStr));
			if (applyWithdrawals)
			{
				kingdom.TributeWallet += num2;
			}
		}
	}

	private void AddExpensesForCallToWarAgreements(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		Kingdom kingdom = clan.Kingdom;
		if (kingdom != null && kingdom.CallToWarWallet < 0)
		{
			float num = CalculateShareFactor(clan);
			int num2 = (int)((float)(-kingdom.CallToWarWallet) * num);
			int num3 = num2;
			int num4 = (int)((float)clan.Gold + goldChange.ResultNumber);
			if (applyWithdrawals && num4 - num3 < 5000)
			{
				num3 = MathF.Max(0, num4 - 5000);
				clan.DebtToKingdom += num2 - num3;
			}
			ApplyShareForExpenses(clan, ref goldChange, applyWithdrawals, num3, Game.Current.GameTextManager.FindText(_callToWarExpenses));
			if (applyWithdrawals)
			{
				kingdom.CallToWarWallet += num2;
			}
		}
	}

	private static void ApplyShareForExpenses(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, int expenseShare, TextObject mercenaryExpensesStr)
	{
		if (applyWithdrawals)
		{
			int num = (int)((float)clan.Gold + goldChange.ResultNumber);
			if (expenseShare > num)
			{
				int num2 = expenseShare - num;
				expenseShare = num;
				clan.DebtToKingdom += num2;
			}
		}
		goldChange.Add(-expenseShare, mercenaryExpensesStr);
	}

	private void AddSettlementIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions);
		foreach (Town fief in clan.Fiefs)
		{
			ExplainedNumber explainedNumber2 = Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(fief);
			ExplainedNumber explainedNumber3 = Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromTariffs(clan, fief, applyWithdrawals);
			int num = Campaign.Current.Models.ClanFinanceModel.CalculateTownIncomeFromProjects(fief);
			explainedNumber.Add((int)explainedNumber2.ResultNumber, Game.Current.GameTextManager.FindText(_townTaxStr), fief.Name);
			explainedNumber.Add((int)explainedNumber3.ResultNumber, Game.Current.GameTextManager.FindText(_tariffTaxStr), fief.Name);
			explainedNumber.Add(num, Game.Current.GameTextManager.FindText(_projectsIncomeStr));
			foreach (Village village in fief.Villages)
			{
				int num2 = CalculateVillageIncome(clan, village, applyWithdrawals);
				explainedNumber.Add(num2, village.Name);
			}
		}
		if (!includeDetails)
		{
			goldChange.Add(explainedNumber.ResultNumber, Game.Current.GameTextManager.FindText(_settlementIncome));
		}
		else
		{
			goldChange.AddFromExplainedNumber(explainedNumber, Game.Current.GameTextManager.FindText(_settlementIncome));
		}
	}

	public override ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false)
	{
		ExplainedNumber bonuses = new ExplainedNumber((int)((float)town.TradeTaxAccumulated / RevenueSmoothenFraction()));
		int num = MathF.Round(bonuses.ResultNumber);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.ContentTrades, town, ref bonuses);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Crossbow.Steady, town, ref bonuses);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.SaltTheEarth, town, ref bonuses);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.GivingHands, town, ref bonuses);
		CalculateSettlementProjectTariffBonuses(town, ref bonuses);
		if (applyWithdrawals)
		{
			town.TradeTaxAccumulated -= num;
			if (clan == Clan.PlayerClan)
			{
				CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.Taxes, (int)bonuses.ResultNumber);
			}
		}
		return bonuses;
	}

	private void CalculateSettlementProjectTariffBonuses(Town town, ref ExplainedNumber result)
	{
		town.AddEffectOfBuildings(BuildingEffectEnum.TariffIncome, ref result);
	}

	public override int CalculateTownIncomeFromProjects(Town town)
	{
		ExplainedNumber result = default(ExplainedNumber);
		if (town.CurrentDefaultBuilding != null && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.ArchitecturalCommisions))
		{
			result.Add((int)DefaultPerks.Engineering.ArchitecturalCommisions.SecondaryBonus);
		}
		town.AddEffectOfBuildings(BuildingEffectEnum.DenarByBoundVillageHeartPerDay, ref result);
		return (int)result.ResultNumber;
	}

	public override int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false)
	{
		int num = ((village.VillageState != Village.VillageStates.Looted && village.VillageState != Village.VillageStates.BeingRaided) ? ((int)((float)village.TradeTaxAccumulated / RevenueSmoothenFraction())) : 0);
		int num2 = num;
		if (clan.Kingdom != null && clan.Kingdom.RulingClan != clan && clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LandTax))
		{
			num -= (int)(0.05f * (float)num);
		}
		if (village.Bound.Town != null && village.Bound.Town.Governor != null && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Scouting.ForestKin))
		{
			num += MathF.Round((float)num * DefaultPerks.Scouting.ForestKin.SecondaryBonus);
		}
		if (village.Bound?.Town?.Governor != null && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Steward.Logistician))
		{
			num += MathF.Round((float)num * DefaultPerks.Steward.Logistician.SecondaryBonus);
		}
		if (applyWithdrawals)
		{
			village.TradeTaxAccumulated -= num2;
			if (clan == Clan.PlayerClan)
			{
				CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.Taxes, num);
			}
		}
		return num;
	}

	private static float CalculateShareFactor(Clan clan)
	{
		Kingdom kingdom = clan.Kingdom;
		int num = kingdom.Fiefs.Sum((Town x) => x.IsCastle ? 1 : 3) + 1 + kingdom.Clans.Count;
		return (float)(clan.Fiefs.Sum((Town x) => x.IsCastle ? 1 : 3) + ((clan == kingdom.RulingClan) ? 1 : 0) + 1) / (float)num;
	}

	private void AddMercenaryIncome(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		if (clan.IsUnderMercenaryService && clan.Leader != null && clan.Kingdom != null)
		{
			int num = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) * clan.MercenaryAwardMultiplier;
			if (applyWithdrawals)
			{
				clan.Kingdom.MercenaryWallet -= num;
			}
			goldChange.Add(num, Game.Current.GameTextManager.FindText(_mercenaryStr));
		}
	}

	private void AddIncomeFromKingdomBudget(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = ((clan.Gold < 5000) ? 2000 : ((clan.Gold < 10000) ? 1500 : ((clan.Gold < 20000) ? 1000 : 500)));
		num *= ((clan.Kingdom.KingdomBudgetWallet <= 1000000) ? 1 : 2);
		num *= ((clan.Leader != clan.Kingdom.Leader) ? 1 : 2);
		int num2 = MathF.Min(clan.Kingdom.KingdomBudgetWallet, num);
		if (applyWithdrawals)
		{
			clan.Kingdom.KingdomBudgetWallet -= num2;
		}
		goldChange.Add(num2, Game.Current.GameTextManager.FindText(_kingdomSupport));
	}

	private void AddPlayerClanIncomeFromOwnedAlleys(ref ExplainedNumber goldChange)
	{
		int num = 0;
		foreach (Alley ownedAlley in Hero.MainHero.OwnedAlleys)
		{
			num += Campaign.Current.Models.AlleyModel.GetDailyIncomeOfAlley(ownedAlley);
		}
		goldChange.Add(num, Game.Current.GameTextManager.FindText(_alley));
	}

	private void AddIncomeFromTribute(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions);
		IFaction mapFaction = clan.MapFaction;
		float num = 1f;
		if (clan.Kingdom != null)
		{
			num = CalculateShareFactor(clan);
		}
		foreach (StanceLink stance in FactionHelper.GetStances(mapFaction))
		{
			IFaction faction = ((stance.Faction1 == mapFaction) ? stance.Faction2 : stance.Faction1);
			int dailyTributeToPay = stance.GetDailyTributeToPay(mapFaction);
			if (mapFaction.IsAtWarWith(faction) || dailyTributeToPay >= 0)
			{
				continue;
			}
			int num2 = (int)((float)dailyTributeToPay * num);
			if (applyWithdrawals)
			{
				faction.TributeWallet += num2;
				if (stance.Faction1 == mapFaction)
				{
					stance.TotalTributePaidFrom2To1 += -num2;
				}
				if (stance.Faction2 == mapFaction)
				{
					stance.TotalTributePaidFrom1To2 += -num2;
				}
				CampaignEventDispatcher.Instance.OnClanEarnedGoldFromTribute(clan, faction);
				if (clan == Clan.PlayerClan)
				{
					CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.TributesEarned, -num2);
				}
			}
			explainedNumber.Add(-num2, Game.Current.GameTextManager.FindText(_tributeIncomeStr), faction.InformalName);
		}
		if (!includeDetails)
		{
			goldChange.Add(explainedNumber.ResultNumber, Game.Current.GameTextManager.FindText(_tributeIncomes));
		}
		else
		{
			goldChange.AddFromExplainedNumber(explainedNumber, Game.Current.GameTextManager.FindText(_tributeIncomes));
		}
	}

	private void AddIncomeFromCallToWarAgrements(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		if (clan.Kingdom == null || clan.Kingdom.CallToWarWallet <= 0)
		{
			return;
		}
		float num = CalculateShareFactor(clan);
		int num2 = (int)((float)clan.Kingdom.CallToWarWallet * num);
		if (applyWithdrawals)
		{
			clan.Kingdom.CallToWarWallet -= num2;
			if (clan == Clan.PlayerClan)
			{
				CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.TributesEarned, num2);
			}
		}
		goldChange.Add(num2, Game.Current.GameTextManager.FindText(_callToWarIncomes));
	}

	private void AddIncomeFromParties(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions);
		foreach (Hero aliveLord in clan.AliveLords)
		{
			foreach (CaravanPartyComponent ownedCaravan in aliveLord.OwnedCaravans)
			{
				if (ownedCaravan.MobileParty.IsActive && ownedCaravan.MobileParty.LeaderHero != clan.Leader && (ownedCaravan.MobileParty.IsLordParty || ownedCaravan.MobileParty.IsGarrison || ownedCaravan.MobileParty.IsCaravan))
				{
					int num = AddIncomeFromParty(ownedCaravan.MobileParty, clan, ref goldChange, applyWithdrawals);
					explainedNumber.Add(num, Game.Current.GameTextManager.FindText(ownedCaravan.MobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability ? _convoyIncomeStr : _caravanIncomeStr), (ownedCaravan.Leader != null) ? ownedCaravan.Leader.Name : ownedCaravan.Name);
				}
			}
		}
		foreach (Hero companion in clan.Companions)
		{
			foreach (CaravanPartyComponent ownedCaravan2 in companion.OwnedCaravans)
			{
				if (ownedCaravan2.MobileParty.IsActive && ownedCaravan2.MobileParty.LeaderHero != clan.Leader && (ownedCaravan2.MobileParty.IsLordParty || ownedCaravan2.MobileParty.IsGarrison || ownedCaravan2.MobileParty.IsCaravan))
				{
					int num2 = AddIncomeFromParty(ownedCaravan2.MobileParty, clan, ref goldChange, applyWithdrawals);
					explainedNumber.Add(num2, Game.Current.GameTextManager.FindText(ownedCaravan2.MobileParty.CaravanPartyComponent.CanHaveNavalNavigationCapability ? _convoyIncomeStr : _caravanIncomeStr), (ownedCaravan2.Leader != null) ? ownedCaravan2.Leader.Name : ownedCaravan2.Name);
				}
			}
		}
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.IsActive && warPartyComponent.MobileParty.LeaderHero != clan.Leader && (warPartyComponent.MobileParty.IsLordParty || warPartyComponent.MobileParty.IsGarrison || warPartyComponent.MobileParty.IsCaravan))
			{
				int num3 = AddIncomeFromParty(warPartyComponent.MobileParty, clan, ref goldChange, applyWithdrawals);
				explainedNumber.Add(num3, Game.Current.GameTextManager.FindText(_partyIncomeStr), warPartyComponent.MobileParty.Name);
			}
		}
		if (!includeDetails)
		{
			goldChange.Add(explainedNumber.ResultNumber, Game.Current.GameTextManager.FindText(_caravanAndPartyIncome));
		}
		else
		{
			goldChange.AddFromExplainedNumber(explainedNumber, Game.Current.GameTextManager.FindText(_caravanAndPartyIncome));
		}
	}

	private int AddIncomeFromParty(MobileParty party, Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = 0;
		if (party.IsActive && party.LeaderHero != clan.Leader && (party.IsLordParty || party.IsGarrison || party.IsCaravan))
		{
			int partyTradeGold = party.PartyTradeGold;
			if (partyTradeGold > 10000)
			{
				num = (partyTradeGold - 10000) / 10;
				if (applyWithdrawals)
				{
					party.PartyTradeGold -= num;
					if (party.LeaderHero != null && num > 0)
					{
						SkillLevelingManager.OnTradeProfitMade(party.LeaderHero, num);
					}
					if (party.Party.Owner?.Clan?.Leader != null && party.IsCaravan && party.Party.Owner.Clan.Leader.GetPerkValue(DefaultPerks.Trade.GreatInvestor) && num > 0)
					{
						party.Party.Owner.Clan.AddRenown(DefaultPerks.Trade.GreatInvestor.PrimaryBonus);
					}
					if (clan == Clan.PlayerClan && party.IsCaravan)
					{
						CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.Caravan, num);
					}
				}
			}
		}
		return num;
	}

	private void AddExpensesFromPartiesAndGarrisons(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, bool includeDetails)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, goldChange.IncludeDescriptions);
		int num = AddExpenseFromLeaderParty(clan, goldChange, applyWithdrawals);
		explainedNumber.Add(num, Game.Current.GameTextManager.FindText(_mainPartywageStr));
		foreach (Hero aliveLord in clan.AliveLords)
		{
			foreach (CaravanPartyComponent ownedCaravan in aliveLord.OwnedCaravans)
			{
				if (ownedCaravan.MobileParty.IsActive && ownedCaravan.MobileParty.LeaderHero != clan.Leader)
				{
					int num2 = AddPartyExpense(ownedCaravan.MobileParty, clan, goldChange, applyWithdrawals);
					explainedNumber.Add(num2, Game.Current.GameTextManager.FindText(_partyExpensesStr), ownedCaravan.Name);
				}
			}
		}
		foreach (Hero companion in clan.Companions)
		{
			foreach (CaravanPartyComponent ownedCaravan2 in companion.OwnedCaravans)
			{
				int num3 = AddPartyExpense(ownedCaravan2.MobileParty, clan, goldChange, applyWithdrawals);
				explainedNumber.Add(num3, Game.Current.GameTextManager.FindText(_partyExpensesStr), ownedCaravan2.Name);
			}
		}
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.IsActive && warPartyComponent.MobileParty.LeaderHero != clan.Leader)
			{
				int num4 = AddPartyExpense(warPartyComponent.MobileParty, clan, goldChange, applyWithdrawals);
				explainedNumber.Add(num4, Game.Current.GameTextManager.FindText(_partyExpensesStr), warPartyComponent.Name);
			}
		}
		foreach (Town fief in clan.Fiefs)
		{
			if (fief.GarrisonParty != null && fief.GarrisonParty.IsActive)
			{
				int num5 = AddPartyExpense(fief.GarrisonParty, clan, goldChange, applyWithdrawals);
				TextObject textObject = new TextObject("{=fsTBcLvA}{SETTLEMENT} Garrison");
				textObject.SetTextVariable("SETTLEMENT", fief.Name);
				explainedNumber.Add(num5, Game.Current.GameTextManager.FindText(_partyExpensesStr), textObject);
			}
		}
		if (!includeDetails)
		{
			goldChange.Add(explainedNumber.ResultNumber, Game.Current.GameTextManager.FindText(_garrisonAndPartyExpenses));
		}
		else
		{
			goldChange.AddFromExplainedNumber(explainedNumber, Game.Current.GameTextManager.FindText(_garrisonAndPartyExpenses));
		}
	}

	private void AddExpensesForAutoRecruitment(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
	{
		int num = clan.AutoRecruitmentExpenses / 5;
		if (applyWithdrawals)
		{
			clan.AutoRecruitmentExpenses -= num;
		}
		goldChange.Add(-num, Game.Current.GameTextManager.FindText(_autoRecruitmentStr));
	}

	private int AddExpenseFromLeaderParty(Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
	{
		MobileParty mobileParty = clan.Leader?.PartyBelongedTo;
		if (mobileParty != null)
		{
			int num = clan.Gold + (int)goldChange.ResultNumber;
			if (num < 2000 && applyWithdrawals && clan != Clan.PlayerClan)
			{
				num = 0;
			}
			return -CalculatePartyWage(mobileParty, num, applyWithdrawals);
		}
		return 0;
	}

	private int AddPartyExpense(MobileParty party, Clan clan, ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = clan.Gold + (int)goldChange.ResultNumber;
		int num2 = num;
		if (num < (party.IsGarrison ? 8000 : 4000) && applyWithdrawals && clan != Clan.PlayerClan)
		{
			num2 = ((party.LeaderHero != null && party.PartyTradeGold < 500) ? MathF.Min(num, 250) : 0);
		}
		int num3 = CalculatePartyWage(party, num2, applyWithdrawals);
		int partyTradeGold = party.PartyTradeGold;
		if (applyWithdrawals)
		{
			if (party.IsLordParty && party.LeaderHero == null)
			{
				party.ActualClan.Leader.Gold -= num3;
			}
			else
			{
				party.PartyTradeGold -= num3;
			}
		}
		partyTradeGold -= num3;
		if (partyTradeGold < PartyGoldLowerThreshold)
		{
			int num4 = PartyGoldLowerThreshold - partyTradeGold;
			if (party.IsLordParty && party.LeaderHero == null)
			{
				num4 = num3;
			}
			if (applyWithdrawals)
			{
				num4 = MathF.Min(num4, num2);
				party.PartyTradeGold += num4;
			}
			return -num4;
		}
		return 0;
	}

	public override int CalculateOwnerIncomeFromCaravan(MobileParty caravan)
	{
		return (int)((float)MathF.Max(0, caravan.PartyTradeGold - Campaign.Current.Models.CaravanModel.GetInitialTradeGold(caravan.Owner, caravan.CaravanPartyComponent.CanHaveNavalNavigationCapability, eliteCaravan: false)) / RevenueSmoothenFraction());
	}

	public override int CalculateOwnerIncomeFromWorkshop(Workshop workshop)
	{
		return (int)((float)MathF.Max(0, workshop.ProfitMade) / RevenueSmoothenFraction());
	}

	private void CalculateHeroIncomeFromAssets(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = 0;
		foreach (CaravanPartyComponent ownedCaravan in hero.OwnedCaravans)
		{
			if (ownedCaravan.MobileParty.PartyTradeGold > Campaign.Current.Models.CaravanModel.GetInitialTradeGold(ownedCaravan.Owner, ownedCaravan.CanHaveNavalNavigationCapability, eliteCaravan: false))
			{
				int num2 = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromCaravan(ownedCaravan.MobileParty);
				if (applyWithdrawals)
				{
					ownedCaravan.MobileParty.PartyTradeGold -= num2;
					SkillLevelingManager.OnTradeProfitMade(hero, num2);
				}
				if (num2 > 0)
				{
					num += num2;
				}
			}
		}
		goldChange.Add(num, Game.Current.GameTextManager.FindText(_caravanIncomeStr));
		CalculateHeroIncomeFromWorkshops(hero, ref goldChange, applyWithdrawals);
		if (hero.CurrentSettlement == null)
		{
			return;
		}
		foreach (Alley alley in hero.CurrentSettlement.Alleys)
		{
			if (alley.Owner == hero)
			{
				goldChange.Add(30f, alley.Name);
			}
		}
	}

	private void CalculateHeroIncomeFromWorkshops(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
	{
		int num = 0;
		int num2 = 0;
		foreach (Workshop ownedWorkshop in hero.OwnedWorkshops)
		{
			int num3 = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(ownedWorkshop);
			num += num3;
			if (applyWithdrawals && num3 > 0)
			{
				ownedWorkshop.ChangeGold(-num3);
				if (hero == Hero.MainHero)
				{
					CampaignEventDispatcher.Instance.OnPlayerEarnedGoldFromAsset(AssetIncomeType.Workshop, num3);
				}
			}
			if (num3 > 0)
			{
				num2++;
			}
		}
		goldChange.Add(num, Game.Current.GameTextManager.FindText(_shopIncomeStr));
		if (hero.Clan != null && (hero.Clan.Leader?.GetPerkValue(DefaultPerks.Trade.ArtisanCommunity) ?? false) && applyWithdrawals && num2 > 0)
		{
			hero.Clan.AddRenown((float)num2 * DefaultPerks.Trade.ArtisanCommunity.PrimaryBonus);
		}
	}

	public override float RevenueSmoothenFraction()
	{
		return 5f;
	}

	private int CalculatePartyWage(MobileParty mobileParty, int budget, bool applyWithdrawals)
	{
		int totalWage = mobileParty.TotalWage;
		int num = totalWage;
		if (applyWithdrawals)
		{
			num = MathF.Min(totalWage, budget);
			ApplyMoraleEffect(mobileParty, totalWage, num);
		}
		return num;
	}

	public override int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals)
	{
		ExplainedNumber goldChange = new ExplainedNumber(0f, includeDescriptions: false, null);
		CalculateHeroIncomeFromAssets(hero, ref goldChange, applyWithdrawals);
		return (int)goldChange.ResultNumber;
	}

	private static void ApplyMoraleEffect(MobileParty mobileParty, int wage, int paymentAmount)
	{
		if (paymentAmount < wage && wage > 0)
		{
			float num = 1f - (float)paymentAmount / (float)wage;
			float num2 = (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * num;
			if (mobileParty.HasUnpaidWages < num)
			{
				num2 += (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * (num - mobileParty.HasUnpaidWages);
			}
			mobileParty.RecentEventsMorale += num2;
			mobileParty.HasUnpaidWages = num;
			MBTextManager.SetTextVariable("reg1", MathF.Round(MathF.Abs(num2), 1));
			if (mobileParty == MobileParty.MainParty)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_party_loses_moral_due_to_insufficent_funds"));
			}
		}
		else
		{
			mobileParty.HasUnpaidWages = 0f;
		}
	}
}
