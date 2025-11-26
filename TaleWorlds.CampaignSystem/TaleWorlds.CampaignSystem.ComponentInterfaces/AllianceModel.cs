using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class AllianceModel : MBGameModel<AllianceModel>
{
	public abstract CampaignTime MaxDurationOfAlliance { get; }

	public abstract CampaignTime MaxDurationOfWarParticipation { get; }

	public abstract int MaxNumberOfAlliances { get; }

	public abstract CampaignTime DurationForOffers { get; }

	public abstract int GetCallToWarCost(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst);

	public abstract ExplainedNumber GetScoreOfStartingAlliance(Kingdom kingdomDeclaresAlliance, Kingdom kingdomDeclaredAlliance, IFaction evaluatingFaction, out TextObject explanation, bool includeDescription = false);

	public abstract float GetScoreOfCallingToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason);

	public abstract float GetScoreOfJoiningWar(Kingdom offeringKingdom, Kingdom kingdomToOfferToJoinWarWith, Kingdom kingdomToOfferToJoinWarAgainst, IFaction evaluatingFaction, out TextObject reason);

	public abstract int GetInfluenceCostOfProposingStartingAlliance(Clan proposingClan);

	public abstract int GetInfluenceCostOfCallingToWar(Clan proposingClan);
}
