using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomTruceItemVM : KingdomDiplomacyItemVM
{
	private readonly Action<KingdomDiplomacyItemVM> _onSelection;

	private int _tributePaid;

	private bool _hasTradeAgreement;

	private bool _hasAlliance;

	private string _tradeAgreementEndTimeStr;

	private string _allianceEndTimeStr;

	[DataSourceProperty]
	public int TributePaid
	{
		get
		{
			return _tributePaid;
		}
		set
		{
			if (value != _tributePaid)
			{
				_tributePaid = value;
				OnPropertyChangedWithValue(value, "TributePaid");
			}
		}
	}

	[DataSourceProperty]
	public bool HasTradeAgreement
	{
		get
		{
			return _hasTradeAgreement;
		}
		set
		{
			if (value != _hasTradeAgreement)
			{
				_hasTradeAgreement = value;
				OnPropertyChangedWithValue(value, "HasTradeAgreement");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAlliance
	{
		get
		{
			return _hasAlliance;
		}
		set
		{
			if (value != _hasAlliance)
			{
				_hasAlliance = value;
				OnPropertyChangedWithValue(value, "HasAlliance");
			}
		}
	}

	[DataSourceProperty]
	public string AllianceEndTimeStr
	{
		get
		{
			return _allianceEndTimeStr;
		}
		set
		{
			if (value != _allianceEndTimeStr)
			{
				_allianceEndTimeStr = value;
				OnPropertyChangedWithValue(value, "AllianceEndTimeStr");
			}
		}
	}

	[DataSourceProperty]
	public string TradeAgreementEndTimeStr
	{
		get
		{
			return _tradeAgreementEndTimeStr;
		}
		set
		{
			if (value != _tradeAgreementEndTimeStr)
			{
				_tradeAgreementEndTimeStr = value;
				OnPropertyChangedWithValue(value, "TradeAgreementEndTimeStr");
			}
		}
	}

	public KingdomTruceItemVM(IFaction faction1, IFaction faction2, Action<KingdomDiplomacyItemVM> onSelection)
		: base(faction1, faction2)
	{
		_onSelection = onSelection;
		UpdateDiplomacyProperties();
	}

	protected override void OnSelect()
	{
		UpdateDiplomacyProperties();
		_onSelection(this);
	}

	protected override void UpdateDiplomacyProperties()
	{
		base.UpdateDiplomacyProperties();
		base.Stats.Add(new KingdomWarComparableStatVM((int)Faction1.CurrentTotalStrength, (int)Faction2.CurrentTotalStrength, GameTexts.FindText("str_total_strength"), _faction1Color, _faction2Color, 10000));
		base.Stats.Add(new KingdomWarComparableStatVM(_faction1Towns.Count, _faction2Towns.Count, GameTexts.FindText("str_towns"), _faction1Color, _faction2Color, 25, new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(_faction1Towns, Faction1.Name, isTown: true)), new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(_faction2Towns, Faction2.Name, isTown: true))));
		base.Stats.Add(new KingdomWarComparableStatVM(_faction1Castles.Count, _faction2Castles.Count, GameTexts.FindText("str_castles"), _faction1Color, _faction2Color, 25, new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(_faction1Castles, Faction1.Name, isTown: false)), new BasicTooltipViewModel(() => CampaignUIHelper.GetTruceOwnedSettlementsTooltip(_faction2Castles, Faction2.Name, isTown: false))));
		StanceLink stanceWith = _playerKingdom.GetStanceWith(Faction2);
		TributePaid = stanceWith.GetDailyTributeToPay(_playerKingdom);
		if (stanceWith.IsNeutral && TributePaid != 0)
		{
			base.Stats.Add(new KingdomWarComparableStatVM(TaleWorlds.Library.MathF.Max(stanceWith.GetTotalTributePaid(Faction2), 0), TaleWorlds.Library.MathF.Max(stanceWith.GetTotalTributePaid(Faction1), 0), GameTexts.FindText("str_comparison_tribute_received"), _faction1Color, _faction2Color, 10000));
		}
		if (Faction1.IsKingdomFaction && Faction2.IsKingdomFaction)
		{
			HasTradeAgreement = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>()?.HasTradeAgreement(Faction1 as Kingdom, Faction2 as Kingdom) ?? false;
			HasAlliance = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>()?.IsAllyWithKingdom(Faction1 as Kingdom, Faction2 as Kingdom) ?? false;
			if (HasTradeAgreement)
			{
				int num = TaleWorlds.Library.MathF.Ceiling(Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>().GetTradeAgreementEndDate(Faction1 as Kingdom, Faction2 as Kingdom).RemainingDaysFromNow);
				TradeAgreementEndTimeStr = new TextObject("{=6ayEZQE1}Expires in {DAYS} {?DAYS > 1}days{?}day{\\?}.").SetTextVariable("DAYS", num.ToString()).ToString();
			}
			else
			{
				TradeAgreementEndTimeStr = null;
			}
			if (HasAlliance)
			{
				int num2 = TaleWorlds.Library.MathF.Ceiling(Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().GetAllianceEndDate(Faction1 as Kingdom, Faction2 as Kingdom).RemainingDaysFromNow);
				AllianceEndTimeStr = new TextObject("{=6ayEZQE1}Expires in {DAYS} {?DAYS > 1}days{?}day{\\?}.").SetTextVariable("DAYS", num2.ToString()).ToString();
			}
			else
			{
				AllianceEndTimeStr = null;
			}
		}
		else
		{
			HasTradeAgreement = false;
			HasAlliance = false;
			TradeAgreementEndTimeStr = null;
			AllianceEndTimeStr = null;
		}
	}
}
