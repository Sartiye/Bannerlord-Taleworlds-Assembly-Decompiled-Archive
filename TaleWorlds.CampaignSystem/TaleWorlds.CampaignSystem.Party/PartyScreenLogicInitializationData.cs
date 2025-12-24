using Helpers;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party;

public struct PartyScreenLogicInitializationData
{
	public TroopRoster LeftMemberRoster;

	public TroopRoster LeftPrisonerRoster;

	public TroopRoster RightMemberRoster;

	public TroopRoster RightPrisonerRoster;

	public PartyBase LeftOwnerParty;

	public PartyBase RightOwnerParty;

	public TextObject LeftPartyName;

	public TextObject RightPartyName;

	public TextObject Header;

	public Hero LeftLeaderHero;

	public Hero RightLeaderHero;

	public int LeftPartyMembersSizeLimit;

	public int LeftPartyPrisonersSizeLimit;

	public int RightPartyMembersSizeLimit;

	public int RightPartyPrisonersSizeLimit;

	public PartyPresentationDoneButtonDelegate PartyPresentationDoneButtonDelegate;

	public PartyPresentationDoneButtonConditionDelegate PartyPresentationDoneButtonConditionDelegate;

	public PartyPresentationCancelButtonActivateDelegate PartyPresentationCancelButtonActivateDelegate;

	public IsTroopTransferableDelegate TroopTransferableDelegate;

	public CanTalkToHeroDelegate CanTalkToTroopDelegate;

	public PartyPresentationCancelButtonDelegate PartyPresentationCancelButtonDelegate;

	public PartyScreenClosedDelegate PartyScreenClosedDelegate;

	public bool DoNotApplyGoldTransactions;

	public bool IsDismissMode;

	public bool TransferHealthiesGetWoundedsFirst;

	public bool IsTroopUpgradesDisabled;

	public bool ShowProgressBar;

	public int QuestModeWageDaysMultiplier;

	public PartyScreenLogic.TransferState MemberTransferState;

	public PartyScreenLogic.TransferState PrisonerTransferState;

	public PartyScreenLogic.TransferState AccompanyingTransferState;

	public PartyScreenHelper.PartyScreenMode PartyScreenMode;

	public static PartyScreenLogicInitializationData CreateBasicInitDataWithMainParty(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, PartyScreenLogic.TransferState accompanyingTransferState, IsTroopTransferableDelegate troopTransferableDelegate, PartyScreenHelper.PartyScreenMode partyScreenMode, PartyBase leftOwnerParty = null, TextObject leftPartyName = null, TextObject header = null, Hero leftLeaderHero = null, int leftPartyMembersSizeLimit = 0, int leftPartyPrisonersSizeLimit = 0, PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = null, PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = null, PartyPresentationCancelButtonDelegate partyPresentationCancelButtonDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null, PartyScreenClosedDelegate partyScreenClosedDelegate = null, bool isDismissMode = false, bool transferHealthiesGetWoundedsFirst = false, bool isTroopUpgradesDisabled = false, bool showProgressBar = false, int questModeWageDaysMultiplier = 0)
	{
		PartyScreenLogicInitializationData result = default(PartyScreenLogicInitializationData);
		result.LeftOwnerParty = leftOwnerParty;
		result.RightOwnerParty = PartyBase.MainParty;
		result.LeftMemberRoster = leftMemberRoster;
		result.LeftPrisonerRoster = leftPrisonerRoster;
		result.RightMemberRoster = PartyBase.MainParty.MemberRoster;
		result.RightPrisonerRoster = PartyBase.MainParty.PrisonRoster;
		result.LeftLeaderHero = leftLeaderHero;
		result.RightLeaderHero = PartyBase.MainParty.LeaderHero;
		result.LeftPartyMembersSizeLimit = leftPartyMembersSizeLimit;
		result.LeftPartyPrisonersSizeLimit = leftPartyPrisonersSizeLimit;
		result.RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit;
		result.RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit;
		result.LeftPartyName = leftPartyName;
		result.RightPartyName = PartyBase.MainParty.Name;
		result.TroopTransferableDelegate = troopTransferableDelegate;
		result.PartyScreenMode = partyScreenMode;
		result.PartyPresentationDoneButtonDelegate = partyPresentationDoneButtonDelegate;
		result.PartyPresentationDoneButtonConditionDelegate = partyPresentationDoneButtonConditionDelegate;
		result.PartyPresentationCancelButtonActivateDelegate = partyPresentationCancelButtonActivateDelegate;
		result.PartyPresentationCancelButtonDelegate = partyPresentationCancelButtonDelegate;
		result.IsDismissMode = isDismissMode;
		result.IsTroopUpgradesDisabled = isTroopUpgradesDisabled;
		result.Header = header;
		result.PartyScreenClosedDelegate = partyScreenClosedDelegate;
		result.TransferHealthiesGetWoundedsFirst = transferHealthiesGetWoundedsFirst;
		result.ShowProgressBar = showProgressBar;
		result.MemberTransferState = memberTransferState;
		result.PrisonerTransferState = prisonerTransferState;
		result.AccompanyingTransferState = accompanyingTransferState;
		result.QuestModeWageDaysMultiplier = questModeWageDaysMultiplier;
		return result;
	}

