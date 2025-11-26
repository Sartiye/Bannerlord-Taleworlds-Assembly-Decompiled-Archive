using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace SandBox.ViewModelCollection;

public class SPScoreboardVM : ScoreboardBaseVM, IBattleObserver
{
	private readonly BattleSimulation _battleSimulation;

	private static readonly TextObject _renownStr = new TextObject("{=eiWQoW9j}You gained {A0} renown.");

	private static readonly TextObject _influenceStr = new TextObject("{=5zeL8sa9}You gained {A0} influence.");

	private static readonly TextObject _moraleStr = new TextObject("{=WAKz9xX8}You gained {A0} morale.");

	private static readonly TextObject _lootStr = new TextObject("{=xu5NA6AW}You earned {A0}% of the loot.");

	private static readonly TextObject _deadLordStr = new TextObject("{=gDKhs4lD}{A0} has died on the battlefield.");

	private static readonly TextObject _figureheadStr = new TextObject("{=ANoYN1yZ}You unlocked the {A0} figurehead.");

	private float _missionEndScoreboardDelayTimer;

	private MBBindingList<BattleResultVM> _battleResults;

	private bool _isPlayerDefendingSiege
	{
		get
		{
			Mission current = Mission.Current;
			if (current != null && current.IsSiegeBattle)
			{
				return Mission.Current.PlayerTeam.IsDefender;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public override MBBindingList<BattleResultVM> BattleResults
	{
		get
		{
			return _battleResults;
		}
		set
		{
			if (value != _battleResults)
			{
				_battleResults = value;
				OnPropertyChangedWithValue(value, "BattleResults");
			}
		}
	}

	public SPScoreboardVM(BattleSimulation simulation)
	{
		_battleSimulation = simulation;
		BattleResults = new MBBindingList<BattleResultVM>();
	}

	protected override void UpdateQuitText()
	{
		if (base.IsOver)
		{
			base.QuitText = GameTexts.FindText("str_done").ToString();
		}
		else if (base.IsMainCharacterDead && !base.IsSimulation)
		{
			base.QuitText = GameTexts.FindText("str_end_battle").ToString();
		}
		else if (_isPlayerDefendingSiege)
		{
			base.QuitText = GameTexts.FindText("str_surrender").ToString();
		}
		else
		{
			base.QuitText = GameTexts.FindText("str_retreat").ToString();
		}
	}

	public override void Initialize(IMissionScreen missionScreen, Mission mission, Action releaseSimulationSources, Action<bool> onToggle)
	{
		base.Initialize(missionScreen, mission, releaseSimulationSources, onToggle);
		if (_battleSimulation != null)
		{
			PlayerSide = (PlayerEncounter.PlayerIsAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			base.Defenders = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "defender"), MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.Banner, isSimulation: true);
			base.Attackers = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "attacker"), MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.Banner, isSimulation: true);
			base.IsSimulation = true;
			base.IsMainCharacterDead = true;
			base.ShowScoreboard = true;
			foreach (List<BattleResultPartyData> team in _battleSimulation.Teams)
			{
				foreach (BattleResultPartyData item in team)
				{
					PartyBase party = item.Party;
					SPScoreboardSideVM side = GetSide(party.Side);
					bool isPlayerParty = party?.Owner == Hero.MainHero;
					foreach (TroopRosterElement item2 in party.MemberRoster.GetTroopRoster())
					{
						side.UpdateScores(party, isPlayerParty, item2.Character, item2.Number - item2.WoundedNumber, 0, 0, 0, 0, 0);
					}
				}
			}
			_battleSimulation.BattleObserver = this;
			base.PowerComparer.Update(base.Defenders.CurrentPower, base.Attackers.CurrentPower, base.Defenders.CurrentPower, base.Attackers.CurrentPower);
		}
		else
		{
			base.IsSimulation = false;
			if (Campaign.Current != null)
			{
				if (PlayerEncounter.Battle != null)
				{
					base.Defenders = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "defender"), MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.Banner, isSimulation: false);
					base.Attackers = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "attacker"), MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.Banner, isSimulation: false);
					PlayerSide = (PlayerEncounter.PlayerIsAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
				}
				else
				{
					base.Defenders = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "defender"), Mission.Current.Teams.Defender.Banner, isSimulation: false);
					base.Attackers = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "attacker"), Mission.Current.Teams.Attacker.Banner, isSimulation: false);
					PlayerSide = BattleSideEnum.Defender;
				}
			}
			else
			{
				Debug.FailedAssert("SPScoreboard on CustomBattle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "Initialize", 116);
			}
			BattleObserverMissionLogic missionBehavior = _mission.GetMissionBehavior<BattleObserverMissionLogic>();
			if (missionBehavior != null)
			{
				missionBehavior.SetObserver(this);
			}
			else
			{
				Debug.FailedAssert("SPScoreboard on CustomBattle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "Initialize", 141);
			}
		}
		string defenderColor;
		string attackerColor;
		if (MobileParty.MainParty.MapEvent != null)
		{
			defenderColor = ((!(MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty?.MapFaction is Kingdom)) ? Color.FromUint(MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.MapFaction?.Banner.GetPrimaryColor() ?? 0).ToString() : Color.FromUint(((Kingdom)MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.MapFaction).PrimaryBannerColor).ToString());
			attackerColor = ((!(MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty?.MapFaction is Kingdom)) ? Color.FromUint(MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction?.Banner.GetPrimaryColor() ?? 0).ToString() : Color.FromUint(((Kingdom)MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction).PrimaryBannerColor).ToString());
		}
		else
		{
			attackerColor = Color.FromUint(Mission.Current.Teams.Attacker.Color).ToString();
			defenderColor = Color.FromUint(Mission.Current.Teams.Defender.Color).ToString();
		}
		base.PowerComparer.SetColors(defenderColor, attackerColor);
		base.MissionTimeInSeconds = -1;
	}

	protected override void OnTick(float dt)
	{
		if (!base.IsSimulation)
		{
			SallyOutEndLogic sallyOutEndLogic = Mission.Current?.GetMissionBehavior<SallyOutEndLogic>();
			if (!base.IsOver)
			{
				Mission mission = _mission;
				if (mission == null || !mission.IsMissionEnding)
				{
					BattleEndLogic battleEndLogic = _battleEndLogic;
					if ((battleEndLogic == null || !battleEndLogic.IsEnemySideRetreating) && (sallyOutEndLogic == null || !sallyOutEndLogic.IsSallyOutOver))
					{
						goto IL_0078;
					}
				}
				if (_missionEndScoreboardDelayTimer < 1.5f)
				{
					_missionEndScoreboardDelayTimer += dt;
				}
				else
				{
					OnBattleOver();
				}
			}
		}
		goto IL_0078;
		IL_0078:
		if (!base.IsSimulation && !base.IsOver)
		{
			base.MissionTimeInSeconds = (int)Mission.Current.CurrentTime;
		}
		if (base.IsSimulation)
		{
			base.Attackers.Morale = MobileParty.MainParty.MapEvent.AttackerSide.GetSideMorale();
			base.Defenders.Morale = MobileParty.MainParty.MapEvent.DefenderSide.GetSideMorale();
		}
		else
		{
			base.Attackers.Morale = GetBattleMoraleOfSide(BattleSideEnum.Attacker);
			base.Defenders.Morale = GetBattleMoraleOfSide(BattleSideEnum.Defender);
		}
	}

	public override void ExecutePlayAction()
	{
		if (base.IsSimulation)
		{
			_battleSimulation.Play();
		}
	}

	public override void ExecuteFastForwardAction()
	{
		if (base.IsSimulation)
		{
			base.IsPaused = false;
			if (!base.IsFastForwarding)
			{
				_battleSimulation.Play();
			}
			else
			{
				_battleSimulation.FastForward();
			}
		}
		else
		{
			Mission.Current.SetFastForwardingFromUI(base.IsFastForwarding);
		}
	}

	public override void ExecutePauseSimulationAction()
	{
		if (base.IsSimulation)
		{
			base.IsFastForwarding = false;
			if (!base.IsPaused)
			{
				_battleSimulation.Play();
			}
			else
			{
				_battleSimulation.Pause();
			}
		}
	}

	public override void ExecuteEndSimulationAction()
	{
		if (base.IsSimulation)
		{
			base.IsPaused = false;
			base.IsFastForwarding = false;
			_battleSimulation.Skip();
		}
	}

	public override void ExecuteQuitAction()
	{
		OnExitBattle();
	}

	private void GetBattleRewards(bool playerVictory)
	{
		BattleResults.Clear();
		if (playerVictory)
		{
			ExplainedNumber renownExplained = new ExplainedNumber(0f, includeDescriptions: true);
			ExplainedNumber influencExplained = new ExplainedNumber(0f, includeDescriptions: true);
			ExplainedNumber moraleExplained = new ExplainedNumber(0f, includeDescriptions: true);
			PlayerEncounter.GetBattleRewards(out var renownChange, out var influenceChange, out var moraleChange, out var _, out var playerEarnedLootPercentage, out var playerEarnedFigurehead, ref renownExplained, ref influencExplained, ref moraleExplained);
			if (renownChange > 0.1f)
			{
				BattleResults.Add(new BattleResultVM(_renownStr.Format(renownChange), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref renownExplained)));
			}
			if (influenceChange > 0.1f)
			{
				BattleResults.Add(new BattleResultVM(_influenceStr.Format(influenceChange), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref influencExplained)));
			}
			if (moraleChange > 0.1f || moraleChange < -0.1f)
			{
				BattleResults.Add(new BattleResultVM(_moraleStr.Format(moraleChange), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref moraleExplained)));
			}
			int num = ((PlayerSide == BattleSideEnum.Attacker) ? base.Attackers.Parties.Count : base.Defenders.Parties.Count);
			if (playerEarnedLootPercentage > 0.1f && num > 1)
			{
				BattleResults.Add(new BattleResultVM(_lootStr.Format(playerEarnedLootPercentage), () => SandBoxUIHelper.GetBattleLootAwardTooltip(playerEarnedLootPercentage)));
			}
			if (playerEarnedFigurehead != null)
			{
				if (playerEarnedFigurehead?.Name != null)
				{
					BattleResults.Add(new BattleResultVM(_figureheadStr.SetTextVariable("A0", playerEarnedFigurehead.Name?.ToString() ?? "").ToString(), () => SandBoxUIHelper.GetFigureheadTooltip(playerEarnedFigurehead)));
				}
				else
				{
					Debug.FailedAssert("Battle rewards contain an invalid figurehead (null or name missing)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 330);
				}
			}
		}
		foreach (SPScoreboardPartyVM party in base.Defenders.Parties)
		{
			foreach (SPScoreboardUnitVM item in party.Members.Where((SPScoreboardUnitVM member) => member.IsHero && member.Score.Dead > 0))
			{
				if (item.Character == null)
				{
					Debug.FailedAssert("Scoreboard has a member element without a character", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 347);
					continue;
				}
				BattleResults.Add(new BattleResultVM(_deadLordStr.SetTextVariable("A0", item.Character.Name).ToString(), () => new List<TooltipProperty>(), SandBoxUIHelper.GetCharacterCode(item.Character as CharacterObject)));
			}
		}
		foreach (SPScoreboardPartyVM party2 in base.Attackers.Parties)
		{
			foreach (SPScoreboardUnitVM item2 in party2.Members.Where((SPScoreboardUnitVM member) => member.IsHero && member.Score.Dead > 0))
			{
				if (item2.Character == null)
				{
					Debug.FailedAssert("Scoreboard has a member element without a character", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 364);
					continue;
				}
				BattleResults.Add(new BattleResultVM(_deadLordStr.SetTextVariable("A0", item2.Character.Name).ToString(), () => new List<TooltipProperty>(), SandBoxUIHelper.GetCharacterCode(item2.Character as CharacterObject)));
			}
		}
	}

	private void UpdateSimulationResult(bool playerVictory)
	{
		if (base.IsSimulation)
		{
			if (playerVictory)
			{
				if (PlayerEncounter.Battle.PartiesOnSide(PlayerSide).Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers) < 70)
				{
					base.SimulationResult = "SimulationVictorySmall";
				}
				else
				{
					base.SimulationResult = "SimulationVictoryLarge";
				}
			}
			else
			{
				base.SimulationResult = "SimulationDefeat";
			}
		}
		else
		{
			base.SimulationResult = "NotSimulation";
		}
	}

	public void OnBattleOver()
	{
		BattleResultType battleResultType = BattleResultType.NotOver;
		if (PlayerEncounter.IsActive && PlayerEncounter.Battle != null)
		{
			base.IsOver = true;
			bool playerVictory = false;
			if (PlayerEncounter.WinningSide == PlayerSide)
			{
				battleResultType = BattleResultType.Victory;
				playerVictory = true;
			}
			else
			{
				CampaignBattleResult campaignBattleResult = PlayerEncounter.CampaignBattleResult;
				battleResultType = ((campaignBattleResult != null && campaignBattleResult.EnemyPulledBack) ? BattleResultType.Retreat : BattleResultType.Defeat);
			}
			GetBattleRewards(playerVictory);
		}
		else
		{
			Mission current = Mission.Current;
			if (current != null && current.MissionEnded)
			{
				base.IsOver = true;
				battleResultType = (((Mission.Current.HasMissionBehavior<SallyOutEndLogic>() && !Mission.Current.MissionResult.BattleResolved) || Mission.Current.MissionResult.PlayerVictory) ? BattleResultType.Victory : ((Mission.Current.MissionResult.BattleState == BattleState.DefenderPullBack && Mission.Current.PlayerTeam.Side == BattleSideEnum.Attacker) ? BattleResultType.Retreat : BattleResultType.Defeat));
			}
			else
			{
				BattleEndLogic battleEndLogic = _battleEndLogic;
				if (battleEndLogic != null && battleEndLogic.IsEnemySideRetreating)
				{
					base.IsOver = true;
				}
			}
		}
		switch (battleResultType)
		{
		case BattleResultType.Defeat:
			base.BattleResult = GameTexts.FindText("str_defeat").ToString();
			base.BattleResultIndex = (int)battleResultType;
			break;
		case BattleResultType.Victory:
			if (PlayerEncounter.Battle != null && PlayerEncounter.Battle.EndedByRetreat)
			{
				base.BattleResult = ((PlayerEncounter.Battle.RetreatingSide == BattleSideEnum.Attacker) ? GameTexts.FindText("str_attackers_retreated").ToString() : GameTexts.FindText("str_defenders_retreated").ToString());
			}
			else
			{
				base.BattleResult = GameTexts.FindText("str_victory").ToString();
			}
			base.BattleResultIndex = (int)battleResultType;
			break;
		case BattleResultType.Retreat:
			base.BattleResult = GameTexts.FindText("str_battle_result_retreat").ToString();
			base.BattleResultIndex = (int)battleResultType;
			break;
		}
		if (battleResultType != BattleResultType.NotOver)
		{
			UpdateSimulationResult(battleResultType == BattleResultType.Victory || battleResultType == BattleResultType.Retreat);
		}
	}

	public void OnExitBattle()
	{
		if (base.IsSimulation)
		{
			if (_battleSimulation.IsSimulationFinished)
			{
				_releaseSimulationSources();
				_battleSimulation.OnFinished();
				return;
			}
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_order_Retreat").ToString(), GameTexts.FindText("str_retreat_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
				_releaseSimulationSources();
				_battleSimulation.OnPlayerRetreat();
			}, delegate
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}));
			return;
		}
		BattleEndLogic missionBehavior = _mission.GetMissionBehavior<BattleEndLogic>();
		BasicMissionHandler missionBehavior2 = _mission.GetMissionBehavior<BasicMissionHandler>();
		BattleEndLogic.ExitResult exitResult = (BattleEndLogic.ExitResult)(((int?)missionBehavior?.TryExit()) ?? ((!_mission.MissionEnded) ? 1 : 3));
		switch (exitResult)
		{
		case BattleEndLogic.ExitResult.NeedsPlayerConfirmation:
		case BattleEndLogic.ExitResult.SurrenderSiege:
			OnToggle(obj: false);
			missionBehavior2.CreateWarningWidgetForResult(exitResult);
			return;
		case BattleEndLogic.ExitResult.False:
			InformationManager.ShowInquiry(_retreatInquiryData);
			return;
		}
		if (missionBehavior == null && exitResult == BattleEndLogic.ExitResult.True)
		{
			_mission.EndMission();
		}
	}

	public void TroopNumberChanged(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject character, int number = 0, int numberDead = 0, int numberWounded = 0, int numberRouted = 0, int numberKilled = 0, int numberReadyToUpgrade = 0)
	{
		bool isPlayerParty = (battleCombatant as PartyBase)?.Owner == Hero.MainHero;
		GetSide(side).UpdateScores(battleCombatant, isPlayerParty, character, number, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		base.PowerComparer.Update(base.Defenders.CurrentPower, base.Attackers.CurrentPower, base.Defenders.InitialPower, base.Attackers.InitialPower);
	}

	public void HeroSkillIncreased(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject heroCharacter, SkillObject upgradedSkill)
	{
		bool isPlayerParty = (battleCombatant as PartyBase)?.Owner == Hero.MainHero;
		GetSide(side).UpdateHeroSkills(battleCombatant, isPlayerParty, heroCharacter, upgradedSkill);
	}

	public void BattleResultsReady()
	{
		if (!base.IsOver)
		{
			OnBattleOver();
		}
	}

	public void TroopSideChanged(BattleSideEnum prevSide, BattleSideEnum newSide, IBattleCombatant battleCombatant, BasicCharacterObject character)
	{
		SPScoreboardStatsVM scoreToBringOver = GetSide(prevSide).RemoveTroop(battleCombatant, character);
		GetSide(newSide).GetPartyAddIfNotExists(battleCombatant, (battleCombatant as PartyBase)?.Owner == Hero.MainHero);
		GetSide(newSide).AddTroop(battleCombatant, character, scoreToBringOver);
	}
}
