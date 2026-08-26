using NavalDLC.Map;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalDLCMobilePartyAIModel : MobilePartyAIModel
{
	private IPiratePatrolBehavior _piratePatrolBehavior;

	private IPiratePatrolBehavior PiratePatrolBehavior
	{
		get
		{
			if (_piratePatrolBehavior == null)
			{
				_piratePatrolBehavior = Campaign.Current.GetCampaignBehavior<IPiratePatrolBehavior>();
			}
			return _piratePatrolBehavior;
		}
	}

	public override float AiCheckInterval => base.BaseModel.AiCheckInterval;

	public override float FleeToNearbyPartyRadius => base.BaseModel.FleeToNearbyPartyRadius;

	public override float FleeToNearbySettlementRadius => base.BaseModel.FleeToNearbySettlementRadius;

	public override float HideoutPatrolDistanceAsDays => base.BaseModel.HideoutPatrolDistanceAsDays;

	public override float FortificationPatrolDistanceAsDays => base.BaseModel.FortificationPatrolDistanceAsDays;

	public override float FortificationPortPatrolDistanceAsDays => 0.5f;

	public override float VillagePatrolDistanceAsDays => base.BaseModel.VillagePatrolDistanceAsDays;

	public override float SettlementDefendingNearbyPartyCheckRadius => 20f;

	public override float SettlementDefendingWaitingPositionRadius => 3f;

	public override float NeededFoodsInDaysThresholdForSiege => base.BaseModel.NeededFoodsInDaysThresholdForSiege;

	public override float NeededFoodsInDaysThresholdForRaid => base.BaseModel.NeededFoodsInDaysThresholdForRaid;

	public override float GetPatrolRadius(MobileParty mobileParty, CampaignVec2 patrolPoint)
	{
		if (!patrolPoint.IsOnLand && patrolPoint.IsValid())
		{
			if (mobileParty.IsBandit && PiratePatrolBehavior != null)
			{
				return PiratePatrolBehavior.GetPatrolRadius(mobileParty);
			}
			if (mobileParty.IsLordParty)
			{
				if (!mobileParty.IsCurrentlyAtSea)
				{
					return 0f;
				}
				float num = 1f;
				if (mobileParty.TargetSettlement.MapFaction == mobileParty.MapFaction)
				{
					num = MBMath.Map(mobileParty.TargetSettlement.NearbyNavalThreatIntensity, 0f, 2f, 1f, 0.5f);
				}
				return base.BaseModel.GetPatrolRadius(mobileParty, patrolPoint) * num;
			}
			if (mobileParty.IsPatrolParty)
			{
				return Campaign.Current.EstimatedAverageBanditPartyNavalSpeed * (float)CampaignTime.HoursInDay * 0.5f;
			}
		}
		return base.BaseModel.GetPatrolRadius(mobileParty, patrolPoint);
	}

	public override float GetSettlementNearbyThreatAndAllyCheckRadius(Settlement settlement, bool isPort)
	{
		return base.BaseModel.GetSettlementNearbyThreatAndAllyCheckRadius(settlement, isPort);
	}

	public override bool ShouldPartyCheckInitiativeBehavior(MobileParty mobileParty)
	{
		return base.BaseModel.ShouldPartyCheckInitiativeBehavior(mobileParty);
	}

	public override void GetBestInitiativeBehavior(MobileParty mobileParty, out AiBehavior bestInitiativeBehavior, out MobileParty bestInitiativeTargetParty, out float bestInitiativeBehaviorScore, out Vec2 averageEnemyVec)
	{
		base.BaseModel.GetBestInitiativeBehavior(mobileParty, out bestInitiativeBehavior, out bestInitiativeTargetParty, out bestInitiativeBehaviorScore, out averageEnemyVec);
		float num = ((mobileParty.ShortTermBehavior == AiBehavior.FleeToPoint && mobileParty.ShortTermTargetParty == null) ? 0.7f : 0.5f);
		Storm storm = null;
		float num2 = float.MaxValue;
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.IsActive)
			{
				num2 = spawnedStorm.CurrentPosition.Distance(mobileParty.Position.ToVec2());
				if (num2 < spawnedStorm.EffectRadius)
				{
					storm = spawnedStorm;
				}
			}
		}
		if (storm == null || !mobileParty.IsCurrentlyAtSea)
		{
			return;
		}
		float num3 = 1f - num2 / storm.EffectRadius;
		float num4 = mobileParty.Ships.SumQ((Ship x) => x.HitPoints / x.MaxHitPoints) / (float)mobileParty.Ships.Count - num;
		if (num3 - num4 > 0f)
		{
			bestInitiativeBehaviorScore = 5f;
			bestInitiativeTargetParty = null;
			if (NavalDLCManager.Instance.GameModels.MapStormModel.CanPartyGetDamagedByStorm(mobileParty))
			{
				_ = NavalDLCManager.Instance.StormManager.DebugVisualsEnabled;
				averageEnemyVec = storm.CurrentPosition - mobileParty.Position.ToVec2();
				bestInitiativeBehavior = AiBehavior.FleeToPoint;
			}
			else if (mobileParty.CurrentSettlement != null)
			{
				bestInitiativeBehavior = AiBehavior.Hold;
			}
		}
	}

	public override bool ShouldConsiderAttacking(MobileParty party, MobileParty targetParty)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && (targetParty.IsNavalStorylineQuestParty() || targetParty.IsMainParty) && !party.IsBandit)
		{
			return false;
		}
		if (party.IsBandit && party.IsCurrentlyAtSea && Campaign.Current.Models.BanditDensityModel.IsPositionInsideNavalSafeZone(targetParty.Position))
		{
			return false;
		}
		return base.BaseModel.ShouldConsiderAttacking(party, targetParty);
	}

	public override bool ShouldConsiderAvoiding(MobileParty party, MobileParty targetParty)
	{
		if (party.IsCurrentlyAtSea != targetParty.IsCurrentlyAtSea && party.CurrentSettlement != null)
		{
			return false;
		}
		return base.BaseModel.ShouldConsiderAvoiding(party, targetParty);
	}
}