	public static PartyScreenLogicInitializationData CreateBasicInitDataWithMainPartyAndOther(MobileParty party, PartyScreenLogic.TransferState memberTransferState, PartyScreenLogic.TransferState prisonerTransferState, PartyScreenLogic.TransferState accompanyingTransferState, IsTroopTransferableDelegate troopTransferableDelegate, PartyScreenHelper.PartyScreenMode partyScreenMode, TextObject header = null, PartyPresentationDoneButtonDelegate partyPresentationDoneButtonDelegate = null, PartyPresentationDoneButtonConditionDelegate partyPresentationDoneButtonConditionDelegate = null, PartyPresentationCancelButtonDelegate partyPresentationCancelButtonDelegate = null, PartyPresentationCancelButtonActivateDelegate partyPresentationCancelButtonActivateDelegate = null, PartyScreenClosedDelegate partyScreenClosedDelegate = null, bool isDismissMode = false, bool transferHealthiesGetWoundedsFirst = false, bool isTroopUpgradesDisabled = true, bool showProgressBar = false)
	{
		PartyScreenLogicInitializationData result = default(PartyScreenLogicInitializationData);
		result.LeftOwnerParty = party.Party;
		result.RightOwnerParty = PartyBase.MainParty;
		result.LeftMemberRoster = party.MemberRoster;
		result.LeftPrisonerRoster = party.PrisonRoster;
		result.RightMemberRoster = PartyBase.MainParty.MemberRoster;
		result.RightPrisonerRoster = PartyBase.MainParty.PrisonRoster;
		result.LeftLeaderHero = party.LeaderHero;
		result.RightLeaderHero = PartyBase.MainParty.LeaderHero;
		result.LeftPartyMembersSizeLimit = party.Party.PartySizeLimit;
		result.LeftPartyPrisonersSizeLimit = party.Party.PrisonerSizeLimit;
		result.RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit;
		result.RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit;
		result.LeftPartyName = party.Name;
		result.RightPartyName = PartyBase.MainParty.Name;
		result.TroopTransferableDelegate = troopTransferableDelegate;
		result.PartyScreenMode = partyScreenMode;
		result.PartyPresentationDoneButtonDelegate = partyPresentationDoneButtonDelegate;
		result.PartyPresentationDoneButtonConditionDelegate = partyPresentationDoneButtonConditionDelegate;
		result.PartyPresentationCancelButtonActivateDelegate = partyPresentationCancelButtonActivateDelegate;
		result.PartyPresentationCancelButtonDelegate = partyPresentationCancelButtonDelegate;
		result.IsDismissMode = isDismissMode;
		result.IsTroopUpgradesDisabled = isTroopUpgradesDisabled;
		result.Header = header;
		result.PartyScreenClosedDelegate = partyScreenClosedDelegate;
		result.TransferHealthiesGetWoundedsFirst = transferHealthiesGetWoundedsFirst;
		result.ShowProgressBar = showProgressBar;
		result.MemberTransferState = memberTransferState;
		result.PrisonerTransferState = prisonerTransferState;
		result.AccompanyingTransferState = accompanyingTransferState;
		return result;
	}
}
