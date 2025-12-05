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
using TaleWorlds.ModuleManager;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class MainStorylineCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private static void OnGameLoadFinished()
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.7.103044"))
		{
			if (Clan.PlayerClan.Kingdom != null && !Clan.PlayerClan.IsUnderMercenaryService && !Clan.PlayerClan.IsNoble)
			{
				Clan.PlayerClan.IsNoble = true;
			}
			int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
			AgingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AgingCampaignBehavior>();
			FieldInfo field = typeof(AgingCampaignBehavior).GetField("_heroesYoungerThanHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic);
			Dictionary<Hero, int> dictionary = (Dictionary<Hero, int>)field.GetValue(campaignBehavior);
			if (StoryModeHeroes.LittleSister.Age < (float)heroComesOfAge)
			{
				if (!StoryModeHeroes.LittleSister.IsDisabled && !StoryModeHeroes.LittleSister.IsNotSpawned)
				{
					DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
				}
				if (!dictionary.ContainsKey(StoryModeHeroes.LittleSister))
				{
					dictionary.Add(StoryModeHeroes.LittleSister, (int)StoryModeHeroes.LittleSister.Age);
					field.SetValue(campaignBehavior, dictionary);
				}
			}
			else if (!ModuleHelper.IsModuleActive("NavalDLC"))
			{
				if (!StoryModeHeroes.LittleSister.IsDisabled && (!StoryModeManager.Current.MainStoryLine.FamilyRescued || Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest))))
				{
					DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
					if (StoryModeHeroes.LittleSister.GovernorOf != null)
					{
						ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleSister);
					}
				}
				else if (StoryModeManager.Current.MainStoryLine.FamilyRescued && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest)) && !dictionary.ContainsKey(StoryModeHeroes.LittleSister))
				{
					if (StoryModeHeroes.LittleSister.IsNotSpawned)
					{
						HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.LittleSister, HeroHelper.GetSettlementForRelativeSpawn(StoryModeHeroes.LittleSister));
					}
					else if (StoryModeHeroes.LittleSister.IsDisabled)
					{
						StoryModeHeroes.LittleSister.ChangeState(Hero.CharacterStates.Active);
						Settlement settlement = ((StoryModeHeroes.LittleSister.GovernorOf != null) ? StoryModeHeroes.LittleSister.GovernorOf.Settlement : HeroHelper.GetSettlementForRelativeSpawn(StoryModeHeroes.LittleSister));
						EnterSettlementAction.ApplyForCharacterOnly(StoryModeHeroes.LittleSister, settlement);
					}
					if (StoryModeHeroes.LittleSister.Clan == null)
					{
						StoryModeHeroes.LittleSister.Clan = Clan.PlayerClan;
						MakeHeroFugitiveAction.Apply(StoryModeHeroes.LittleSister);
					}
				}
			}
			if (StoryModeHeroes.LittleBrother.Age < (float)heroComesOfAge)
			{
				if (!StoryModeHeroes.LittleBrother.IsDisabled && !StoryModeHeroes.LittleBrother.IsNotSpawned)
				{
					DisableHeroAction.Apply(StoryModeHeroes.LittleBrother);
				}
				if (!dictionary.ContainsKey(StoryModeHeroes.LittleBrother))
				{
					dictionary.Add(StoryModeHeroes.LittleBrother, (int)StoryModeHeroes.LittleBrother.Age);
					field.SetValue(campaignBehavior, dictionary);
				}
			}
			else if (!StoryModeHeroes.LittleBrother.IsDisabled && (!StoryModeManager.Current.MainStoryLine.FamilyRescued || Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest))))
			{
				DisableHeroAction.Apply(StoryModeHeroes.LittleBrother);
				if (StoryModeHeroes.LittleBrother.GovernorOf != null)
				{
					ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleBrother);
				}
			}
			else if (StoryModeManager.Current.MainStoryLine.FamilyRescued && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest)) && !dictionary.ContainsKey(StoryModeHeroes.LittleBrother))
			{
				if (StoryModeHeroes.LittleBrother.IsNotSpawned)
				{
					HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.LittleBrother, HeroHelper.GetSettlementForRelativeSpawn(StoryModeHeroes.LittleBrother));
				}
				else if (StoryModeHeroes.LittleBrother.IsDisabled)
				{
					StoryModeHeroes.LittleBrother.ChangeState(Hero.CharacterStates.Active);
				}
				if (StoryModeHeroes.LittleBrother.Clan == null)
				{
					StoryModeHeroes.LittleBrother.Clan = Clan.PlayerClan;
					MakeHeroFugitiveAction.Apply(StoryModeHeroes.LittleBrother);
				}
			}
			if (StoryModeManager.Current.MainStoryLine.FamilyRescued && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuestBehavior.RescueFamilyQuest)))
			{
				if (StoryModeHeroes.ElderBrother.IsNotSpawned)
				{
					HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.ElderBrother, HeroHelper.GetSettlementForRelativeSpawn(StoryModeHeroes.ElderBrother));
				}
				else if (StoryModeHeroes.ElderBrother.IsDisabled)
				{
					StoryModeHeroes.ElderBrother.ChangeState(Hero.CharacterStates.Active);
				}
				if (StoryModeHeroes.ElderBrother.Clan == null)
				{
					StoryModeHeroes.ElderBrother.Clan = Clan.PlayerClan;
					MakeHeroFugitiveAction.Apply(StoryModeHeroes.ElderBrother);
				}
			}
			if (StoryModeHeroes.LittleSister.GovernorOf != null && StoryModeHeroes.LittleSister.CurrentSettlement != StoryModeHeroes.LittleSister.GovernorOf.Settlement)
			{
				ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleSister);
			}
			if (StoryModeHeroes.LittleBrother.GovernorOf != null && StoryModeHeroes.LittleBrother.CurrentSettlement != StoryModeHeroes.LittleBrother.GovernorOf.Settlement)
			{
				ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleBrother);
			}
			if (StoryModeHeroes.ElderBrother.GovernorOf != null && StoryModeHeroes.ElderBrother.CurrentSettlement != StoryModeHeroes.ElderBrother.GovernorOf.Settlement)
			{
				ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.ElderBrother);
			}
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

	private static void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan && newKingdom != null && (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom))
		{
			Clan.PlayerClan.IsNoble = true;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
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
