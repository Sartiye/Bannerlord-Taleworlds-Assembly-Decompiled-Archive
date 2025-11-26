using System;
using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.GameState;

public interface IMenuContextHandler
{
	void OnBackgroundMeshNameSet(string name);

	void OnOpenTownManagement();

	void OnOpenRecruitVolunteers();

	void OnOpenTournamentLeaderboard();

	void OnOpenTroopSelection(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount);

	void OnMenuCreate();

	void OnMenuActivate();

	void OnMenuRefresh();

	void OnHourlyTick();

	void OnPanelSoundIDSet(string panelSoundID);

	void OnAmbientSoundIDSet(string ambientSoundID);
}
