using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.ComponentInterfaces;

public abstract class ShipDistributionModel : MBGameModel<ShipDistributionModel>
{
	public abstract float GetScoreForPartyShipComposition(MobileParty party, MBReadOnlyList<Ship> shipsToConsider);

	public abstract bool CanPartyTakeShip(PartyBase party, Ship ship);

	public abstract bool CanSendShipToParty(Ship ship, MobileParty mobileParty);
}
