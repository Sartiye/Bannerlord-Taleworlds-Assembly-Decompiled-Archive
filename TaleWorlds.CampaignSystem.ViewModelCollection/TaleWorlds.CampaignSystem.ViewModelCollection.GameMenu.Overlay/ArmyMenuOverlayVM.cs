using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

[MenuOverlay("ArmyMenuOverlay")]
public class ArmyMenuOverlayVM : GameMenuOverlay
{
	private Army _army;

	private List<MobileParty> _savedPartyList;

	private const float CohesionWarningMin = 30f;

	public Action OpenArmyManagement;

	private readonly Concept _cohesionConceptObj;

	private string _latestTutorialElementID;

	private bool _isVisualsDirty;

	private MBBindingList<GameMenuPartyItemVM> _partyList;

	private string _manCountText;

	private int _cohesion;

	private int _food;

	private bool _isCohesionWarningEnabled;

	private bool _isPlayerArmyLeader;

	private bool _canManageArmy;

	private HintViewModel _manageArmyHint;

	public ElementNotificationVM _tutorialNotification;

	private BasicTooltipViewModel _cohesionHint;

	private BasicTooltipViewModel _manCountHint;

	private BasicTooltipViewModel _foodHint;

	private MBBindingList<StringItemWithHintVM> _issueList;

	private IEnumerable<MobileParty> _mergedPartiesAndLeaderParty
	{
		get
		{
			yield return _army.LeaderParty;
			foreach (MobileParty attachedParty in _army.LeaderParty.AttachedParties)
			{
				yield return attachedParty;
			}
		}
	}

