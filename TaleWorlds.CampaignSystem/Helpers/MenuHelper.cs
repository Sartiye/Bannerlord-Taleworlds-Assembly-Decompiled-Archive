using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class MenuHelper
{
	public static bool SetOptionProperties(MenuCallbackArgs args, bool canPlayerDo, bool shouldBeDisabled, TextObject disabledText)
	{
		if (canPlayerDo)
		{
			return true;
		}
		if (!shouldBeDisabled)
		{
			return false;
		}
		args.IsEnabled = false;
		args.Tooltip = disabledText;
		return true;
	}

	public static void SetIssueAndQuestDataForHero(MenuCallbackArgs args, Hero hero)
	{
		if (hero.Issue != null && hero.Issue.IssueQuest == null)
		{
			args.OptionQuestData |= GameMenuOption.IssueQuestFlags.AvailableIssue;
		}
		Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero, out var value);
		if (value != null)
		{
			for (int i = 0; i < value.Count; i++)
			{
				if (!value[i].IsTrackEnabled)
				{
					continue;
				}
				if (value[i].IsSpecialQuest)
				{
					if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == 0 && value[i].QuestGiver != hero)
					{
						args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedStoryQuest;
					}
					else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == 0 && value[i].QuestGiver == hero)
					{
						args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
					}
				}
				else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == 0 && value[i].QuestGiver != hero)
				{
					args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedIssue;
				}
				else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == 0 && value[i].QuestGiver == hero)
				{
					args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
				}
			}
		}
		if (hero.PartyBelongedTo != null && ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == 0 || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == 0 || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == 0 || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == 0))
		{
			Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero.PartyBelongedTo, out var value2);
			if (value2 != null)
			{
				for (int j = 0; j < value2.Count; j++)
				{
					if (!value2[j].IsTrackEnabled)
					{
						continue;
					}
					if (value2[j].IsSpecialQuest)
					{
						if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == 0 && value2[j].QuestGiver != hero)
						{
							args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedStoryQuest;
						}
						else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == 0 && value2[j].QuestGiver == hero)
						{
							args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
						}
					}
					else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == 0 && value2[j].QuestGiver != hero)
					{
						args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedIssue;
					}
					else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == 0 && value2[j].QuestGiver == hero)
					{
						args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
					}
				}
			}
		}
		if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == 0 && hero.Issue?.IssueQuest != null && hero.Issue.IssueQuest.IsTrackEnabled)
		{
			args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
		}
	}

	public static void SetIssueAndQuestDataForLocations(MenuCallbackArgs args, List<Location> locations)
	{
		GameMenuOption.IssueQuestFlags issueQuestFlags = Campaign.Current.IssueManager.CheckIssueForMenuLocations(locations, getIssuesWithoutAQuest: true);
		args.OptionQuestData |= issueQuestFlags;
		args.OptionQuestData |= Campaign.Current.QuestManager.CheckQuestForMenuLocations(locations);
	}

	public static bool CheckAndOpenNextLocation(MenuCallbackArgs args)
	{
		if (Campaign.Current.GameMenuManager.NextLocation != null && GameStateManager.Current.ActiveState is MapState)
		{
			PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation);
			switch (Campaign.Current.GameMenuManager.NextLocation.StringId)
			{
			case "center":
				if (Settlement.CurrentSettlement.IsCastle)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("castle");
				}
				else if (Settlement.CurrentSettlement.IsTown)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("town");
				}
				else if (Settlement.CurrentSettlement.IsVillage)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("village");
				}
				else
				{
					Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CheckAndOpenNextLocation", 192);
				}
				break;
			case "tavern":
				Campaign.Current.GameMenuManager.SetNextMenu("town_backstreet");
				break;
			case "arena":
				Campaign.Current.GameMenuManager.SetNextMenu("town_arena");
				break;
			case "lordshall":
			case "prison":
				Campaign.Current.GameMenuManager.SetNextMenu("town_keep");
				break;
			case "port":
				Campaign.Current.GameMenuManager.SetNextMenu("port_menu");
				break;
			}
			Campaign.Current.GameMenuManager.NextLocation = null;
			Campaign.Current.GameMenuManager.PreviousLocation = null;
			return true;
		}
		return false;
	}

	public static void DecideMenuState()
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (!string.IsNullOrEmpty(genericStateMenu))
		{
			GameMenu.SwitchToMenu(genericStateMenu);
		}
		else
		{
			GameMenu.ExitToLast();
		}
	}

	public static bool EncounterAttackCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
		if (MapEvent.PlayerMapEvent == null)
		{
			return false;
		}
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		Settlement mapEventSettlement = playerMapEvent.MapEventSettlement;
		if (mapEventSettlement != null && mapEventSettlement.IsFortification && playerMapEvent.IsSiegeAssault && PlayerSiege.PlayerSiegeEvent != null && !PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete)
		{
			return false;
		}
		bool flag = MapEvent.PlayerMapEvent.PartiesOnSide(PartyBase.MainParty.OpponentSide).Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
		if (Hero.MainHero.IsWounded)
		{
			args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.");
			args.IsEnabled = false;
		}
		bool flag2 = (playerMapEvent.HasTroopsOnBothSides() || playerMapEvent.IsSiegeAssault) && MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide) != null;
		if (!MobileParty.MainParty.IsInRaftState)
		{
			MobileParty mobileParty = playerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide)[0].Party.MobileParty;
			if (mobileParty == null || !mobileParty.IsInRaftState)
			{
				goto IL_0125;
			}
		}
		args.Tooltip = new TextObject("{=x9ePfpw5}You are on a raft, in desperate circumstances, and cannot fight");
		args.IsEnabled = false;
		goto IL_0125;
		IL_0125:
		if (flag && !flag2 && !Hero.MainHero.IsWounded)
		{
			Debug.FailedAssert("This encounter case should be investigated", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "EncounterAttackCondition", 275);
			return false;
		}
		if (flag && Game.Current.IsDevelopmentMode && (mapEventSettlement == null || playerMapEvent.IsBlockadeSallyOut || playerMapEvent.IsSallyOut || playerMapEvent.IsSiegeOutside || playerMapEvent.IsBlockade))
		{
			bool isNavalEncounter = PlayerEncounter.IsNavalEncounter();
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 position = MobileParty.MainParty.Position;
			MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(in position);
			string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, isNavalEncounter);
			args.Tooltip = new TextObject("{=!}[DEV] Scene: (" + battleSceneForMapPatch + ")");
		}
		return flag;
	}

	public static bool EncounterCaptureEnemyCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
		MapEvent battle = PlayerEncounter.Battle;
		return battle?.PartiesOnSide(battle.GetOtherSide(battle.PlayerSide)).All((MapEventParty party) => !party.Party.IsSettlement && (party.Party.NumberOfHealthyMembers == 0 || party.Party.MobileParty.IsInRaftState)) ?? false;
	}

	public static void EncounterAttackConsequence(MenuCallbackArgs args)
	{
		MapEvent battle = PlayerEncounter.Battle;
		PartyBase leaderParty = battle.GetLeaderParty(PartyBase.MainParty.OpponentSide);
		BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, leaderParty);
		if (PlayerEncounter.Current == null)
		{
			return;
		}
		Settlement mapEventSettlement = MobileParty.MainParty.MapEvent.MapEventSettlement;
		if (mapEventSettlement != null && !battle.IsBlockadeSallyOut && !battle.IsSallyOut && !battle.IsSiegeOutside && !battle.IsBlockade)
		{
			if (mapEventSettlement.IsFortification)
			{
				if (battle.IsRaid)
				{
					PlayerEncounter.StartVillageBattleMission();
				}
				else if (battle.IsSiegeAmbush)
				{
					PlayerEncounter.StartSiegeAmbushMission();
				}
				else if (battle.IsSiegeAssault)
				{
					if (PlayerSiege.PlayerSiegeEvent == null && PartyBase.MainParty.Side == BattleSideEnum.Attacker)
					{
						PlayerSiege.StartPlayerSiege(MobileParty.MainParty.Party.Side, isSimulation: false, mapEventSettlement);
					}
					else
					{
						if (PlayerSiege.PlayerSiegeEvent != null && !PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide.GetOppositeSide()).GetInvolvedPartiesForEventType().Any((PartyBase party) => party.NumberOfHealthyMembers > 0))
						{
							PlayerEncounter.Update();
							return;
						}
						if (PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
						{
							FlattenedTroopRoster priorityListForLordsHallFightMission = Campaign.Current.Models.SiegeLordsHallFightModel.GetPriorityListForLordsHallFightMission(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount);
							int num = MathF.Max(1, MathF.Min(Campaign.Current.Models.SiegeLordsHallFightModel.MaxAttackerSideTroopCount, MathF.Round((float)priorityListForLordsHallFightMission.Troops.Count() * Campaign.Current.Models.SiegeLordsHallFightModel.AttackerDefenderTroopCountRatio)));
							TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
							MobileParty mobileParty = ((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty);
							troopRoster.Add(mobileParty.MemberRoster);
							foreach (MobileParty attachedParty in mobileParty.AttachedParties)
							{
								troopRoster.Add(attachedParty.MemberRoster);
							}
							TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
							FlattenedTroopRoster flattenedTroopRoster = troopRoster.ToFlattenedRoster();
							flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
							troopRoster2.Add(MobilePartyHelper.GetStrongestAndPriorTroops(flattenedTroopRoster, num, includePlayer: true));
							int minSelectableTroopCount = 1;
							args.MenuContext.OpenTroopSelection(troopRoster, troopRoster2, (CharacterObject character) => !character.IsPlayerCharacter, LordsHallTroopRosterManageDone, num, minSelectableTroopCount);
						}
						else
						{
							PlayerSiege.StartSiegeMission(mapEventSettlement);
						}
					}
				}
			}
			else if (mapEventSettlement.IsVillage)
			{
				PlayerEncounter.StartVillageBattleMission();
			}
			else if (mapEventSettlement.IsHideout)
			{
				CampaignMission.OpenHideoutBattleMission("sea_bandit_a", null);
			}
		}
		else
		{
			bool flag = PlayerEncounter.IsNavalEncounter();
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
			CampaignVec2 position = MobileParty.MainParty.Position;
			MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(in position);
			string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, flag);
			MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch);
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
			rec.TerrainType = (int)faceTerrainType;
			rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
			rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
			rec.NeedsRandomTerrain = false;
			rec.PlayingInCampaignMode = true;
			rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
			rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
			rec.SceneHasMapPatch = true;
			rec.DecalAtlasGroup = 2;
			rec.PatchCoordinates = mapPatchAtPosition.normalizedCoordinates;
			rec.PatchEncounterDir = (battle.AttackerSide.LeaderParty.Position.ToVec2() - battle.DefenderSide.LeaderParty.Position.ToVec2()).Normalized();
			bool flag2 = MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty involvedParty) => involvedParty.Party.IsMobile && (involvedParty.Party.MobileParty.IsCaravan || (involvedParty.Party.Owner != null && involvedParty.Party.Owner.IsMerchant)));
			bool flag3 = MapEvent.PlayerMapEvent.MapEventSettlement == null && MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty involvedParty) => involvedParty.Party.IsMobile && involvedParty.Party.MobileParty.IsVillager);
			if (flag)
			{
				CampaignMission.OpenNavalBattleMission(rec);
			}
			else if (flag2 || flag3)
			{
				CampaignMission.OpenCaravanBattleMission(rec, flag2);
			}
			else
			{
				CampaignMission.OpenBattleMission(rec);
			}
		}
		PlayerEncounter.StartAttackMission();
		MapEvent.PlayerMapEvent.BeginWait();
	}

	private static void LordsHallTroopRosterManageDone(TroopRoster selectedTroops)
	{
		MapEvent.PlayerMapEvent.ResetBattleState();
		int wallLevel = PlayerSiege.BesiegedSettlement.Town.GetWallLevel();
		CampaignMission.OpenSiegeLordsHallFightMission(PlayerSiege.BesiegedSettlement.LocationComplex.GetLocationWithId("lordshall").GetSceneName(wallLevel), selectedTroops.ToFlattenedRoster());
	}

	private static void LordsHallTroopRosterManageDoneForSimulation(TroopRoster selectedTroops)
	{
		EncounterOrderAttack(selectedTroops);
	}

	public static void CheckEnemyAttackableHonorably(MenuCallbackArgs args)
	{
		if ((MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && !PlayerEncounter.PlayerIsDefender)
		{
			IFaction mapFaction = PlayerEncounter.EncounteredParty.MapFaction;
			if (mapFaction != null && mapFaction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				args.IsEnabled = false;
				args.Tooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip");
			}
		}
	}

	public static bool EncounterOrderAttackCondition(MenuCallbackArgs args)
	{
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (playerMapEvent != null)
		{
			args.optionLeaveType = ((!playerMapEvent.IsNavalMapEvent) ? GameMenuOption.LeaveType.OrderTroopsToAttack : GameMenuOption.LeaveType.OrderShipsToAttack);
			MobileParty mobileParty = MapEvent.PlayerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide)[0].Party.MobileParty;
			if (mobileParty != null && mobileParty.IsInRaftState)
			{
				return false;
			}
			CheckEnemyAttackableHonorably(args);
			int num = 0;
			foreach (MapEventParty party in MobileParty.MainParty.MapEventSide.Parties)
			{
				if (!party.Party.IsMobile || !party.Party.MobileParty.IsInRaftState)
				{
					num += party.Party.MemberRoster.Sum((TroopRosterElement x) => x.Character.IsHero ? ((x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded) ? 1 : 0) : (x.Number - x.WoundedNumber));
				}
			}
			if (playerMapEvent.HasTroopsOnBothSides() && playerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide) != null && num > 0)
			{
				int num2 = 0;
				if (!MobileParty.MainParty.IsInRaftState)
				{
					num2 = MobileParty.MainParty.MemberRoster.Sum((TroopRosterElement x) => x.Character.IsHero ? ((x.Character != CharacterObject.PlayerCharacter && !x.Character.HeroObject.IsWounded) ? 1 : 0) : (x.Number - x.WoundedNumber));
				}
				if (num2 > 0)
				{
					if (MobileParty.MainParty.MapEvent.IsNavalMapEvent)
					{
						MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=NFnS5YqQ}Send ships.");
					}
					else
					{
						MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=QfMeoKOm}Send troops.");
					}
				}
				else
				{
					MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=jo3UHKMD}Leave it to the others.");
				}
				if (playerMapEvent.IsInvulnerable)
				{
					playerMapEvent.IsInvulnerable = false;
				}
				if (!MobilePartyHelper.CanPartyAttackWithCurrentMorale(MobileParty.MainParty))
				{
					args.Tooltip = new TextObject("{=xnRtINwH}Your men lack the courage to continue the battle without you. (Low Morale)");
					args.IsEnabled = false;
				}
				else
				{
					IFaction mapFaction = PlayerEncounter.EncounteredParty.MapFaction;
					if (mapFaction == null || mapFaction.NotAttackableByPlayerUntilTime.IsPast)
					{
						args.Tooltip = TooltipHelper.GetSendTroopsPowerContextTooltipForMapEvent();
					}
				}
				return true;
			}
		}
		return false;
	}

	private static void EncounterOrderAttack(TroopRoster selectedTroopsForPlayerSide)
	{
		MapEvent battle = PlayerEncounter.Battle;
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			ISiegeEventSide siegeEventSide = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide.GetOppositeSide());
			if (siegeEventSide != null && !siegeEventSide.GetInvolvedPartiesForEventType().Any((PartyBase party) => party.NumberOfHealthyMembers > 0) && (battle == null || !battle.GetMapEventSide(battle.GetOtherSide(battle.PlayerSide)).Parties.Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0)))
			{
				PlayerEncounter.Update();
				return;
			}
		}
		PartyBase leaderParty = battle.GetLeaderParty(PartyBase.MainParty.OpponentSide);
		MobileParty mobileParty = MobileParty.MainParty.AttachedTo ?? MobileParty.MainParty;
		if (leaderParty.SiegeEvent?.BesiegerCamp != null && !leaderParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(leaderParty) && mobileParty.BesiegerCamp == null)
		{
			mobileParty.BesiegerCamp = leaderParty.SiegeEvent.BesiegerCamp;
		}
		BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, leaderParty);
		if (PlayerEncounter.Current != null)
		{
			GameMenu.ExitToLast();
			if (selectedTroopsForPlayerSide != null && PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
			{
				FlattenedTroopRoster priorityListForLordsHallFightMission = Campaign.Current.Models.SiegeLordsHallFightModel.GetPriorityListForLordsHallFightMission(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount);
				PlayerEncounter.InitSimulation(selectedTroopsForPlayerSide.ToFlattenedRoster(), priorityListForLordsHallFightMission);
			}
			else
			{
				PlayerEncounter.InitSimulation(null, null);
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.BattleSimulation != null)
			{
				((MapState)Game.Current.GameStateManager.ActiveState).StartBattleSimulation();
			}
		}
	}

	public static void EncounterOrderAttackConsequence(MenuCallbackArgs args)
	{
		if (PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
		{
			FlattenedTroopRoster priorityListForLordsHallFightMission = Campaign.Current.Models.SiegeLordsHallFightModel.GetPriorityListForLordsHallFightMission(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount);
			int num = MathF.Max(1, MathF.Min(Campaign.Current.Models.SiegeLordsHallFightModel.MaxAttackerSideTroopCount, MathF.Round((float)priorityListForLordsHallFightMission.Troops.Count() * Campaign.Current.Models.SiegeLordsHallFightModel.AttackerDefenderTroopCountRatio)));
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			MobileParty mobileParty = ((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty);
			troopRoster.Add(mobileParty.MemberRoster);
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				troopRoster.Add(attachedParty.MemberRoster);
			}
			TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
			FlattenedTroopRoster flattenedTroopRoster = troopRoster.ToFlattenedRoster();
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
			troopRoster2.Add(MobilePartyHelper.GetStrongestAndPriorTroops(flattenedTroopRoster, num, includePlayer: false));
			int minSelectableTroopCount = 1;
			args.MenuContext.OpenTroopSelection(troopRoster, troopRoster2, (CharacterObject character) => !character.IsPlayerCharacter, LordsHallTroopRosterManageDoneForSimulation, num, minSelectableTroopCount);
		}
		else
		{
			EncounterOrderAttack(null);
		}
	}

	public static void EncounterCaptureTheEnemyOnConsequence(MenuCallbackArgs args)
	{
		MapEvent.PlayerMapEvent.SetOverrideWinner(MapEvent.PlayerMapEvent.PlayerSide);
		PlayerEncounter.Update();
	}

	public static void EncounterLeaveConsequence()
	{
		Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
		MapEvent mapEvent = ((PlayerEncounter.Battle != null) ? PlayerEncounter.Battle : PlayerEncounter.EncounteredBattle);
		int numberOfInvolvedMen = mapEvent.GetNumberOfInvolvedMen(PartyBase.MainParty.Side);
		PlayerEncounter.Finish(MobileParty.MainParty.CurrentSettlement?.SiegeEvent == null || MobileParty.MainParty.CurrentSettlement?.MapFaction != MobileParty.MainParty.MapFaction);
		if (MobileParty.MainParty.BesiegerCamp != null)
		{
			MobileParty.MainParty.BesiegerCamp = null;
		}
		if (mapEvent != null && !mapEvent.IsFinalized && !mapEvent.IsRaid && numberOfInvolvedMen == PartyBase.MainParty.NumberOfHealthyMembers)
		{
			mapEvent.SimulateBattleSetup(PlayerEncounter.Current?.BattleSimulation?.SelectedTroops);
			mapEvent.SimulateBattleRound((PartyBase.MainParty.Side == BattleSideEnum.Attacker) ? 1 : 0, (PartyBase.MainParty.Side != BattleSideEnum.Attacker) ? 1 : 0);
		}
		if (currentSettlement != null)
		{
			EncounterManager.StartSettlementEncounter(MobileParty.MainParty, currentSettlement);
		}
	}

	public static string GetEncounterCultureBackgroundMesh(CultureObject encounterCulture)
	{
		if (string.IsNullOrEmpty(encounterCulture?.EncounterBackgroundMesh))
		{
			Debug.FailedAssert("Background mesh is invalid for current encounter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetEncounterCultureBackgroundMesh", 718);
			return string.Empty;
		}
		string text = encounterCulture.EncounterBackgroundMesh;
		if (PlayerEncounter.IsNavalEncounter())
		{
			text += "_naval";
		}
		return text;
	}
}
