using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using StoryMode.Quests.PlayerClanQuests;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class MainStorylineCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnGameLoadFinished()
	{
		if (Clan.PlayerClan.Kingdom != null && !Clan.PlayerClan.IsUnderMercenaryService)
		{
			Clan.PlayerClan.IsNoble = true;
		}
		int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		if (StoryModeHeroes.LittleSister.Age < (float)heroComesOfAge)
		{
			if (!StoryModeHeroes.LittleSister.IsDisabled && !StoryModeHeroes.LittleSister.IsNotSpawned)
			{
				DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
			}
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.6"))
			{
				AgingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AgingCampaignBehavior>();
				if (campaignBehavior != null)
				{
					FieldInfo field = typeof(AgingCampaignBehavior).GetField("_heroesYoungerThanHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic);
					Dictionary<Hero, int> dictionary = (Dictionary<Hero, int>)field.GetValue(campaignBehavior);
					if (!dictionary.ContainsKey(StoryModeHeroes.LittleSister))
					{
						dictionary.Add(StoryModeHeroes.LittleSister, (int)StoryModeHeroes.LittleSister.Age);
						field.SetValue(campaignBehavior, dictionary);
					}
				}
			}
		}
		else if (!StoryModeHeroes.LittleSister.IsDisabled && (!StoryModeManager.Current.MainStoryLine.FamilyRescued || Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest))))
		{
			DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
		}
		else if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.6") && StoryModeManager.Current.MainStoryLine.FamilyRescued && StoryModeHeroes.LittleSister.IsNotSpawned)
		{
			HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.LittleSister, GetSettlementForRelativeSpawn(StoryModeHeroes.LittleSister));
		}
		if (StoryModeHeroes.LittleBrother.Age < (float)heroComesOfAge)
		{
			if (!StoryModeHeroes.LittleBrother.IsDisabled && !StoryModeHeroes.LittleBrother.IsNotSpawned)
			{
				DisableHeroAction.Apply(StoryModeHeroes.LittleBrother);
			}
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.6"))
			{
				AgingCampaignBehavior campaignBehavior2 = Campaign.Current.GetCampaignBehavior<AgingCampaignBehavior>();
				if (campaignBehavior2 != null)
				{
					FieldInfo field2 = typeof(AgingCampaignBehavior).GetField("_heroesYoungerThanHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic);
					Dictionary<Hero, int> dictionary2 = (Dictionary<Hero, int>)field2.GetValue(campaignBehavior2);
					if (!dictionary2.ContainsKey(StoryModeHeroes.LittleBrother))
					{
						dictionary2.Add(StoryModeHeroes.LittleBrother, (int)StoryModeHeroes.LittleBrother.Age);
						field2.SetValue(campaignBehavior2, dictionary2);
					}
				}
			}
		}
		else if (!StoryModeHeroes.LittleBrother.IsDisabled && (!StoryModeManager.Current.MainStoryLine.FamilyRescued || Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest))))
		{
			DisableHeroAction.Apply(StoryModeHeroes.LittleBrother);
		}
		else if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.6") && StoryModeManager.Current.MainStoryLine.FamilyRescued && StoryModeHeroes.LittleBrother.IsNotSpawned)
		{
			HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.LittleBrother, GetSettlementForRelativeSpawn(StoryModeHeroes.LittleBrother));
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0"))
		{
			FirstPhase instance = FirstPhase.Instance;
			if (instance != null && instance.AllPiecesCollected)
			{
				ItemObject @object = Campaign.Current.ObjectManager.GetObject<ItemObject>("dragon_banner");
				bool flag = false;
				foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
				{
					if (item.EquipmentElement.Item == @object)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					StoryModeManager.Current.MainStoryLine.FirstPhase?.MergeDragonBanner();
				}
			}
		}
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.2.9.35367")))
		{
			return;
		}
		List<EquipmentElement> list = new List<EquipmentElement>();
		foreach (ItemRosterElement item2 in MobileParty.MainParty.ItemRoster)
		{
			string text = item2.EquipmentElement.Item?.StringId;
			if (!item2.EquipmentElement.IsQuestItem)
			{
				switch (text)
				{
				case "dragon_banner_center":
				case "dragon_banner_dragonhead":
				case "dragon_banner_handle":
				case "dragon_banner":
					list.Add(item2.EquipmentElement);
					break;
				}
			}
		}
		if (!list.Any())
		{
			return;
		}
		foreach (EquipmentElement item3 in list)
		{
			MobileParty.MainParty.ItemRoster.AddToCounts(item3, -1);
			MobileParty.MainParty.ItemRoster.AddToCounts(new EquipmentElement(item3.Item, null, null, isQuestItem: true), 1);
		}
	}

	private Settlement GetSettlementForRelativeSpawn(Hero hero)
	{
		if (!hero.HomeSettlement.OwnerClan.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			return hero.HomeSettlement;
		}
		if (!Clan.PlayerClan.MapFaction.Settlements.IsEmpty())
		{
			return Clan.PlayerClan.MapFaction.Settlements.GetRandomElement();
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				return item;
			}
		}
		return Village.All.GetRandomElement().Settlement;
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan && newKingdom != null && (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom))
		{
			Clan.PlayerClan.IsNoble = true;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if ((hero == StoryModeHeroes.Radagos && StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest)) && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RebuildPlayerClanQuest)) && causeOfDeath == KillCharacterAction.KillCharacterActionDetail.Executed) || causeOfDeath == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
		{
			result = true;
		}
		else if (hero.IsSpecial && hero != StoryModeHeroes.RadagosHenchman && !StoryModeManager.Current.MainStoryLine.IsCompleted)
		{
			result = false;
		}
	}
}
