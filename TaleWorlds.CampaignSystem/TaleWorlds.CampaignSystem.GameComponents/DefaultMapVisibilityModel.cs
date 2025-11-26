using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMapVisibilityModel : MapVisibilityModel
{
	private const float PartySpottingDifficultyInForests = 0.3f;

	public override float MaximumSeeingRange()
	{
		return 60f;
	}

	public override float GetPartySpottingRangeBase(MobileParty party)
	{
		if (!Campaign.Current.IsNight)
		{
			return 12f;
		}
		return 6f;
	}

	public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
	{
		float partySpottingRangeBase = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRangeBase(party);
		ExplainedNumber explainedNumber = new ExplainedNumber(partySpottingRangeBase, includeDescriptions);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
		SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingSpottingDistance, party, ref explainedNumber);
		if (!party.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.EagleEye, party, isPrimaryBonus: false, ref explainedNumber);
		}
		Hero effectiveScout = party.EffectiveScout;
		if (effectiveScout != null)
		{
			if (faceTerrainType == TerrainType.Forest && PartyBaseHelper.HasFeat(party.Party, DefaultCulturalFeats.BattanianForestSpeedFeat))
			{
				explainedNumber.AddFactor(0.15f, GameTexts.FindText("str_culture"));
			}
			if (!party.IsCurrentlyAtSea)
			{
				if ((faceTerrainType == TerrainType.Plain || faceTerrainType == TerrainType.Steppe) && effectiveScout.GetPerkValue(DefaultPerks.Scouting.WaterDiviner))
				{
					explainedNumber.AddFactor(DefaultPerks.Scouting.WaterDiviner.PrimaryBonus, DefaultPerks.Scouting.WaterDiviner.Name);
				}
				if (Campaign.Current.IsNight)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.NightRunner, party, isPrimaryBonus: false, ref explainedNumber);
				}
				else
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.DayTraveler, party, isPrimaryBonus: false, ref explainedNumber);
				}
			}
			if (!party.IsMoving && !party.IsCurrentlyAtSea && party.StationaryStartTime.ElapsedHoursUntilNow >= 1f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.VantagePoint, party, isPrimaryBonus: false, ref explainedNumber);
			}
			if (effectiveScout.GetPerkValue(DefaultPerks.Scouting.MountedScouts) && !party.IsCurrentlyAtSea)
			{
				float num = 0f;
				for (int i = 0; i < party.MemberRoster.Count; i++)
				{
					if (party.MemberRoster.GetCharacterAtIndex(i).DefaultFormationClass.Equals(FormationClass.Cavalry))
					{
						num += (float)party.MemberRoster.GetElementNumber(i);
					}
				}
				if (num / (float)party.MemberRoster.TotalManCount >= 0.5f)
				{
					explainedNumber.AddFactor(DefaultPerks.Scouting.MountedScouts.PrimaryBonus, DefaultPerks.Scouting.MountedScouts.Name);
				}
			}
		}
		return explainedNumber;
	}

	public override float GetPartyRelativeInspectionRange(IMapPoint party)
	{
		return 0.5f;
	}

	public override float GetPartySpottingDifficulty(MobileParty spottingParty, MobileParty party)
	{
		float num = 1f;
		if (party != null && spottingParty != null && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace) == TerrainType.Forest)
		{
			float num2 = 0.3f;
			if (spottingParty.HasPerk(DefaultPerks.Scouting.KeenSight))
			{
				num2 += num2 * DefaultPerks.Scouting.KeenSight.PrimaryBonus;
			}
			num += num2;
		}
		return (1f / MathF.Pow((float)(party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners + 2) * 0.2f, 0.6f) + 0.94f) * num;
	}

	public override float GetHideoutSpottingDistance()
	{
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.RumourNetwork, checkSecondaryRole: true))
		{
			return MobileParty.MainParty.SeeingRange * 1.2f * (1f + DefaultPerks.Scouting.RumourNetwork.SecondaryBonus);
		}
		return MobileParty.MainParty.SeeingRange * 1.2f;
	}
}