	[DataSourceProperty]
	public ElementNotificationVM TutorialNotification
	{
		get
		{
			return _tutorialNotification;
		}
		set
		{
			if (value != _tutorialNotification)
			{
				_tutorialNotification = value;
				OnPropertyChangedWithValue(value, "TutorialNotification");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ManageArmyHint
	{
		get
		{
			return _manageArmyHint;
		}
		set
		{
			if (value != _manageArmyHint)
			{
				_manageArmyHint = value;
				OnPropertyChangedWithValue(value, "ManageArmyHint");
			}
		}
	}

	[DataSourceProperty]
	public int Cohesion
	{
		get
		{
			return _cohesion;
		}
		set
		{
			if (value != _cohesion)
			{
				_cohesion = value;
				OnPropertyChangedWithValue(value, "Cohesion");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCohesionWarningEnabled
	{
		get
		{
			return _isCohesionWarningEnabled;
		}
		set
		{
			if (value != _isCohesionWarningEnabled)
			{
				_isCohesionWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsCohesionWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool CanManageArmy
	{
		get
		{
			return _canManageArmy;
		}
		set
		{
			if (value != _canManageArmy)
			{
				_canManageArmy = value;
				OnPropertyChangedWithValue(value, "CanManageArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerArmyLeader
	{
		get
		{
			return _isPlayerArmyLeader;
		}
		set
		{
			if (value != _isPlayerArmyLeader)
			{
				_isPlayerArmyLeader = value;
				OnPropertyChangedWithValue(value, "IsPlayerArmyLeader");
			}
		}
	}

	[DataSourceProperty]
	public string ManCountText
	{
		get
		{
			return _manCountText;
		}
		set
		{
			if (value != _manCountText)
			{
				_manCountText = value;
				OnPropertyChangedWithValue(value, "ManCountText");
			}
		}
	}

	[DataSourceProperty]
	public int Food
	{
		get
		{
			return _food;
		}
		set
		{
			if (value != _food)
			{
				_food = value;
				OnPropertyChangedWithValue(value, "Food");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuPartyItemVM> PartyList
	{
		get
		{
			return _partyList;
		}
		set
		{
			if (value != _partyList)
			{
				_partyList = value;
				OnPropertyChangedWithValue(value, "PartyList");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CohesionHint
	{
		get
		{
			return _cohesionHint;
		}
		set
		{
			if (value != _cohesionHint)
			{
				_cohesionHint = value;
				OnPropertyChangedWithValue(value, "CohesionHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ManCountHint
	{
		get
		{
			return _manCountHint;
		}
		set
		{
			if (value != _manCountHint)
			{
				_manCountHint = value;
				OnPropertyChangedWithValue(value, "ManCountHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel FoodHint
	{
		get
		{
			return _foodHint;
		}
		set
		{
			if (value != _foodHint)
			{
				_foodHint = value;
				OnPropertyChangedWithValue(value, "FoodHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringItemWithHintVM> IssueList
	{
		get
		{
			if (_issueList == null)
			{
				_issueList = new MBBindingList<StringItemWithHintVM>();
			}
			return _issueList;
		}
	}

	public ArmyMenuOverlayVM()
	{
		PartyList = new MBBindingList<GameMenuPartyItemVM>();
		base.CurrentOverlayType = 2;
		base.IsInitializationOver = false;
		_army = MobileParty.MainParty.Army ?? MobileParty.MainParty.TargetParty.Army;
		_savedPartyList = new List<MobileParty>();
		CohesionHint = new BasicTooltipViewModel();
		ManCountHint = new BasicTooltipViewModel();
		FoodHint = new BasicTooltipViewModel();
		TutorialNotification = new ElementNotificationVM();
		ManageArmyHint = new HintViewModel();
		Refresh();
		_contextMenuItem = null;
		CampaignEvents.ArmyOverlaySetDirtyEvent.AddNonSerializedListener(this, Refresh);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedAnotherParty);
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		_cohesionConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_army_cohesion");
		base.IsInitializationOver = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TutorialNotification?.RefreshValues();
		Refresh();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.ArmyOverlaySetDirtyEvent.ClearListeners(this);
		CampaignEvents.PartyAttachedAnotherParty.ClearListeners(this);
		CampaignEvents.OnTroopRecruitedEvent.ClearListeners(this);
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	protected override void ExecuteOnSetAsActiveContextMenuItem(GameMenuPartyItemVM troop)
	{
		base.ExecuteOnSetAsActiveContextMenuItem(troop);
		base.ContextList.Clear();
		if (_contextMenuItem.Party.MobileParty?.Army != null && _contextMenuItem.Party.MobileParty.Army.LeaderParty == MobileParty.MainParty && _contextMenuItem.Party.MapEvent == null && _contextMenuItem.Party != _contextMenuItem.Party.MobileParty.Army.LeaderParty.Party)
		{
			TextObject disabledReason;
			bool mapScreenActionIsEnabledWithReason = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.ArmyDismiss.ToString()).ToString(), mapScreenActionIsEnabledWithReason, MenuOverlayContextList.ArmyDismiss, disabledReason));
		}
		float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		CampaignVec2 v = MobileParty.MainParty.MapEvent?.Position ?? MobileParty.MainParty.Position;
		bool flag = troop.Party.MobileParty?.Position.DistanceSquared(v) < getEncounterJoiningRadius * getEncounterJoiningRadius;
		bool flag2 = troop.Party.MobileParty.MapEvent == MobileParty.MainParty.MapEvent;
		bool flag3 = PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
		if (_contextMenuItem.Party.LeaderHero != null && flag && flag2 && !flag3 && _contextMenuItem.Party != PartyBase.MainParty && PlayerEncounter.Current?.BattleSimulation == null)
		{
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.DonateTroops.ToString()).ToString(), enabled: true, MenuOverlayContextList.DonateTroops));
			if (MobileParty.MainParty.CurrentSettlement == null && LocationComplex.Current == null)
			{
				base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.ConverseWithLeader.ToString()).ToString(), enabled: true, MenuOverlayContextList.ConverseWithLeader));
			}
		}
		base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.Encyclopedia.ToString()).ToString(), enabled: true, MenuOverlayContextList.Encyclopedia));
		CharacterObject characterObject = _contextMenuItem.Character ?? _contextMenuItem.Party.LeaderHero?.CharacterObject;
		if (characterObject == null)
		{
			Debug.FailedAssert("ArmyMenuOverlayVM.ExecuteOnSetAsActiveContextMenuItem called on party with no leader hero: " + _contextMenuItem.Party.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteOnSetAsActiveContextMenuItem", 139);
		}
		else
		{
			CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpOpened(characterObject);
		}
	}

	public override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		CanManageArmy = CampaignUIHelper.GetCanManageCurrentArmyWithReason(out var disabledReason);
		ManageArmyHint.HintText = disabledReason;
		for (int i = 0; i < PartyList.Count; i++)
		{
			PartyList[i].RefreshQuestStatus();
		}
		if (_isVisualsDirty)
		{
			RefreshVisualsOfItems();
			_isVisualsDirty = false;
		}
	}

	public sealed override void Refresh()
	{
		if (PartyBase.MainParty.MobileParty.Army != null)
		{
			_army = PartyBase.MainParty.MobileParty.Army;
			base.IsInitializationOver = false;
			UpdateLists();
			UpdateProperties();
			base.IsInitializationOver = true;
		}
	}

	private void UpdateProperties()
	{
		MBTextManager.SetTextVariable("newline", "\n");
		float num = _army.LeaderParty.Food;
		foreach (MobileParty attachedParty in _army.LeaderParty.AttachedParties)
		{
			num += attachedParty.Food;
		}
		Food = (int)num;
		Cohesion = (int)MobileParty.MainParty.Army.Cohesion;
		ManCountText = CampaignUIHelper.GetPartyNameplateText(_army.LeaderParty, includeAttachedParties: true);
		FoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyFoodTooltip(_army));
		CohesionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyCohesionTooltip(_army));
		ManCountHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyManCountTooltip(_army));
		IsCohesionWarningEnabled = MobileParty.MainParty.Army.Cohesion <= 30f;
		IsPlayerArmyLeader = MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty;
	}

	private void UpdateLists()
	{
		List<MobileParty> list = _army.Parties.Except(_savedPartyList).ToList();
		list.Remove(_army.LeaderParty);
		List<MobileParty> list2 = _savedPartyList.Except(_army.Parties).ToList();
		_savedPartyList = _army.Parties.ToList();
		foreach (MobileParty item in list2)
		{
			for (int num = PartyList.Count - 1; num >= 0; num--)
			{
				if (PartyList[num].Party == item.Party)
				{
					PartyList.RemoveAt(num);
					break;
				}
			}
		}
		foreach (MobileParty item2 in list)
		{
			PartyList.Add(new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, item2.Party, canShowQuest: true));
		}
		foreach (GameMenuPartyItemVM party in PartyList)
		{
			party.RefreshProperties();
		}
		if (PartyList.Count <= 0 || PartyList[0].Party == _army.LeaderParty.Party)
		{
			return;
		}
		if (PartyList.SingleOrDefault((GameMenuPartyItemVM p) => p.Party == _army.LeaderParty.Party) != null)
		{
			int index = PartyList.IndexOf(PartyList.SingleOrDefault((GameMenuPartyItemVM p) => p.Party == _army.LeaderParty.Party));
			PartyList.RemoveAt(index);
		}
		GameMenuPartyItemVM gameMenuPartyItemVM = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, _army.LeaderParty.Party, canShowQuest: true)
		{
			IsLeader = true
		};
		PartyList.Insert(0, gameMenuPartyItemVM);
		gameMenuPartyItemVM.RefreshProperties();
	}

	public void ExecuteOpenArmyManagement()
	{
		if (MobileParty.MainParty?.Army != null)
		{
			MobileParty leaderParty = MobileParty.MainParty.Army.LeaderParty;
			if (leaderParty != null && leaderParty.IsMainParty)
			{
				OpenArmyManagement?.Invoke();
			}
		}
	}

	private void ExecuteCohesionLink()
	{
		if (_cohesionConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_cohesionConceptObj.EncyclopediaLink);
		}
		else
		{
			Debug.FailedAssert("Couldn't find Cohesion encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteCohesionLink", 266);
		}
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = string.Empty;
			}
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = _latestTutorialElementID;
			}
		}
	}

	private void RefreshVisualsOfItems()
	{
		for (int i = 0; i < PartyList.Count; i++)
		{
			PartyList[i].RefreshVisual();
		}
	}

	private void OnPartyAttachedAnotherParty(MobileParty party)
	{
		if (party.AttachedTo?.Army != null && party.AttachedTo.Army == MobileParty.MainParty.Army)
		{
			_isVisualsDirty = true;
		}
	}

	private void OnTroopRecruited(Hero recruiterHero, Settlement settlement, Hero troopSource, CharacterObject troop, int number)
	{
		if (recruiterHero?.PartyBelongedTo == null || !recruiterHero.IsPartyLeader)
		{
			return;
		}
		for (int i = 0; i < PartyList.Count; i++)
		{
			if (PartyList[i].Party == recruiterHero.PartyBelongedTo.Party)
			{
				PartyList[i].RefreshProperties();
				break;
			}
		}
	}
}
