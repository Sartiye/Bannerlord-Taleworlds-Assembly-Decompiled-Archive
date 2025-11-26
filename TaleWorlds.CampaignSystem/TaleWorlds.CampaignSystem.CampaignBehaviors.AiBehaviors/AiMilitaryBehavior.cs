using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;

public class AiMilitaryBehavior : CampaignBehaviorBase
{
	private const int MinimumInfluenceNeededToCreateArmy = 50;

	private const float MeaningfulCohesionThresholdForArmy = 40f;

	private const float MinimumCohesionScoreThreshold = 0.25f;

	private const float AverageSiegeDurationAsDays = 5.73f;

	private IDisbandPartyCampaignBehavior _disbandPartyCampaignBehavior;

	private readonly HashSet<Settlement> _checkedNeighbors = new HashSet<Settlement>();

	public override void RegisterEvents()
	{
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (mapEvent.IsNavalMapEvent || mapEvent.MapEventSettlement == null || !mapEvent.MapEventSettlement.IsFortification || !mapEvent.MapEventSettlement.HasPort || mapEvent.MapEventSettlement.SiegeEvent == null || !mapEvent.MapEventSettlement.SiegeEvent.IsBlockadeActive)
		{
			return;
		}
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if (allLordParty.DefaultBehavior == AiBehavior.DefendSettlement && allLordParty.TargetSettlement == mapEvent.MapEventSettlement && allLordParty.IsTargetingPort)
			{
				allLordParty.SetMoveModeHold();
			}
		}
	}

	private void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		foreach (MobileParty item in MobileParty.All)
		{
			if (item.DefaultBehavior == AiBehavior.GoToSettlement && item.TargetSettlement == siegeEvent.BesiegedSettlement && item.CurrentSettlement != siegeEvent.BesiegedSettlement && !item.IsTargetingPort)
			{
				item.SetMoveModeHold();
			}
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.RetreatingSide != BattleSideEnum.None)
		{
			MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.RetreatingSide.GetOppositeSide());
			{
				foreach (MapEventParty party in mapEvent.GetMapEventSide(mapEvent.RetreatingSide).Parties)
				{
					MobileParty mobileParty = party.Party.MobileParty;
					if (mobileParty != null && mobileParty.AttachedTo == null)
					{
						mobileParty.TeleportPartyToOutSideOfEncounterRadius();
						mobileParty.Ai.CalculateFleePosition(out var fleeTargetPoint, mapEventSide.LeaderParty.MobileParty, mapEventSide.LeaderParty.MobileParty.Position.ToVec2());
						mobileParty.SetMoveGoToPoint(fleeTargetPoint, (!mobileParty.IsCurrentlyAtSea) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
					}
				}
				return;
			}
		}
		MobileParty mobileParty2 = mapEvent.AttackerSide.LeaderParty.MobileParty;
		bool flag = mapEvent.IsRaid && mapEvent.BattleState == BattleState.AttackerVictory && !mapEvent.MapEventSettlement.SettlementHitPoints.ApproximatelyEqualsTo(0f);
		Settlement mapEventSettlement = mapEvent.MapEventSettlement;
		if (mobileParty2 != MobileParty.MainParty && flag)
		{
			mobileParty2.SetMoveRaidSettlement(mapEventSettlement, mobileParty2.NavigationCapability);
			mobileParty2.RecalculateShortTermBehavior();
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		_disbandPartyCampaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void FindBestTargetAndItsValueForFaction(Army.ArmyTypes missionType, PartyThinkParams p, float ourStrength, float newArmyCreatingAdditionalConstant = 1f)
	{
		MobileParty mobilePartyOf = p.MobilePartyOf;
		IFaction mapFaction = mobilePartyOf.MapFaction;
		if ((mobilePartyOf.Army != null && mobilePartyOf.Army.LeaderParty != mobilePartyOf) || (mobilePartyOf.Objective == MobileParty.PartyObjective.Defensive && (missionType == Army.ArmyTypes.Besieger || missionType == Army.ArmyTypes.Raider)) || (mobilePartyOf.Objective == MobileParty.PartyObjective.Aggressive && missionType == Army.ArmyTypes.Defender))
		{
			return;
		}
		float num = 1f;
		if (mobilePartyOf.Army != null && mobilePartyOf.Army.Cohesion < 40f)
		{
			num *= mobilePartyOf.Army.Cohesion / 40f;
		}
		if (!(num > 0.25f))
		{
			return;
		}
		float partySizeScore = GetPartySizeScore(mobilePartyOf, missionType);
		AiBehavior aiBehavior = AiBehavior.Hold;
		switch (missionType)
		{
		case Army.ArmyTypes.Defender:
			aiBehavior = AiBehavior.DefendSettlement;
			break;
		case Army.ArmyTypes.Besieger:
			aiBehavior = AiBehavior.BesiegeSettlement;
			break;
		case Army.ArmyTypes.Raider:
			aiBehavior = AiBehavior.RaidSettlement;
			break;
		}
		float foodScoreForActionType = GetFoodScoreForActionType(p, missionType);
		if (!(foodScoreForActionType > 0f))
		{
			return;
		}
		switch (missionType)
		{
		case Army.ArmyTypes.Defender:
			CalculateMilitaryBehaviorForFactionSettlements(mapFaction, p, missionType, aiBehavior, ourStrength, partySizeScore, num, foodScoreForActionType, newArmyCreatingAdditionalConstant);
			break;
		case Army.ArmyTypes.Raider:
			if (mobilePartyOf.Army != null || p.WillGatherAnArmy)
			{
				break;
			}
			goto default;
		default:
		{
			for (int i = 0; i < mapFaction.FactionsAtWarWith.Count; i++)
			{
				IFaction faction = mapFaction.FactionsAtWarWith[i];
				if (faction.Leader != null && faction.IsMapFaction)
				{
					CalculateMilitaryBehaviorForFactionSettlements(faction, p, missionType, aiBehavior, ourStrength, partySizeScore, num, foodScoreForActionType, newArmyCreatingAdditionalConstant);
				}
			}
			break;
		}
		}
	}

	private float GetFoodScoreForActionType(PartyThinkParams p, Army.ArmyTypes type)
	{
		float num = ((type == Army.ArmyTypes.Raider) ? Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForRaid : Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege);
		MobileParty mobilePartyOf = p.MobilePartyOf;
		int num2 = mobilePartyOf.GetNumDaysForFoodToLast();
		if (p.WillGatherAnArmy)
		{
			foreach (MobileParty item in p.PossibleArmyMembersUponArmyCreation)
			{
				num2 += item.GetNumDaysForFoodToLast();
			}
			num2 /= p.PossibleArmyMembersUponArmyCreation.Count + 1;
		}
		else if (mobilePartyOf.Army != null && mobilePartyOf == mobilePartyOf.Army.LeaderParty)
		{
			foreach (MobileParty attachedParty in mobilePartyOf.Army.LeaderParty.AttachedParties)
			{
				num2 += attachedParty.GetNumDaysForFoodToLast();
			}
			num2 /= mobilePartyOf.Army.LeaderParty.AttachedParties.Count + 1;
		}
		if ((p.WillGatherAnArmy || type == Army.ArmyTypes.Raider) && num > (float)num2)
		{
			return 0f;
		}
		if (!((float)num2 < num))
		{
			return 1f;
		}
		return 0.1f + 0.9f * ((float)num2 / num);
	}

	private float GetPartySizeScore(MobileParty mobileParty, Army.ArmyTypes missionType)
	{
		float num = 1f;
		if (mobileParty.Army != null)
		{
			float num2 = 0f;
			foreach (MobileParty party in mobileParty.Army.Parties)
			{
				float num3 = PartyBaseHelper.FindPartySizeNormalLimit(party);
				float num4 = party.PartySizeRatio / num3;
				num2 += num4;
			}
			num = num2 / (float)mobileParty.Army.Parties.Count;
		}
		else
		{
			float num5 = PartyBaseHelper.FindPartySizeNormalLimit(mobileParty);
			num = mobileParty.PartySizeRatio / num5;
		}
		float num6 = MathF.Max(1f, MathF.Min((float)mobileParty.MapFaction.Fiefs.Count((Town x) => x.IsTown) / 5f, 2.5f));
		switch (missionType)
		{
		case Army.ArmyTypes.Defender:
			num6 = MathF.Pow(num6, 0.75f);
			break;
		case Army.ArmyTypes.Raider:
			num6 *= 0.75f;
			break;
		}
		return MathF.Min(1f, MathF.Pow(num, num6));
	}

	private void CalculateMilitaryBehaviorForFactionSettlements(IFaction faction, PartyThinkParams p, Army.ArmyTypes missionType, AiBehavior aiBehavior, float ourStrength, float partySizeScore, float cohesionScore, float foodScore, float newArmyCreatingAdditionalConstant)
	{
		MobileParty mobilePartyOf = p.MobilePartyOf;
		for (int i = 0; i < faction.Settlements.Count; i++)
		{
			Settlement settlement = faction.Settlements[i];
			if (CheckIfSettlementIsSuitableForMilitaryAction(settlement, mobilePartyOf, missionType, p.WillGatherAnArmy))
			{
				CalculateMilitaryBehaviorForSettlement(settlement, missionType, aiBehavior, p, ourStrength, partySizeScore, cohesionScore, foodScore, newArmyCreatingAdditionalConstant);
			}
		}
	}

	private bool CheckIfSettlementIsSuitableForMilitaryAction(Settlement settlement, MobileParty mobileParty, Army.ArmyTypes missionType, bool isCalculatingForNewArmyCreation)
	{
		if (MobileParty.MainParty.ShouldBeIgnored && !mobileParty.IsMainParty && !mobileParty.AttachedParties.Contains(MobileParty.MainParty) && ((settlement.Party.MapEvent != null && settlement.Party.MapEvent == MapEvent.PlayerMapEvent) || (settlement.SiegeEvent != null && settlement.SiegeEvent.IsPlayerSiegeEvent)))
		{
			return false;
		}
		if ((mobileParty.Army != null || isCalculatingForNewArmyCreation) && missionType == Army.ArmyTypes.Defender && settlement.IsVillage)
		{
			return false;
		}
		return true;
	}

	private void CalculateDistanceScoreForBesieging(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
	{
		_checkedNeighbors.Clear();
		float num = 0.01f;
		float num2 = 0.0001f;
		IFaction mapFaction = mobileParty.MapFaction;
		MBReadOnlyList<Settlement> neighborFortifications = targetSettlement.Town.GetNeighborFortifications(MobileParty.NavigationType.All);
		foreach (Settlement item in neighborFortifications)
		{
			_checkedNeighbors.Add(item);
			if (item.MapFaction != targetSettlement.MapFaction)
			{
				num += 1f;
				if (item.MapFaction == mapFaction)
				{
					num2 += 1f;
				}
			}
		}
		float num3 = 0.01f;
		float num4 = 0.0001f;
		foreach (Settlement item2 in neighborFortifications)
		{
			foreach (Settlement neighborFortification in item2.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification == targetSettlement || _checkedNeighbors.Contains(neighborFortification))
				{
					continue;
				}
				_checkedNeighbors.Add(neighborFortification);
				if (neighborFortification.MapFaction != targetSettlement.MapFaction)
				{
					num3 += 1f;
					if (neighborFortification.MapFaction == mapFaction)
					{
						num4 += 1f;
					}
				}
			}
		}
		bestDistanceScore = 0f + num2 / num * 1f + num4 / num3 * 0.25f;
		if (bestDistanceScore < 0.1f)
		{
			bestDistanceScore = 0f;
		}
		isTargetingPort = false;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out var _, out isFromPort);
	}

	private void GetDistanceScoreForRaiding(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
	{
		isTargetingPort = false;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out var bestNavigationDistance, out isFromPort);
		float num = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 3f;
		if (bestNavigationDistance > num)
		{
			bestNavigationType = MobileParty.NavigationType.None;
			bestNavigationDistance = float.MaxValue;
			isTargetingPort = false;
		}
		bestDistanceScore = MBMath.Map(0.75f - bestNavigationDistance / num, 0f, 1f, 0.1f, 1f);
	}

	private void GetDistanceScoreForDefending(Settlement targetSettlement, MobileParty mobileParty, out MobileParty.NavigationType bestNavigationType, out float bestDistanceScore, out bool isFromPort, out bool isTargetingPort)
	{
		isTargetingPort = false;
		bool flag = targetSettlement.HasPort && mobileParty.HasNavalNavigationCapability;
		int num;
		if (flag && targetSettlement.SiegeEvent != null)
		{
			if (targetSettlement.SiegeEvent.IsBlockadeActive)
			{
				if (targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent == null)
				{
					num = 0;
					goto IL_008e;
				}
				if (!targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockade)
				{
					num = (targetSettlement.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent.IsBlockadeSallyOut ? 1 : 0);
					if (num == 0)
					{
						goto IL_008e;
					}
				}
				else
				{
					num = 1;
				}
			}
			else
			{
				num = 1;
			}
			isTargetingPort = true;
		}
		else
		{
			num = 0;
		}
		goto IL_008e;
		IL_008e:
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort, out bestNavigationType, out var bestNavigationDistance, out isFromPort);
		if (num == 0 && flag)
		{
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, targetSettlement, isTargetingPort: true, out bestNavigationType, out bestNavigationDistance, out isFromPort);
		}
		float num2 = ((bestNavigationType == MobileParty.NavigationType.Naval) ? Campaign.Current.EstimatedAverageLordPartyNavalSpeed : Campaign.Current.EstimatedAverageLordPartySpeed);
		if (bestNavigationType == MobileParty.NavigationType.All)
		{
			num2 = (Campaign.Current.EstimatedAverageLordPartyNavalSpeed + Campaign.Current.EstimatedAverageLordPartySpeed) * 0.5f;
		}
		float num3 = bestNavigationDistance / (num2 * (float)CampaignTime.HoursInDay);
		float num4 = 2.865f;
		if (targetSettlement.IsVillage)
		{
			MapEvent mapEvent = targetSettlement.Party.MapEvent;
			if (mapEvent != null && mapEvent.Component is RaidEventComponent { RaidDamage: >0f, RaidDamage: var raidDamage })
			{
				float num5 = raidDamage / mapEvent.BattleStartTime.ElapsedDaysUntilNow;
				num4 = targetSettlement.SettlementHitPoints / num5;
			}
		}
		else if (targetSettlement.IsFortification && targetSettlement.Party.SiegeEvent != null)
		{
			num4 = 5.73f;
		}
		if (num3 >= num4)
		{
			bestNavigationType = MobileParty.NavigationType.None;
			bestDistanceScore = 0f;
			isTargetingPort = false;
		}
		else if (targetSettlement.Party.MapEventSide == null && targetSettlement.SiegeEvent != null && mobileParty.NavigationCapability == MobileParty.NavigationType.All)
		{
			bool shouldJoinLandSide = false;
			bool shouldConsiderJoiningNearbyAllyParties = mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.ShortTermTargetParty != null && !mobileParty.ShortTermTargetParty.MapFaction.IsAtWarWith(mobileParty.MapFaction);
			if (shouldConsiderJoiningNearbyAllyParties)
			{
				shouldJoinLandSide = !mobileParty.ShortTermTargetParty.IsCurrentlyAtSea;
				bestNavigationType = MobileParty.NavigationType.All;
			}
			else
			{
				mobileParty.Ai.GetNearbyPartyDataWhileDefendingSettlement(targetSettlement, out shouldConsiderJoiningNearbyAllyParties, out shouldJoinLandSide, out var _, out var _, out var _);
			}
			isTargetingPort = !shouldJoinLandSide && targetSettlement.HasPort;
		}
		bestDistanceScore = MBMath.Map(1f - num3 / (num4 + 0.01f), 0f, 1f, 0.1f, 1f);
	}

	private void CalculateMilitaryBehaviorForSettlement(Settlement settlement, Army.ArmyTypes missionType, AiBehavior aiBehavior, PartyThinkParams p, float ourStrength, float partySizeScore, float cohesionScore, float foodScore, float newArmyCreatingAdditionalConstant = 1f)
	{
		if ((missionType != Army.ArmyTypes.Defender || settlement.LastAttackerParty == null || !settlement.LastAttackerParty.IsActive) && (missionType != Army.ArmyTypes.Raider || !settlement.IsVillage || settlement.Village.VillageState == Village.VillageStates.Looted) && (missionType != 0 || !settlement.IsFortification || (settlement.SiegeEvent != null && settlement.SiegeEvent.BesiegerCamp.MapFaction != p.MobilePartyOf.MapFaction)))
		{
			return;
		}
		MobileParty mobilePartyOf = p.MobilePartyOf;
		if ((missionType == Army.ArmyTypes.Raider && (settlement.Village.VillageState != 0 || settlement.Party.MapEvent != null) && (mobilePartyOf.MapEvent == null || mobilePartyOf.MapEvent.MapEventSettlement != settlement)) || (missionType == Army.ArmyTypes.Besieger && (settlement.Party.MapEvent != null || settlement.SiegeEvent != null) && (settlement.SiegeEvent == null || settlement.SiegeEvent.BesiegerCamp.MapFaction != mobilePartyOf.MapFaction) && (mobilePartyOf.MapEvent == null || mobilePartyOf.MapEvent.MapEventSettlement != settlement)) || (missionType == Army.ArmyTypes.Defender && (settlement.LastAttackerParty == null || !settlement.LastAttackerParty.IsActive || !settlement.LastAttackerParty.MapFaction.IsAtWarWith(mobilePartyOf.MapFaction))) || (mobilePartyOf.Army == null && missionType == Army.ArmyTypes.Besieger && ((settlement.Party.MapEvent != null && settlement.Party.MapEvent.AttackerSide.LeaderParty != mobilePartyOf.Party) || (settlement.Party.SiegeEvent != null && mobilePartyOf.BesiegedSettlement != settlement))))
		{
			return;
		}
		MobileParty.NavigationType bestNavigationType = MobileParty.NavigationType.None;
		float bestDistanceScore = float.MaxValue;
		bool isTargetingPort = false;
		bool isFromPort = false;
		switch (missionType)
		{
		case Army.ArmyTypes.Besieger:
			CalculateDistanceScoreForBesieging(settlement, mobilePartyOf, out bestNavigationType, out bestDistanceScore, out isFromPort, out isTargetingPort);
			break;
		case Army.ArmyTypes.Raider:
			GetDistanceScoreForRaiding(settlement, mobilePartyOf, out bestNavigationType, out bestDistanceScore, out isFromPort, out isTargetingPort);
			break;
		case Army.ArmyTypes.Defender:
			GetDistanceScoreForDefending(settlement, mobilePartyOf, out bestNavigationType, out bestDistanceScore, out isFromPort, out isTargetingPort);
			break;
		}
		if (!(bestDistanceScore > 0f))
		{
			return;
		}
		if (mobilePartyOf.SiegeEvent != null && mobilePartyOf.BesiegerCamp != null && mobilePartyOf.SiegeEvent.BesiegedSettlement == settlement)
		{
			ourStrength = mobilePartyOf.BesiegerCamp.GetInvolvedPartiesForEventType().Sum((PartyBase x) => x.EstimatedStrength);
		}
		float targetScoreForFaction = Campaign.Current.Models.TargetScoreCalculatingModel.GetTargetScoreForFaction(settlement, missionType, mobilePartyOf, ourStrength);
		targetScoreForFaction *= bestDistanceScore * cohesionScore * partySizeScore * foodScore * newArmyCreatingAdditionalConstant;
		if (mobilePartyOf.Objective == MobileParty.PartyObjective.Defensive)
		{
			targetScoreForFaction = ((aiBehavior != AiBehavior.DefendSettlement) ? (targetScoreForFaction * 0.8f) : (targetScoreForFaction * 1.2f));
		}
		else if (mobilePartyOf.Objective == MobileParty.PartyObjective.Aggressive)
		{
			targetScoreForFaction = ((aiBehavior != AiBehavior.BesiegeSettlement && aiBehavior != AiBehavior.RaidSettlement) ? (targetScoreForFaction * 0.8f) : (targetScoreForFaction * 1.2f));
		}
		if (!mobilePartyOf.IsDisbanding)
		{
			IDisbandPartyCampaignBehavior disbandPartyCampaignBehavior = _disbandPartyCampaignBehavior;
			if (disbandPartyCampaignBehavior == null || !disbandPartyCampaignBehavior.IsPartyWaitingForDisband(mobilePartyOf))
			{
				goto IL_02d4;
			}
		}
		targetScoreForFaction *= 0.25f;
		goto IL_02d4;
		IL_02d4:
		if (bestNavigationType != 0)
		{
			AIBehaviorData item = new AIBehaviorData(settlement, aiBehavior, bestNavigationType, p.WillGatherAnArmy, isFromPort, isTargetingPort);
			(AIBehaviorData, float) value = (item, targetScoreForFaction);
			p.AddBehaviorScore(in value);
		}
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (mobileParty.IsMilitia || mobileParty.IsCaravan || mobileParty.IsVillager || mobileParty.IsBandit || mobileParty.IsPatrolParty || mobileParty.IsDisbanding || mobileParty.LeaderHero == null || (mobileParty.MapFaction != Clan.PlayerClan.MapFaction && !mobileParty.MapFaction.IsKingdomFaction) || mobileParty.CurrentSettlement?.SiegeEvent != null)
		{
			return;
		}
		if (mobileParty.Army != null)
		{
			mobileParty.Ai.SetInitiative(0.33f, 0.33f, 24f);
			if (mobileParty.Army.LeaderParty == mobileParty && mobileParty.Army.LeaderParty.Army.IsWaitingForArmyMembers())
			{
				mobileParty.Ai.SetInitiative(0.33f, 1f, 24f);
				p.DoNotChangeBehavior = true;
			}
			else if (mobileParty.Army.LeaderParty.DefaultBehavior == AiBehavior.PatrolAroundPoint)
			{
				mobileParty.Ai.SetInitiative(1f, 1f, 24f);
			}
			else if (mobileParty.Army.LeaderParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.Army.LeaderParty == mobileParty && mobileParty.Army.AiBehaviorObject != null && mobileParty.Army.AiBehaviorObject is Settlement && ((Settlement)mobileParty.Army.AiBehaviorObject).Position.DistanceSquared(mobileParty.Position) < Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(mobileParty.NavigationCapability) * 1.53f)
			{
				mobileParty.Ai.SetInitiative(1f, 1f, 24f);
			}
			if (mobileParty.Army.LeaderParty != mobileParty)
			{
				return;
			}
		}
		else if (mobileParty.DefaultBehavior == AiBehavior.DefendSettlement || mobileParty.Objective == MobileParty.PartyObjective.Defensive)
		{
			mobileParty.Ai.SetInitiative(0.33f, 1f, 2f);
		}
		float totalLandStrengthWithFollowers = mobileParty.GetTotalLandStrengthWithFollowers();
		p.Initialization();
		bool flag = false;
		float newArmyCreatingAdditionalConstant = 1f;
		float num = totalLandStrengthWithFollowers;
		if (mobileParty.LeaderHero != null && mobileParty.Army == null && mobileParty.LeaderHero.Clan != null && mobileParty.PartySizeRatio > 0.6f && (mobileParty.LeaderHero.Clan.Leader == mobileParty.LeaderHero || (mobileParty.LeaderHero.Clan.Leader.PartyBelongedTo == null && mobileParty.LeaderHero.Clan.WarPartyComponents != null && mobileParty.LeaderHero.Clan.WarPartyComponents.FirstOrDefault() == mobileParty.WarPartyComponent)))
		{
			int traitLevel = mobileParty.LeaderHero.GetTraitLevel(DefaultTraits.Calculating);
			IFaction mapFaction = mobileParty.MapFaction;
			Kingdom kingdom = (Kingdom)mapFaction;
			int num2 = ((Kingdom)mapFaction).Armies.CountQ((Army x) => !x.Parties.Contains(MobileParty.MainParty));
			int num3 = 50 + num2 * num2 * 20 + mobileParty.LeaderHero.RandomInt(20) + traitLevel * 20;
			float num4 = 1f - (float)num2 * 0.4f;
			flag = !mobileParty.IsCurrentlyAtSea && mobileParty.MapEvent == null && mobileParty.LeaderHero.Clan.Influence > (float)num3 && mobileParty.MapFaction.IsKingdomFaction && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && (float)mobileParty.GetNumDaysForFoodToLast() > Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege && mobileParty.MapFaction.FactionsAtWarWith.AnyQ((IFaction x) => x.Fiefs.Any());
			if (flag)
			{
				float num5 = ((kingdom.Armies.Count == 0) ? (1f + MathF.Sqrt((int)CampaignTime.Now.ToDays - kingdom.LastArmyCreationDay) * 0.15f) : 1f);
				float num6 = (10f + MathF.Sqrt(MathF.Min(900f, mobileParty.LeaderHero.Clan.Influence))) / 50f;
				float num7 = MathF.Sqrt(mobileParty.PartySizeRatio);
				newArmyCreatingAdditionalConstant = num5 * num6 * num4 * num7;
				num = mobileParty.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
				List<MobileParty> mobilePartiesToCallToArmy = Campaign.Current.Models.ArmyManagementCalculationModel.GetMobilePartiesToCallToArmy(mobileParty);
				if (mobilePartiesToCallToArmy.Count == 0)
				{
					flag = false;
				}
				else
				{
					foreach (MobileParty item in mobilePartiesToCallToArmy)
					{
						p.AddPotentialArmyMember(item);
						num += item.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
					}
				}
			}
		}
		for (int i = 0; i < 4; i++)
		{
			Army.ArmyTypes armyTypes = (Army.ArmyTypes)i;
			if (flag && armyTypes != Army.ArmyTypes.Raider)
			{
				p.WillGatherAnArmy = true;
				FindBestTargetAndItsValueForFaction(armyTypes, p, num, newArmyCreatingAdditionalConstant);
			}
			p.WillGatherAnArmy = false;
			FindBestTargetAndItsValueForFaction(armyTypes, p, totalLandStrengthWithFollowers);
		}
	}
}
