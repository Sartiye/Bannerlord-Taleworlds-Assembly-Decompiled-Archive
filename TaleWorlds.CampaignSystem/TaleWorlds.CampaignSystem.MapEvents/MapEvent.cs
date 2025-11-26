using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem.MapEvents;

public sealed class MapEvent : MBObjectBase
{
	public enum BattleTypes
	{
		None,
		FieldBattle,
		Raid,
		IsForcingVolunteers,
		IsForcingSupplies,
		Siege,
		Hideout,
		SallyOut,
		SiegeOutside,
		BlockadeBattle,
		BlockadeSallyOutBattle
	}

	public enum PowerCalculationContext
	{
		PlainBattle,
		SteppeBattle,
		DesertBattle,
		DuneBattle,
		SnowBattle,
		ForestBattle,
		RiverCrossingBattle,
		Village,
		Siege,
		SeaBattle,
		OpenSeaBattle,
		RiverBattle,
		Estimated
	}

	[SaveableField(101)]
	private MapEventState _state;

	[SaveableField(102)]
	private MapEventSide[] _sides = new MapEventSide[2];

	public const float SiegeAdvantage = 1.5f;

	public bool DiplomaticallyFinished;

	[SaveableField(107)]
	private CampaignTime _nextSimulationTime;

	[SaveableField(108)]
	private CampaignTime _mapEventStartTime;

	[SaveableField(110)]
	private BattleTypes _mapEventType;

	[CachedData]
	private TerrainType _eventTerrainType;

	[CachedData]
	public IMapEventVisual MapEventVisual;

	private bool _playerFigureheadCalculated;

	private bool _mapEventResultsApplied;

	private bool _mapEventResultsCalculated;

	[SaveableField(114)]
	private bool _isVisible;

	private bool _keepSiegeEvent;

	[SaveableField(116)]
	private bool FirstUpdateIsDone;

	[SaveableField(117)]
	private BattleState _battleState;

	private bool _isFinishCalled;

	private MapEventResultExplainer _battleResultExplainers;

	[SaveableField(125)]
	public float[] StrengthOfSide = new float[2];

	public TroopUpgradeTracker TroopUpgradeTracker { get; private set; } = new TroopUpgradeTracker();


	public static MapEvent PlayerMapEvent => MobileParty.MainParty?.MapEvent;

	public BattleSideEnum PlayerSide => PartyBase.MainParty.Side;

	internal IBattleObserver BattleObserver { get; set; }

	[SaveableProperty(105)]
	public MapEventComponent Component { get; private set; }

	public MapEventState State
	{
		get
		{
			return _state;
		}
		private set
		{
			if (_state != value)
			{
				if (IsPlayerMapEvent)
				{
					Debug.Print("Player MapEvent State: " + value);
				}
				_state = value;
			}
		}
	}

	public MapEventSide AttackerSide => _sides[1];

	public MapEventSide DefenderSide => _sides[0];

	public IEnumerable<PartyBase> InvolvedParties
	{
		get
		{
			MapEventSide[] sides = _sides;
			foreach (MapEventSide mapEventSide in sides)
			{
				foreach (MapEventParty party in mapEventSide.Parties)
				{
					yield return party.Party;
				}
			}
		}
	}

	[SaveableProperty(103)]
	public Settlement MapEventSettlement { get; private set; }

	[SaveableProperty(76)]
	public BattleSideEnum RetreatingSide { get; private set; } = BattleSideEnum.None;


	public bool EndedByRetreat
	{
		get
		{
			if (RetreatingSide != BattleSideEnum.None)
			{
				return PursuitRoundNumber == 0;
			}
			return false;
		}
	}

	[SaveableProperty(75)]
	public int PursuitRoundNumber { get; private set; }

	public int UpdateCount => WonRounds.Count;

	internal PowerCalculationContext SimulationContext
	{
		get
		{
			if (Component != null)
			{
				return Component.SimulationContext;
			}
			if (_mapEventType == BattleTypes.Siege)
			{
				return PowerCalculationContext.Siege;
			}
			return Campaign.Current.Models.MilitaryPowerModel.GetContextForPosition(Position);
		}
	}

	[SaveableProperty(118)]
	public CampaignVec2 Position { get; private set; }

	public BattleTypes EventType => _mapEventType;

	public TerrainType EventTerrainType => _eventTerrainType;

	[SaveableProperty(113)]
	public bool IsInvulnerable { get; set; }

	public bool IsFieldBattle => _mapEventType == BattleTypes.FieldBattle;

	public bool IsRaid => _mapEventType == BattleTypes.Raid;

	public bool IsForcingVolunteers => _mapEventType == BattleTypes.IsForcingVolunteers;

	public bool IsForcingSupplies => _mapEventType == BattleTypes.IsForcingSupplies;

	public bool IsSiegeAssault => _mapEventType == BattleTypes.Siege;

	public bool IsHideoutBattle => _mapEventType == BattleTypes.Hideout;

	public bool IsSallyOut => _mapEventType == BattleTypes.SallyOut;

	public bool IsSiegeOutside => _mapEventType == BattleTypes.SiegeOutside;

	public bool IsBlockade => _mapEventType == BattleTypes.BlockadeBattle;

	public bool IsBlockadeSallyOut => _mapEventType == BattleTypes.BlockadeSallyOutBattle;

	public bool IsSiegeAmbush => Component is SiegeAmbushEventComponent;

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		private set
		{
			_isVisible = value;
			MapEventVisual?.SetVisibility(value);
		}
	}

	public bool IsPlayerMapEvent => this == PlayerMapEvent;

	public BattleState BattleState
	{
		get
		{
			return _battleState;
		}
		internal set
		{
			if (value != _battleState)
			{
				if (IsPlayerMapEvent)
				{
					Debug.Print("Player MapEvent BattleState: " + value);
				}
				_battleState = value;
				if (_battleState == BattleState.AttackerVictory || _battleState == BattleState.DefenderVictory)
				{
					OnBattleWon();
				}
			}
		}
	}

	public MapEventSide Winner
	{
		get
		{
			if (BattleState != BattleState.AttackerVictory)
			{
				if (BattleState != BattleState.DefenderVictory)
				{
					return null;
				}
				return DefenderSide;
			}
			return AttackerSide;
		}
	}

	public BattleSideEnum WinningSide
	{
		get
		{
			if (BattleState != BattleState.AttackerVictory)
			{
				if (BattleState != BattleState.DefenderVictory)
				{
					return BattleSideEnum.None;
				}
				return BattleSideEnum.Defender;
			}
			return BattleSideEnum.Attacker;
		}
	}

	public BattleSideEnum DefeatedSide
	{
		get
		{
			if (BattleState != BattleState.AttackerVictory)
			{
				if (BattleState != BattleState.DefenderVictory)
				{
					return BattleSideEnum.None;
				}
				return BattleSideEnum.Attacker;
			}
			return BattleSideEnum.Defender;
		}
	}

	public MapEventResultExplainer BattleResultExplainers => _battleResultExplainers;

	public bool IsFinalized => _state == MapEventState.WaitingRemoval;

	public CampaignTime BattleStartTime => _mapEventStartTime;

	public bool HasWinner
	{
		get
		{
			if (BattleState != BattleState.AttackerVictory)
			{
				return BattleState == BattleState.DefenderVictory;
			}
			return true;
		}
	}

	[SaveableProperty(123)]
	public bool IsPlayerSimulation { get; set; }

	public bool IsNavalMapEvent => !Position.IsOnLand;

	[SaveableProperty(126)]
	public MBList<BattleSideEnum> WonRounds { get; private set; } = new MBList<BattleSideEnum>();


	internal static void AutoGeneratedStaticCollectObjectsMapEvent(object o, List<object> collectedObjects)
	{
		((MapEvent)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(StrengthOfSide);
		collectedObjects.Add(_sides);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_nextSimulationTime, collectedObjects);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_mapEventStartTime, collectedObjects);
		collectedObjects.Add(Component);
		collectedObjects.Add(MapEventSettlement);
		CampaignVec2.AutoGeneratedStaticCollectObjectsCampaignVec2(Position, collectedObjects);
		collectedObjects.Add(WonRounds);
	}

	internal static object AutoGeneratedGetMemberValueComponent(object o)
	{
		return ((MapEvent)o).Component;
	}

	internal static object AutoGeneratedGetMemberValueMapEventSettlement(object o)
	{
		return ((MapEvent)o).MapEventSettlement;
	}

	internal static object AutoGeneratedGetMemberValueRetreatingSide(object o)
	{
		return ((MapEvent)o).RetreatingSide;
	}

	internal static object AutoGeneratedGetMemberValuePursuitRoundNumber(object o)
	{
		return ((MapEvent)o).PursuitRoundNumber;
	}

	internal static object AutoGeneratedGetMemberValuePosition(object o)
	{
		return ((MapEvent)o).Position;
	}

	internal static object AutoGeneratedGetMemberValueIsInvulnerable(object o)
	{
		return ((MapEvent)o).IsInvulnerable;
	}

	internal static object AutoGeneratedGetMemberValueIsPlayerSimulation(object o)
	{
		return ((MapEvent)o).IsPlayerSimulation;
	}

	internal static object AutoGeneratedGetMemberValueWonRounds(object o)
	{
		return ((MapEvent)o).WonRounds;
	}

	internal static object AutoGeneratedGetMemberValueStrengthOfSide(object o)
	{
		return ((MapEvent)o).StrengthOfSide;
	}

	internal static object AutoGeneratedGetMemberValue_state(object o)
	{
		return ((MapEvent)o)._state;
	}

	internal static object AutoGeneratedGetMemberValue_sides(object o)
	{
		return ((MapEvent)o)._sides;
	}

	internal static object AutoGeneratedGetMemberValue_nextSimulationTime(object o)
	{
		return ((MapEvent)o)._nextSimulationTime;
	}

	internal static object AutoGeneratedGetMemberValue_mapEventStartTime(object o)
	{
		return ((MapEvent)o)._mapEventStartTime;
	}

	internal static object AutoGeneratedGetMemberValue_mapEventType(object o)
	{
		return ((MapEvent)o)._mapEventType;
	}

	internal static object AutoGeneratedGetMemberValue_isVisible(object o)
	{
		return ((MapEvent)o)._isVisible;
	}

	internal static object AutoGeneratedGetMemberValueFirstUpdateIsDone(object o)
	{
		return ((MapEvent)o).FirstUpdateIsDone;
	}

	internal static object AutoGeneratedGetMemberValue_battleState(object o)
	{
		return ((MapEvent)o)._battleState;
	}

	public void BeginWait()
	{
		State = MapEventState.Wait;
	}

	public MapEventSide GetMapEventSide(BattleSideEnum side)
	{
		return _sides[(int)side];
	}

	internal TroopRoster GetMemberRosterReceivingLootShare(PartyBase party)
	{
		return _sides[(int)party.Side].MemberRosterForPlayerLootShare(party);
	}

	internal TroopRoster GetPrisonerRosterReceivingLootShare(PartyBase party)
	{
		return _sides[(int)party.Side].PrisonerRosterForPlayerLootShare(party);
	}

	internal ItemRoster GetItemRosterReceivingLootShare(PartyBase party)
	{
		return _sides[(int)party.Side].ItemRosterForPlayerLootShare(party);
	}

	public MBReadOnlyList<MapEventParty> PartiesOnSide(BattleSideEnum side)
	{
		return _sides[(int)side].Parties;
	}

	public void GetBattleRewards(PartyBase party, out float renownChange, out float influenceChange, out float moraleChange, out float goldChange, out float playerEarnedLootPercentage)
	{
		renownChange = 0f;
		influenceChange = 0f;
		moraleChange = 0f;
		goldChange = 0f;
		playerEarnedLootPercentage = 0f;
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			foreach (MapEventParty party2 in sides[i].Parties)
			{
				if (party == party2.Party)
				{
					renownChange = party2.GainedRenown;
					influenceChange = party2.GainedInfluence;
					moraleChange = party2.MoraleChange;
					goldChange = party2.PlunderedGold - party2.GoldLost;
					float num = GetMapEventSide(party2.Party.Side).CalculateTotalContribution();
					playerEarnedLootPercentage = (int)(100f * ((float)party2.ContributionToBattle / num));
				}
			}
		}
	}

	internal MapEvent()
	{
		MapEventVisual = Campaign.Current.VisualCreator.CreateMapEventVisual(this);
	}

	public override string ToString()
	{
		return string.Concat("Battle: ", AttackerSide.LeaderParty?.Name, " x ", DefenderSide.LeaderParty.Name);
	}

	[LateLoadInitializationCallback]
	private void OnLateLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		if (Component == null && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.0"))
		{
			if (_mapEventType == BattleTypes.Raid)
			{
				float nextSettlementDamage = (float)objectLoadData.GetMemberValueBySaveId(109);
				int lootedItemCount = (int)objectLoadData.GetMemberValueBySaveId(112);
				float raidDamage = (float)objectLoadData.GetMemberValueBySaveId(115);
				Component = RaidEventComponent.CreateComponentForOldSaves(this, nextSettlementDamage, lootedItemCount, raidDamage);
			}
			else if (_mapEventType == BattleTypes.IsForcingSupplies)
			{
				Component = ForceSuppliesEventComponent.CreateComponentForOldSaves(this);
			}
			else if (_mapEventType == BattleTypes.IsForcingVolunteers)
			{
				Component = ForceVolunteersEventComponent.CreateComponentForOldSaves(this);
			}
			else if (_mapEventType == BattleTypes.Hideout)
			{
				Component = HideoutEventComponent.CreateComponentForOldSaves(this, isSendTroops: false);
			}
			else if (_mapEventType == BattleTypes.FieldBattle)
			{
				Component = FieldBattleEventComponent.CreateComponentForOldSaves(this);
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			WonRounds = new MBList<BattleSideEnum>();
			RetreatingSide = BattleSideEnum.None;
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			Vec2 pos = (Vec2)objectLoadData.GetMemberValueBySaveId(111);
			Position = new CampaignVec2(pos, isOnLand: true);
		}
		Component?.AfterLoad(this);
	}

	internal void OnAfterLoad()
	{
		_eventTerrainType = (TerrainType)Position.Face.FaceGroupIndex;
		CacheSimulationData();
		CacheSimulationLeaderModifiers();
		if (!IsFinalized)
		{
			MapEventVisual = Campaign.Current.VisualCreator.CreateMapEventVisual(this);
			MapEventVisual.Initialize(Position, GetBattleSizeValue(), IsVisible);
		}
		if (TroopUpgradeTracker == null)
		{
			TroopUpgradeTracker = new TroopUpgradeTracker();
			MapEventSide[] sides = _sides;
			for (int i = 0; i < sides.Length; i++)
			{
				foreach (MapEventParty party in sides[i].Parties)
				{
					TroopUpgradeTracker.AddParty(party);
				}
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.0"))
		{
			if (!AttackerSide.Parties.Any() || !DefenderSide.Parties.Any())
			{
				if (InvolvedParties.ContainsQ(PlayerEncounter.EncounteredParty))
				{
					PlayerEncounter.Finish();
				}
				FinalizeEvent();
			}
			if (MapEventSettlement != null)
			{
				if (IsRaid && MapEventSettlement.Party.MapEvent == null)
				{
					FinalizeEvent();
				}
				else if (EventType == BattleTypes.Siege && MapEventSettlement.SiegeEvent == null)
				{
					FinalizeEvent();
				}
			}
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && !IsPlayerMapEvent)
		{
			MapEventSide[] sides = _sides;
			for (int i = 0; i < sides.Length; i++)
			{
				sides[i].CommitXpGains();
			}
		}
	}

	internal void Initialize(PartyBase attackerParty, PartyBase defenderParty, MapEventComponent component = null, BattleTypes mapEventType = BattleTypes.None)
	{
		Component = component;
		FirstUpdateIsDone = false;
		RetreatingSide = BattleSideEnum.None;
		PursuitRoundNumber = 0;
		MapEventSettlement = null;
		_mapEventType = mapEventType;
		_sides[0] = new MapEventSide(this, BattleSideEnum.Defender, defenderParty);
		_sides[1] = new MapEventSide(this, BattleSideEnum.Attacker, attackerParty);
		if (attackerParty.MobileParty == MobileParty.MainParty || defenderParty.MobileParty == MobileParty.MainParty)
		{
			if (mapEventType == BattleTypes.Raid)
			{
				Debug.Print(string.Concat("A raid mapEvent has been started on ", defenderParty.Name, "\n"), 0, Debug.DebugColor.DarkGreen, 64uL);
			}
			else if (defenderParty.IsSettlement && defenderParty.Settlement.IsFortification)
			{
				Debug.Print(string.Concat("A siege mapEvent has been started on ", defenderParty.Name, "\n"), 0, Debug.DebugColor.DarkCyan, 64uL);
			}
		}
		if (attackerParty.IsMobile && attackerParty.MobileParty.CurrentSettlement != null)
		{
			MapEventSettlement = attackerParty.MobileParty.CurrentSettlement;
		}
		else if (defenderParty.IsMobile && defenderParty.MobileParty.CurrentSettlement != null)
		{
			MapEventSettlement = defenderParty.MobileParty.CurrentSettlement;
		}
		else if ((!attackerParty.IsMobile || attackerParty.MobileParty.BesiegedSettlement == null) && defenderParty.IsMobile)
		{
			_ = defenderParty.MobileParty.BesiegedSettlement;
		}
		if (attackerParty.IsSettlement)
		{
			MapEventSettlement = attackerParty.Settlement;
		}
		else if (defenderParty.IsSettlement)
		{
			MapEventSettlement = defenderParty.Settlement;
			MapEventSettlement.LastAttackerParty = attackerParty.MobileParty;
		}
		if (IsFieldBattle)
		{
			MapEventSettlement = null;
			if (!IsNavalMapEvent && (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty))
			{
				float settlementBeingNearFieldBattleRadius = Campaign.Current.Models.EncounterModel.GetSettlementBeingNearFieldBattleRadius;
				Village village = SettlementHelper.FindNearestVillageToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement x) => x.Position.DistanceSquared(attackerParty.Position) < settlementBeingNearFieldBattleRadius * settlementBeingNearFieldBattleRadius);
				if (village != null)
				{
					MapEventSettlement = village.Settlement;
					if (Campaign.Current.Models.MapDistanceModel.GetDistance(attackerParty.MobileParty, MapEventSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out var estimatedLandRatio) > settlementBeingNearFieldBattleRadius * 1.5f || Campaign.Current.Models.MapDistanceModel.GetDistance(defenderParty.MobileParty, MapEventSettlement, isTargetingPort: false, MobileParty.NavigationType.Default, out estimatedLandRatio) > settlementBeingNearFieldBattleRadius * 1.5f)
					{
						MapEventSettlement = null;
					}
				}
			}
		}
		if (IsBlockade || IsBlockadeSallyOut)
		{
			Position = defenderParty.MobileParty.BesiegedSettlement.PortPosition;
			MapEventSettlement = defenderParty.MobileParty.BesiegedSettlement;
		}
		else
		{
			Position = attackerParty.Position;
		}
		CacheSimulationData();
		attackerParty.MapEventSide = AttackerSide;
		defenderParty.MapEventSide = DefenderSide;
		if (MapEventSettlement != null && (mapEventType == BattleTypes.Siege || mapEventType == BattleTypes.SiegeOutside || mapEventType == BattleTypes.SallyOut || IsSiegeAmbush))
		{
			foreach (PartyBase item in MapEventSettlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(mapEventType))
			{
				if (item.MapEventSide == null && (item != PartyBase.MainParty || item.MobileParty.Army != null) && (item.MobileParty.Army == null || item.MobileParty.Army.LeaderParty == item.MobileParty))
				{
					item.MapEventSide = ((mapEventType == BattleTypes.SallyOut) ? defenderParty.MapEventSide : attackerParty.MapEventSide);
				}
			}
		}
		if (defenderParty.IsMobile && defenderParty.MobileParty.BesiegedSettlement != null)
		{
			List<PartyBase> involvedPartiesForEventType = defenderParty.MobileParty.SiegeEvent.GetInvolvedPartiesForEventType(_mapEventType);
			PartyBase partyBase = (IsSiegeAssault ? attackerParty : defenderParty);
			foreach (PartyBase item2 in involvedPartiesForEventType)
			{
				if (item2 != partyBase && item2.IsMobile && item2 != PartyBase.MainParty && item2.MobileParty.BesiegedSettlement == defenderParty.MobileParty.BesiegedSettlement && (item2.MobileParty.Army == null || item2.MobileParty.Army.LeaderParty == item2.MobileParty))
				{
					item2.MapEventSide = DefenderSide;
				}
			}
		}
		State = MapEventState.Wait;
		_mapEventStartTime = CampaignTime.Now;
		_nextSimulationTime = CalculateNextSimulationTime();
		if (MapEventSettlement != null && !IsBlockade)
		{
			AddInsideSettlementParties(MapEventSettlement);
		}
		Component?.InitializeComponent();
		MapEventVisual.Initialize(Position, GetBattleSizeValue(), IsVisible);
		BattleState = BattleState.None;
		CacheSimulationLeaderModifiers();
		CampaignEventDispatcher.Instance.OnMapEventStarted(this, attackerParty, defenderParty);
	}

	internal bool IsWinnerSide(BattleSideEnum side)
	{
		if (BattleState != BattleState.DefenderVictory || side != 0)
		{
			if (BattleState == BattleState.AttackerVictory)
			{
				return side == BattleSideEnum.Attacker;
			}
			return false;
		}
		return true;
	}

	private void AddInsideSettlementParties(Settlement relatedSettlement)
	{
		List<PartyBase> list = new List<PartyBase>();
		foreach (PartyBase item in relatedSettlement.GetInvolvedPartiesForEventType(_mapEventType))
		{
			if (item != PartyBase.MainParty && item.MobileParty?.AttachedTo != MobileParty.MainParty)
			{
				list.Add(item);
			}
		}
		foreach (PartyBase item2 in list)
		{
			if (MapEventSettlement.SiegeEvent != null)
			{
				if (MapEventSettlement.SiegeEvent.CanPartyJoinSide(item2, BattleSideEnum.Defender))
				{
					if (IsSallyOut || IsBlockadeSallyOut)
					{
						item2.MapEventSide = AttackerSide;
					}
					else
					{
						item2.MapEventSide = DefenderSide;
					}
				}
				else if (item2.MobileParty != null && !item2.MobileParty.IsGarrison && !item2.MobileParty.IsMilitia)
				{
					LeaveSettlementAction.ApplyForParty(item2.MobileParty);
					item2.MobileParty.SetMoveModeHold();
				}
			}
			else if (CanPartyJoinBattle(item2, BattleSideEnum.Defender))
			{
				item2.MapEventSide = DefenderSide;
			}
			else if (CanPartyJoinBattle(item2, BattleSideEnum.Attacker))
			{
				item2.MapEventSide = AttackerSide;
			}
			else if (item2.MobileParty != null && !item2.MobileParty.IsGarrison && !item2.MobileParty.IsMilitia)
			{
				LeaveSettlementAction.ApplyForParty(item2.MobileParty);
			}
		}
	}

	private int GetBattleSizeValue()
	{
		if (IsSiegeAssault)
		{
			return 4;
		}
		int numberOfInvolvedMen = GetNumberOfInvolvedMen();
		if (numberOfInvolvedMen < 30)
		{
			return 0;
		}
		if (numberOfInvolvedMen < 80)
		{
			return 1;
		}
		if (numberOfInvolvedMen >= 120)
		{
			return 3;
		}
		return 2;
	}

	private static CampaignTime CalculateNextSimulationTime()
	{
		return CampaignTime.Now + CampaignTime.Minutes(30L);
	}

	internal void AddInvolvedPartyInternal(MapEventParty mapEventParty, BattleSideEnum side)
	{
		if (mapEventParty.Party == PartyBase.MainParty)
		{
			TroopUpgradeTracker = new TroopUpgradeTracker();
			MapEventSide[] sides = _sides;
			for (int i = 0; i < sides.Length; i++)
			{
				foreach (MapEventParty party2 in sides[i].Parties)
				{
					TroopUpgradeTracker.AddParty(party2);
				}
			}
		}
		else
		{
			TroopUpgradeTracker?.AddParty(mapEventParty);
		}
		PartyBase party = mapEventParty.Party;
		if (IsSiegeAssault && party.MobileParty != null && party.MobileParty.CurrentSettlement == null && side == BattleSideEnum.Defender)
		{
			_mapEventType = BattleTypes.SiegeOutside;
		}
		if (party.MobileParty != null && party.MobileParty.IsGarrison && side == BattleSideEnum.Attacker && (IsSiegeOutside || IsBlockade))
		{
			_mapEventType = (IsSiegeOutside ? BattleTypes.SallyOut : BattleTypes.BlockadeSallyOutBattle);
			MapEventSettlement = party.MobileParty.CurrentSettlement;
		}
		if (party == MobileParty.MainParty.Party && !IsSiegeAssault && !IsRaid)
		{
			party.MobileParty.SetMoveModeHold();
		}
		if (party == PartyBase.MainParty)
		{
			party.MobileParty.ForceAiNoPathMode = false;
		}
		RecalculateRenownAndInfluenceValues(party);
		if (IsFieldBattle && party.IsMobile && party.MobileParty.BesiegedSettlement == null)
		{
			int sideIndex = GetMapEventSide(side).Parties.Count((MapEventParty p) => p.Party.IsMobile) - 1;
			SetPartyBaseEventLocalPosition(party, side, sideIndex);
		}
		party.SetVisualAsDirty();
		if (party.IsMobile && party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == party.MobileParty)
		{
			foreach (MobileParty attachedParty in party.MobileParty.Army.LeaderParty.AttachedParties)
			{
				attachedParty.Party.SetVisualAsDirty();
			}
		}
		if (HasWinner && party.MapEventSide.MissionSide != WinningSide && party.NumberOfHealthyMembers > 0)
		{
			BattleState = BattleState.None;
		}
		if (party.IsVisible)
		{
			IsVisible = true;
		}
		ResetUnsuitablePartiesThatWereTargetingThisMapEvent();
		Component?.OnPartyAdded(party);
		CampaignEventDispatcher.Instance.OnPartyAddedToMapEvent(party);
	}

	private void SetPartyBaseEventLocalPosition(PartyBase party, BattleSideEnum side, int sideIndex)
	{
		float eventDistance;
		Vec2 eventDirection = GetEventDirection(side, out eventDistance);
		Vec2 vec = Position.ToVec2() - eventDirection * Math.Max(eventDistance * 0.5f, 0.4f) - (party.Position.ToVec2() + party.MobileParty.ArmyPositionAdder);
		if (party != GetMapEventSide(side).LeaderParty)
		{
			party.MobileParty.EventPositionAdder = vec + (sideIndex + 1) / 2 * ((sideIndex % 2 == 0) ? 1 : (-1)) * eventDirection.RightVec() * (IsNavalMapEvent ? 0.8f : 0.4f);
		}
		else
		{
			party.MobileParty.EventPositionAdder = vec;
		}
	}

	private Vec2 GetEventDirection(BattleSideEnum side, out float eventDistance)
	{
		MapEventSide mapEventSide = GetMapEventSide(side);
		Vec2 result = mapEventSide.OtherSide.LeaderParty.Position.ToVec2() - mapEventSide.LeaderParty.Position.ToVec2();
		if (result.Length < 1E-05f)
		{
			result = ((side != 0) ? GetLeaderParty(BattleSideEnum.Attacker).MobileParty.Bearing : (-GetLeaderParty(BattleSideEnum.Attacker).MobileParty.Bearing));
			result.Normalize();
			eventDistance = 0.1f;
		}
		else
		{
			result.Normalize();
			eventDistance = result.Length;
		}
		return result;
	}

	internal void PartyVisibilityChanged(PartyBase party, bool isPartyVisible)
	{
		if (isPartyVisible)
		{
			IsVisible = true;
			return;
		}
		bool isVisible = false;
		foreach (PartyBase involvedParty in InvolvedParties)
		{
			if (involvedParty != party && involvedParty.IsVisible)
			{
				isVisible = true;
				break;
			}
		}
		IsVisible = isVisible;
	}

	internal void RemoveInvolvedPartyInternal(MapEventParty mapEventParty)
	{
		TroopUpgradeTracker?.RemoveParty(mapEventParty);
		if (mapEventParty.Party == PartyBase.MainParty)
		{
			TroopUpgradeTracker = null;
		}
		PartyBase party = mapEventParty.Party;
		party.SetVisualAsDirty();
		if (party.IsMobile && party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == party.MobileParty)
		{
			foreach (MobileParty attachedParty in party.MobileParty.Army.LeaderParty.AttachedParties)
			{
				attachedParty.Party.SetVisualAsDirty();
			}
		}
		if (IsFieldBattle && party.IsMobile && party.MobileParty.BesiegedSettlement == null)
		{
			party.MobileParty.EventPositionAdder = Vec2.Zero;
			MapEventSide[] sides = _sides;
			foreach (MapEventSide mapEventSide in sides)
			{
				for (int j = 0; j < mapEventSide.Parties.Count; j++)
				{
					if (mapEventSide.Parties[j].Party.IsMobile && mapEventSide.Parties[j].Party != mapEventSide.LeaderParty)
					{
						SetPartyBaseEventLocalPosition(mapEventSide.Parties[j].Party, mapEventSide.MissionSide, j);
					}
				}
			}
		}
		if (IsSiegeOutside)
		{
			MapEventSide mapEventSide2 = ((MapEventSettlement != null) ? DefenderSide : AttackerSide);
			if (mapEventSide2.Parties.All((MapEventParty x) => x.Party.MobileParty == null || (MapEventSettlement != null && x.Party.MobileParty.CurrentSettlement == MapEventSettlement)) && MapEventSettlement != null)
			{
				_mapEventType = BattleTypes.Siege;
			}
		}
		if (party == PartyBase.MainParty && State == MapEventState.Wait)
		{
			AttackerSide.RemoveNearbyPartiesFromPlayerMapEvent();
			DefenderSide.RemoveNearbyPartiesFromPlayerMapEvent();
		}
		if (party.IsVisible)
		{
			PartyVisibilityChanged(party, isPartyVisible: false);
		}
		ResetUnsuitablePartiesThatWereTargetingThisMapEvent();
		if (party.IsMobile && !party.MobileParty.IsInRaftState && !party.MobileParty.IsCurrentlyUsedByAQuest && party.SiegeEvent == null && (party.MobileParty.Army == null || party.MobileParty.Army.LeaderParty == party.MobileParty))
		{
			party.MobileParty.SetMoveModeHold();
		}
	}

	public int GetNumberOfInvolvedMen()
	{
		return DefenderSide.RecalculateMemberCountOfSide() + AttackerSide.RecalculateMemberCountOfSide();
	}

	public int GetNumberOfInvolvedMen(BattleSideEnum side)
	{
		return GetMapEventSide(side).RecalculateMemberCountOfSide();
	}

	private void CalculateRenownShares(MapEventResultExplainer resultExplainers = null)
	{
		if (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory)
		{
			((BattleState == BattleState.AttackerVictory) ? AttackerSide : DefenderSide).DistributeRenownAndInfluence(resultExplainers);
		}
	}

	private bool TickBattleSimulation(BattleSideEnum side, float advantage, float strikerSideMorale, float struckSideMorale)
	{
		bool flag = false;
		bool num = SimulateSingleTroopHit(side, advantage, strikerSideMorale, struckSideMorale);
		flag = SimulateSiegeEnginesHit(side, advantage, strikerSideMorale, struckSideMorale);
		return num || flag;
	}

	private bool SimulateSiegeEnginesHit(BattleSideEnum side, float advantage, float strikerSideMorale, float struckSideMorale)
	{
		MapEventSide mapEventSide = _sides[(int)side];
		MapEventSide mapEventSide2 = _sides[(int)(1 - side)];
		bool result = false;
		if (IsNavalMapEvent && mapEventSide.NumRemainingSimulationSiegeEngines > 0 && mapEventSide2.NumRemainingSimulationShips > 0)
		{
			(SiegeEngineType, Ship) randomSimulationSiegeEngine = mapEventSide.GetRandomSimulationSiegeEngine();
			result = SimulateShipHit(mapEventSide, mapEventSide2, randomSimulationSiegeEngine.Item2, randomSimulationSiegeEngine.Item1, advantage, strikerSideMorale, struckSideMorale);
		}
		return result;
	}

	private bool SimulateShipHit(MapEventSide strikerSide, MapEventSide struckSide, Ship strikerShip, SiegeEngineType siegeEngine, float advantage, float strikerSideMorale, float struckSideMorale)
	{
		bool flag = MBRandom.RandomFloat < Campaign.Current.Models.CombatSimulationModel.GetShipSiegeEngineHitChance(strikerShip, siegeEngine, strikerSide.MissionSide);
		if (flag)
		{
			Ship randomSimulationShip = struckSide.GetRandomSimulationShip();
			PartyBase owner = strikerShip.Owner;
			PartyBase owner2 = randomSimulationShip.Owner;
			int troopCasualties;
			int damage = (int)Campaign.Current.Models.CombatSimulationModel.SimulateHit(strikerShip, randomSimulationShip, owner, owner2, siegeEngine, advantage, this, out troopCasualties).ResultNumber;
			bool isFinishingStrike = struckSide.ApplySimulationDamageToShip(damage, randomSimulationShip, siegeEngine, owner);
			strikerSide.ApplySimulatedHitRewardToShip(strikerShip, randomSimulationShip, siegeEngine, damage, isFinishingStrike);
			for (int i = 0; i < troopCasualties; i++)
			{
				if (struckSide.NumRemainingSimulationTroops <= 0)
				{
					break;
				}
				bool flag2 = SimulateSingleTroopHit(strikerSide.MissionSide, advantage, strikerSideMorale, struckSideMorale);
				_ = IsPlayerSimulation && flag2;
			}
		}
		return flag;
	}

	private bool SimulateSingleTroopHit(BattleSideEnum side, float strikerAdvantage, float strikerSideMorale, float struckSideMorale)
	{
		MapEventSide mapEventSide = _sides[(int)side];
		MapEventSide mapEventSide2 = _sides[(int)(1 - side)];
		UniqueTroopDescriptor uniqueTroopDescriptor = mapEventSide.SelectRandomSimulationTroop();
		UniqueTroopDescriptor uniqueTroopDescriptor2 = mapEventSide2.SelectRandomSimulationTroop();
		CharacterObject allocatedTroop = mapEventSide.GetAllocatedTroop(uniqueTroopDescriptor);
		CharacterObject allocatedTroop2 = mapEventSide2.GetAllocatedTroop(uniqueTroopDescriptor2);
		PartyBase allocatedTroopParty = mapEventSide.GetAllocatedTroopParty(uniqueTroopDescriptor);
		PartyBase allocatedTroopParty2 = mapEventSide2.GetAllocatedTroopParty(uniqueTroopDescriptor2);
		int num = (int)Campaign.Current.Models.CombatSimulationModel.SimulateHit(allocatedTroop, allocatedTroop2, allocatedTroopParty, allocatedTroopParty2, strikerAdvantage, this, strikerSideMorale, struckSideMorale).ResultNumber;
		bool flag = false;
		if (num > 0)
		{
			if (IsPlayerSimulation && allocatedTroopParty2 == PartyBase.MainParty)
			{
				float playerTroopsReceivedDamageMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
				num = MBRandom.RoundRandomized((float)num * playerTroopsReceivedDamageMultiplier);
			}
			DamageTypes damageType = ((MBRandom.RandomFloat < Campaign.Current.Models.CombatSimulationModel.GetBluntDamageChance(allocatedTroop, allocatedTroop2, allocatedTroopParty, allocatedTroopParty2, this)) ? DamageTypes.Blunt : DamageTypes.Cut);
			flag = mapEventSide2.ApplySimulationDamageToSelectedTroop(num, damageType, allocatedTroopParty);
			mapEventSide.ApplySimulatedHitRewardToSelectedTroop(allocatedTroop, allocatedTroop2, num, flag);
			if (IsPlayerSimulation && allocatedTroopParty == PartyBase.MainParty && flag)
			{
				CampaignEventDispatcher.Instance.OnPlayerPartyKnockedOrKilledTroop(allocatedTroop2);
			}
		}
		return flag;
	}

	internal void Update()
	{
		if (_isFinishCalled)
		{
			return;
		}
		bool finish = false;
		if (_sides[0].LeaderParty == null || _sides[1].LeaderParty == null || !_sides[0].LeaderParty.MapFaction.IsAtWarWith(_sides[1].LeaderParty.MapFaction))
		{
			DiplomaticallyFinished = true;
		}
		if (DefenderSide.LeaderParty != null && DefenderSide.LeaderParty.IsMobile && DefenderSide.LeaderParty.MobileParty.IsInRaftState)
		{
			BattleState = BattleState.AttackerVictory;
			finish = true;
		}
		if (!DiplomaticallyFinished)
		{
			Component?.Update(ref finish);
			if (((DefenderSide.TroopCount > 0 && AttackerSide.TroopCount > 0) || (!FirstUpdateIsDone && (DefenderSide.TroopCount > 0 || _mapEventType != BattleTypes.Raid))) && _nextSimulationTime.IsPast)
			{
				CheckRunAway();
				SimulateBattleSessionForMapEvent();
				_nextSimulationTime = CalculateNextSimulationTime();
				FirstUpdateIsDone = true;
				finish = RetreatingSide != BattleSideEnum.None && PursuitRoundNumber == 0;
			}
			if ((_mapEventType != BattleTypes.Raid || DefenderSide.Parties.Count > 1) && BattleState != 0)
			{
				finish = true;
			}
		}
		else
		{
			finish = true;
			foreach (PartyBase involvedParty in InvolvedParties)
			{
				if (involvedParty.IsMobile && involvedParty.MobileParty != MobileParty.MainParty && (involvedParty.MobileParty.Army == null || involvedParty.MobileParty.Army.LeaderParty == involvedParty.MobileParty))
				{
					involvedParty.MobileParty.RecalculateShortTermBehavior();
				}
			}
		}
		if (finish)
		{
			Component?.FinishComponent();
			if (!IsPlayerMapEvent || PlayerEncounter.Current == null)
			{
				FinishBattle();
			}
		}
	}

	public void FinishBattleAndKeepSiegeEvent()
	{
		_keepSiegeEvent = true;
		FinishBattle();
	}

	private void CheckSiegeStageChange()
	{
		if (MapEventSettlement != null && IsSiegeAssault)
		{
			int num = AttackerSide.Parties.Sum((MapEventParty party) => party.Party.NumberOfHealthyMembers);
			int num2 = DefenderSide.Parties.Sum((MapEventParty party) => party.Party.NumberOfHealthyMembers);
			if (num == 0)
			{
			}
		}
	}

	public void SimulateBattleSetup(FlattenedTroopRoster[] priorTroops)
	{
		if (IsSiegeAssault)
		{
			CheckSiegeStageChange();
		}
		MapEventSide[] sides = _sides;
		foreach (MapEventSide mapEventSide in sides)
		{
			FlattenedTroopRoster flattenedTroopRoster = ((priorTroops != null) ? priorTroops[(int)mapEventSide.MissionSide] : null);
			mapEventSide.MakeReadyForSimulation(flattenedTroopRoster, flattenedTroopRoster?.Count() ?? (-1));
		}
		_battleState = BattleState.None;
	}

	public void SimulateBattleRound(int simulationTicksDefender, int simulationTicksAttacker)
	{
		Campaign.Current.Models.CombatSimulationModel.GetBattleAdvantage(this, out var defenderAdvantage, out var attackerAdvantage);
		int troopCasualties = AttackerSide.TroopCasualties;
		int troopCasualties2 = DefenderSide.TroopCasualties;
		int shipCasualties = AttackerSide.ShipCasualties;
		int shipCasualties2 = DefenderSide.ShipCasualties;
		float sideMorale = AttackerSide.GetSideMorale();
		float sideMorale2 = DefenderSide.GetSideMorale();
		CalculateWinner(out var showResults, sideMorale, sideMorale2);
		int num = 0;
		while (0 < simulationTicksAttacker + simulationTicksDefender && BattleState == BattleState.None && !showResults)
		{
			float num2 = (float)simulationTicksAttacker / (float)(simulationTicksAttacker + simulationTicksDefender);
			if (MBRandom.RandomFloat < num2)
			{
				simulationTicksAttacker--;
				TickBattleSimulation(BattleSideEnum.Attacker, attackerAdvantage.ResultNumber, sideMorale, sideMorale2);
			}
			else
			{
				simulationTicksDefender--;
				TickBattleSimulation(BattleSideEnum.Defender, defenderAdvantage.ResultNumber, sideMorale2, sideMorale);
			}
			CalculateWinner(out showResults, sideMorale, sideMorale2);
			num++;
		}
		if (!HasWinner && PursuitRoundNumber > 0)
		{
			PursuitRoundNumber--;
			if (PursuitRoundNumber == 0)
			{
				EndByRunAway();
			}
		}
		if (showResults)
		{
			BattleObserver?.BattleResultsReady();
		}
		ApplyRoundEffects(troopCasualties, troopCasualties2, shipCasualties, shipCasualties2);
	}

	private void CalculateSimulationMoraleEffects(MobileParty strikerParty, MobileParty struckParty, ref ExplainedNumber effectiveDamage, MapEvent battle)
	{
		float sideMorale = strikerParty.MapEventSide.GetSideMorale();
		float sideMorale2 = struckParty.MapEventSide.GetSideMorale();
		float num = TaleWorlds.Library.MathF.Min(sideMorale - 50f, 0f);
		float num2 = TaleWorlds.Library.MathF.Max(sideMorale2 - 50f, 0f);
		effectiveDamage.AddFactor((num - num2) * 0.005f);
	}

	private void SimulateBattleSessionForMapEvent()
	{
		SimulateBattleSetup(null);
		SimulateBattleRoundInternal();
		SimulateBattleRoundEndSession();
	}

	internal void SimulatePlayerEncounterBattle()
	{
		CheckRunAway();
		SimulateBattleRoundInternal();
	}

	private void SimulateBattleRoundInternal()
	{
		var (simulationTicksDefender, simulationTicksAttacker) = Campaign.Current.Models.CombatSimulationModel.GetSimulationTicksForBattleRound(this);
		SimulateBattleRound(simulationTicksDefender, simulationTicksAttacker);
	}

	private void SimulateBattleRoundEndSession()
	{
		MapEventSide[] sides = _sides;
		foreach (MapEventSide mapEventSide in sides)
		{
			if (!_mapEventResultsCalculated)
			{
				mapEventSide.CommitXpGains();
			}
			mapEventSide.EndSimulation();
		}
	}

	private bool CheckRunAway()
	{
		CheckSideRunAway(AttackerSide);
		CheckSideRunAway(DefenderSide);
		return RetreatingSide != BattleSideEnum.None;
	}

	private void CheckSideRunAway(MapEventSide mapEventSide)
	{
		if (RetreatingSide == BattleSideEnum.None && Campaign.Current.Models.EncounterModel.GetMapEventSideRunAwayChance(mapEventSide) > MBRandom.RandomFloat)
		{
			RetreatingSide = mapEventSide.MissionSide;
			PursuitRoundNumber = Campaign.Current.Models.CombatSimulationModel.GetPursuitRoundCount(this);
		}
	}

	private void OnBattleWon()
	{
		CalculateMapEventResults();
		if (!IsPlayerMapEvent)
		{
			CalculateAndCommitMapEventResults();
		}
		BattleObserver?.BattleResultsReady();
	}

	private BattleSideEnum CalculateRoundWinner(int attackerTroopCasualtiesAtRoundStart, int defenderTroopCasualtiesAtRoundStart, int attackerShipCasualtiesAtRoundStart, int defenderShipCasualtiesAtRoundStart)
	{
		if (BattleState == BattleState.AttackerVictory)
		{
			return BattleSideEnum.Attacker;
		}
		if (BattleState == BattleState.DefenderVictory)
		{
			return BattleSideEnum.Defender;
		}
		BattleSideEnum result = BattleSideEnum.None;
		int num = AttackerSide.TroopCasualties + AttackerSide.ShipCasualties - attackerTroopCasualtiesAtRoundStart - attackerShipCasualtiesAtRoundStart;
		int num2 = DefenderSide.TroopCasualties + DefenderSide.ShipCasualties - defenderTroopCasualtiesAtRoundStart - defenderShipCasualtiesAtRoundStart;
		if ((float)num > (float)num2 * 1.3f && num > num2 + 1 && (float)num > (float)(AttackerSide.NumRemainingSimulationTroops + num) * 0.02f)
		{
			result = BattleSideEnum.Defender;
		}
		else if ((float)num2 > (float)num * 1.3f && num2 > num + 1 && (float)num2 > (float)(DefenderSide.NumRemainingSimulationTroops + num2) * 0.02f)
		{
			result = BattleSideEnum.Attacker;
		}
		return result;
	}

	private void ApplyRoundEffects(int attackerTroopCasualtiesAtRoundStart, int defenderTroopCasualtiesAtRoundStart, int attackerShipCasualtiesAtRoundStart, int defenderShipCasualtiesAtRoundStart)
	{
		BattleSideEnum battleSideEnum = CalculateRoundWinner(attackerTroopCasualtiesAtRoundStart, defenderTroopCasualtiesAtRoundStart, attackerShipCasualtiesAtRoundStart, defenderShipCasualtiesAtRoundStart);
		WonRounds.Add(battleSideEnum);
		AttackerSide.OnRoundEnd(battleSideEnum);
		DefenderSide.OnRoundEnd(battleSideEnum);
	}

	private void CalculateWinner(out bool showResults, float attackerSideMorale, float defenderSideMorale)
	{
		BattleState battleState = BattleState.None;
		BattleSideEnum battleSideEnum = BattleSideEnum.None;
		int numRemainingSimulationTroops = AttackerSide.NumRemainingSimulationTroops;
		int numRemainingSimulationTroops2 = DefenderSide.NumRemainingSimulationTroops;
		bool flag = false;
		if (numRemainingSimulationTroops2 == 0 || (IsNavalMapEvent && DefenderSide.NumRemainingSimulationShips == 0))
		{
			battleState = BattleState.AttackerVictory;
			battleSideEnum = BattleSideEnum.Attacker;
		}
		else if (numRemainingSimulationTroops == 0 || (IsNavalMapEvent && AttackerSide.NumRemainingSimulationShips == 0))
		{
			battleState = BattleState.DefenderVictory;
			battleSideEnum = BattleSideEnum.Defender;
		}
		else
		{
			PartyBase leaderParty = DefenderSide.LeaderParty;
			if (leaderParty != null && leaderParty.IsMobile && defenderSideMorale.ApproximatelyEqualsTo(0f))
			{
				battleState = BattleState.AttackerVictory;
				battleSideEnum = BattleSideEnum.Attacker;
				flag = true;
			}
			else
			{
				PartyBase leaderParty2 = AttackerSide.LeaderParty;
				if (leaderParty2 != null && leaderParty2.IsMobile && attackerSideMorale.ApproximatelyEqualsTo(0f))
				{
					battleState = BattleState.DefenderVictory;
					battleSideEnum = BattleSideEnum.Defender;
					flag = true;
				}
			}
		}
		foreach (MapEventParty party in DefenderSide.Parties)
		{
			if (party.Party.IsMobile && party.Party.MobileParty.IsInRaftState)
			{
				battleState = BattleState.AttackerVictory;
				battleSideEnum = BattleSideEnum.Attacker;
				break;
			}
		}
		showResults = battleSideEnum != BattleSideEnum.None && !Hero.MainHero.IsWounded && InvolvedParties.Contains(PartyBase.MainParty) && PartyBase.MainParty.Side != battleSideEnum;
		if (battleState != BattleState.None && flag)
		{
			GetMapEventSide(battleSideEnum.GetOppositeSide()).Route();
		}
		BattleState = battleState;
	}

	public void SetOverrideWinner(BattleSideEnum winner)
	{
		BattleState = winner switch
		{
			BattleSideEnum.Defender => BattleState.DefenderVictory, 
			BattleSideEnum.Attacker => BattleState.AttackerVictory, 
			_ => BattleState.None, 
		};
	}

	public void SetDefenderPulledBack()
	{
		BattleState = BattleState.DefenderPullBack;
	}

	public void ResetBattleState()
	{
		BattleState = BattleState.None;
	}

	internal bool CheckIfOneSideHasLost()
	{
		int num = DefenderSide.RecalculateMemberCountOfSide();
		int num2 = AttackerSide.RecalculateMemberCountOfSide();
		if (BattleState == BattleState.None && (num == 0 || num2 == 0))
		{
			BattleState = ((num2 <= 0) ? BattleState.DefenderVictory : BattleState.AttackerVictory);
		}
		if (BattleState != BattleState.AttackerVictory)
		{
			return BattleState == BattleState.DefenderVictory;
		}
		return true;
	}

	internal ItemRoster ItemRosterForPlayerLootShare(PartyBase party)
	{
		return GetMapEventSide(party.Side).ItemRosterForPlayerLootShare(party);
	}

	public bool IsPlayerSergeant()
	{
		if (IsPlayerMapEvent && GetLeaderParty(PlayerSide) != PartyBase.MainParty && MobileParty.MainParty.Army != null)
		{
			return MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty;
		}
		return false;
	}

	private void FinishBattle()
	{
		_isFinishCalled = true;
		FinalizeEventAux();
	}

	internal void CalculateAndCommitMapEventResults()
	{
		if (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory)
		{
			MBList<MapEventParty> defeatedParties = GetMapEventSide(DefeatedSide).Parties.ToMBList();
			MBList<MapEventParty> winnerParties = GetMapEventSide(WinningSide).Parties.ToMBList();
			LootDefeatedPartyCasualties(winnerParties, defeatedParties);
			LootDefeatedPartyItems(winnerParties, defeatedParties);
			LootDefeatedPartyPrisoners(winnerParties, defeatedParties);
			LootDefeatedPartyShips(winnerParties, defeatedParties);
			LootDefeatedPartyMembers(winnerParties, defeatedParties);
			ControlAndUpdateDefeatedPartiesAfterBattle(defeatedParties);
			CommitCalculatedMapEventResults();
		}
		_mapEventResultsApplied = true;
	}

	private void CalculateMapEventResults()
	{
		_mapEventResultsCalculated = true;
		if (IsPlayerMapEvent)
		{
			_battleResultExplainers = new MapEventResultExplainer();
		}
		if (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory)
		{
			MBList<MapEventParty> defeatedParties = GetMapEventSide(DefeatedSide).Parties.ToMBList();
			MBList<MapEventParty> winnerParties = GetMapEventSide(WinningSide).Parties.ToMBList();
			LootDefeatedPartyGold(winnerParties, defeatedParties);
			CalculateRenownShares(_battleResultExplainers);
			CalculatePlayerFigureheadShare(defeatedParties, GetMapEventSide(DefeatedSide).LeaderParty);
		}
	}

	private void LootDefeatedPartyGold(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		int num = 0;
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			int num2 = Campaign.Current.Models.BattleRewardModel.CalculatePlunderedGoldAmountFromDefeatedParty(defeatedParty.Party);
			if (num2 > 0)
			{
				num += num2;
				defeatedParty.GoldLost = num2;
			}
		}
		if (num <= 0)
		{
			return;
		}
		foreach (KeyValuePair<MapEventParty, float> lootGoldChance in Campaign.Current.Models.BattleRewardModel.GetLootGoldChances(winnerParties))
		{
			float value = lootGoldChance.Value;
			int num3 = (int)((float)num * value);
			if (num3 > 0)
			{
				lootGoldChance.Key.PlunderedGold = num3;
			}
		}
	}

	private void LootDefeatedPartyMembers(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		if (RetreatingSide != BattleSideEnum.None)
		{
			return;
		}
		bool isSurrendered = GetMapEventSide(DefeatedSide).IsSurrendered;
		MBReadOnlyList<KeyValuePair<MapEventParty, float>> lootMemberChancesForWinnerParties = Campaign.Current.Models.BattleRewardModel.GetLootMemberChancesForWinnerParties(winnerParties);
		float mainPartyMemberScatterChance = Campaign.Current.Models.BattleRewardModel.GetMainPartyMemberScatterChance();
		for (int num = defeatedParties.Count - 1; num >= 0; num--)
		{
			PartyBase party = defeatedParties[num].Party;
			if (lootMemberChancesForWinnerParties.Count > 0)
			{
				for (int num2 = party.MemberRoster.Count - 1; num2 >= 0; num2--)
				{
					TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(num2);
					if (elementCopyAtIndex.Number != 0)
					{
						CharacterObject character = elementCopyAtIndex.Character;
						if (character.IsHero)
						{
							Hero heroObject = character.HeroObject;
							if (heroObject != Hero.MainHero && heroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle && heroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInLabor && heroObject.Occupation != Occupation.Special)
							{
								bool flag = false;
								if (party.IsMobile && party.LeaderHero == heroObject)
								{
									party.MobileParty.RemovePartyLeader();
								}
								if (heroObject.CanBecomePrisoner() && (party != PartyBase.MainParty || MBRandom.RandomFloat > mainPartyMemberScatterChance))
								{
									TroopRoster troopRoster = FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootMemberChancesForWinnerParties)?.RosterToReceiveLootPrisoners;
									if (troopRoster != null)
									{
										flag = true;
										if (troopRoster.OwnerParty != null)
										{
											TakePrisonerAction.Apply(troopRoster.OwnerParty, heroObject);
										}
										else
										{
											troopRoster.AddToCounts(character, 1);
											party.MemberRoster.AddToCountsAtIndex(num2, -elementCopyAtIndex.Number, 0, 0, removeDepleted: false);
										}
									}
								}
								if (!flag)
								{
									if (heroObject.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
									{
										if (heroObject.IsAlive)
										{
											MakeHeroFugitiveAction.Apply(heroObject, showNotification: true);
										}
										else if (heroObject.IsDead)
										{
											Debug.FailedAssert("There is un-handled situation for hero check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MapEvents\\MapEvent.cs", "LootDefeatedPartyMembers", 1759);
										}
									}
									else if (heroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.Lost && heroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge && heroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInLabor)
									{
										Debug.Print($"Hero with name {heroObject.Name} not handled in loot member part because detail of:  {heroObject.DeathMark}");
										Debug.FailedAssert("There is un-handled member distribution after battle, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MapEvents\\MapEvent.cs", "LootDefeatedPartyMembers", 1770);
									}
								}
							}
						}
						else
						{
							for (int i = 0; i < elementCopyAtIndex.WoundedNumber; i++)
							{
								(FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootMemberChancesForWinnerParties)?.RosterToReceiveLootPrisoners)?.AddToCounts(character, 1, insertAtFront: false, 1);
							}
							if (isSurrendered)
							{
								for (int j = 0; j < elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber; j++)
								{
									(FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootMemberChancesForWinnerParties)?.RosterToReceiveLootPrisoners)?.AddToCounts(character, 1);
								}
							}
							party.MemberRoster.AddToCountsAtIndex(num2, -elementCopyAtIndex.Number, -elementCopyAtIndex.WoundedNumber, 0, removeDepleted: false);
						}
					}
				}
			}
			else
			{
				for (int num3 = party.MemberRoster.Count - 1; num3 >= 0; num3--)
				{
					TroopRosterElement elementCopyAtIndex2 = party.MemberRoster.GetElementCopyAtIndex(num3);
					if (elementCopyAtIndex2.Number > 0)
					{
						if (!elementCopyAtIndex2.Character.IsHero)
						{
							party.MemberRoster.AddToCountsAtIndex(num3, -elementCopyAtIndex2.Number, -elementCopyAtIndex2.WoundedNumber, 0, removeDepleted: false);
						}
						else if (elementCopyAtIndex2.Character.HeroObject != Hero.MainHero && elementCopyAtIndex2.Character.HeroObject.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
						{
							MakeHeroFugitiveAction.Apply(elementCopyAtIndex2.Character.HeroObject);
						}
					}
				}
			}
			if (party == PartyBase.MainParty)
			{
				PartyBase party2 = TaleWorlds.Core.Extensions.MaxBy(winnerParties.WhereQ((MapEventParty x) => x.Party.MemberRoster.TotalManCount > 0), (MapEventParty x) => x.ContributionToBattle).Party;
				if (party2.IsMobile && (party2.MobileParty.IsMilitia || party2.MobileParty.IsGarrison))
				{
					party2 = party2.MobileParty.HomeSettlement.Party;
				}
				TakePrisonerAction.Apply(party2, Hero.MainHero);
			}
			party.MemberRoster.RemoveZeroCounts();
		}
	}

	private void CalculatePlayerFigureheadShare(MBList<MapEventParty> defeatedParties, PartyBase defeatedLeaderParty)
	{
		if (IsPlayerMapEvent && IsNavalMapEvent && WinningSide == PlayerSide && !_playerFigureheadCalculated)
		{
			_playerFigureheadCalculated = true;
			Figurehead figureheadLoot = Campaign.Current.Models.BattleRewardModel.GetFigureheadLoot(defeatedParties, defeatedLeaderParty);
			PlayerEncounter.Current.PlayerLootedFigurehead = figureheadLoot;
		}
	}

	private void LootDefeatedPartyPrisoners(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			if (defeatedParty.Party.PrisonRoster.Count <= 0)
			{
				continue;
			}
			TroopRoster prisonRoster = defeatedParty.Party.PrisonRoster;
			MBList<TroopRosterElement> troopRoster = prisonRoster.GetTroopRoster();
			for (int num = troopRoster.Count - 1; num >= 0; num--)
			{
				TroopRosterElement prisonerElement = troopRoster[num];
				CharacterObject character = prisonerElement.Character;
				MBReadOnlyList<KeyValuePair<MapEventParty, float>> lootPrisonerChances = Campaign.Current.Models.BattleRewardModel.GetLootPrisonerChances(winnerParties, prisonerElement);
				if (!character.IsHero)
				{
					prisonRoster.RemoveTroop(character, prisonerElement.Number);
				}
				if (lootPrisonerChances.Count > 0)
				{
					for (int i = 0; i < prisonerElement.Number; i++)
					{
						MapEventParty mapEventParty = FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootPrisonerChances);
						TroopRoster troopRoster2 = mapEventParty?.RosterToReceiveLootMembers;
						if (troopRoster2 != null)
						{
							if (character.IsHero)
							{
								if (!mapEventParty.IsNpcParty || troopRoster2.OwnerParty.MapFaction.IsAtWarWith(character.HeroObject.MapFaction))
								{
									prisonRoster.RemoveTroop(character, prisonerElement.Number);
									if (!mapEventParty.IsNpcParty)
									{
										troopRoster2.AddToCounts(character, 1);
									}
									else
									{
										mapEventParty.RosterToReceiveLootPrisoners.AddToCounts(character, 1);
									}
								}
								else
								{
									EndCaptivityAction.ApplyByReleasedAfterBattle(character.HeroObject);
								}
							}
							else
							{
								troopRoster2.AddToCounts(character, 1);
							}
						}
						else if (character.IsHero)
						{
							EndCaptivityAction.ApplyByReleasedAfterBattle(character.HeroObject);
						}
					}
				}
				else if (character.IsHero)
				{
					EndCaptivityAction.ApplyByReleasedAfterBattle(character.HeroObject);
				}
			}
			prisonRoster.RemoveZeroCounts();
		}
	}

	private void LootDefeatedPartyItems(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			Dictionary<MapEventParty, ItemRoster> dictionary = new Dictionary<MapEventParty, ItemRoster>();
			PartyBase party = defeatedParty.Party;
			MBList<KeyValuePair<MapEventParty, float>> lootItemChancesForWinnerParties = Campaign.Current.Models.BattleRewardModel.GetLootItemChancesForWinnerParties(winnerParties, party);
			List<ItemRosterElement> list = party.ItemRoster.Where((ItemRosterElement x) => !x.EquipmentElement.Item.NotMerchandise && !x.EquipmentElement.IsQuestItem && !x.EquipmentElement.Item.IsBannerItem).ToList();
			if (lootItemChancesForWinnerParties.Count > 0)
			{
				for (int i = 0; i < list.Count; i++)
				{
					ItemRosterElement itemRosterElement = list[i];
					for (int j = 0; j < itemRosterElement.Amount; j++)
					{
						MapEventParty mapEventParty = FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootItemChancesForWinnerParties.ToMBList());
						if (mapEventParty != null)
						{
							if (!dictionary.TryGetValue(mapEventParty, out var value))
							{
								value = new ItemRoster();
								dictionary.Add(mapEventParty, value);
							}
							value.AddToCounts(itemRosterElement.EquipmentElement, 1);
							party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
						}
					}
				}
				foreach (KeyValuePair<MapEventParty, ItemRoster> item in dictionary)
				{
					if (item.Value.Count > 0)
					{
						ItemRoster value2 = item.Value;
						MapEventParty key = item.Key;
						key.RosterToReceiveLootItems.Add(value2);
						CampaignEventDispatcher.Instance.OnLootDistributedToParty(key.Party, party, value2);
					}
				}
			}
			else
			{
				if (party.IsSettlement || party == PartyBase.MainParty || winnerParties.All((MapEventParty x) => x.Party.MobileParty == null || x.Party.MobileParty.IsGarrison || x.Party.MobileParty.IsMilitia))
				{
					continue;
				}
				foreach (MapEventParty winnerParty in winnerParties)
				{
					Debug.Print($"Winner party name: {winnerParty.Party.Name}");
				}
				foreach (MapEventParty defeatedParty2 in defeatedParties)
				{
					Debug.Print($"Defeated party name: {defeatedParty2.Party.Name}");
				}
			}
		}
		foreach (MapEventParty winnerParty2 in winnerParties)
		{
			if (winnerParty2.RosterToReceiveLootItems.Count > 0 || winnerParty2.Party == PartyBase.MainParty)
			{
				CampaignEventDispatcher.Instance.OnCollectLootItems(winnerParty2.Party, winnerParty2.RosterToReceiveLootItems);
			}
		}
	}

	private void LootDefeatedPartyCasualties(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		float aITradePenalty = Campaign.Current.Models.BattleRewardModel.GetAITradePenalty();
		bool flag = IsPlayerMapEvent && PlayerSide == WinningSide;
		float f = float.MinValue;
		ItemRoster itemRoster = null;
		MapEventParty playerBattleParty = (flag ? winnerParties.Find((MapEventParty x) => x.Party == PartyBase.MainParty) : null);
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			if (defeatedParty.DiedInBattle.Count <= 0 && defeatedParty.WoundedInBattle.Count <= 0)
			{
				continue;
			}
			PartyBase party = defeatedParty.Party;
			MBReadOnlyList<KeyValuePair<MapEventParty, float>> lootCasualtyChances = Campaign.Current.Models.BattleRewardModel.GetLootCasualtyChances(winnerParties, party);
			if (flag)
			{
				if (playerBattleParty == null)
				{
					playerBattleParty = lootCasualtyChances.Find((KeyValuePair<MapEventParty, float> x) => x.Key.Party == PartyBase.MainParty).Key;
				}
				itemRoster = new ItemRoster();
				f = lootCasualtyChances.Find((KeyValuePair<MapEventParty, float> x) => x.Key == playerBattleParty).Value;
			}
			if (lootCasualtyChances.Count <= 0)
			{
				continue;
			}
			CharacterObject characterObject = null;
			for (int num = defeatedParty.DiedInBattle.Count - 1; num >= 0; num--)
			{
				characterObject = defeatedParty.DiedInBattle.GetCharacterAtIndex(num);
				for (int i = 0; i < defeatedParty.DiedInBattle.GetElementNumber(num); i++)
				{
					MapEventParty mapEventParty = FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootCasualtyChances);
					if (mapEventParty != null)
					{
						LootCasualtyCharacter(characterObject, mapEventParty, defeatedParty, aITradePenalty, flag ? MBRandom.RoundRandomized(f) : int.MinValue, itemRoster);
					}
				}
			}
			for (int num2 = defeatedParty.WoundedInBattle.Count - 1; num2 >= 0; num2--)
			{
				characterObject = defeatedParty.WoundedInBattle.GetCharacterAtIndex(num2);
				for (int j = 0; j < defeatedParty.WoundedInBattle.GetElementNumber(num2); j++)
				{
					MapEventParty mapEventParty2 = FindWinnerPartyToGetCurrentLootObjectBasedOnChances(lootCasualtyChances);
					if (mapEventParty2 != null)
					{
						LootCasualtyCharacter(characterObject, mapEventParty2, defeatedParty, aITradePenalty, flag ? MBRandom.RoundRandomized(f) : int.MinValue, itemRoster);
					}
				}
			}
			if (flag && itemRoster.Count > 0)
			{
				CampaignEventDispatcher.Instance.OnLootDistributedToParty(PartyBase.MainParty, party, itemRoster);
				playerBattleParty.RosterToReceiveLootItems.Add(itemRoster);
			}
		}
	}

	private void LootDefeatedPartyShips(MBReadOnlyList<MapEventParty> winnerParties, MBReadOnlyList<MapEventParty> defeatedParties)
	{
		if (!IsNavalMapEvent || RetreatingSide != BattleSideEnum.None)
		{
			return;
		}
		MBList<Ship> mBList = new MBList<Ship>();
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			foreach (Ship item in defeatedParty.Party.Ships.ToList())
			{
				item.OnShipDamaged(Campaign.Current.Models.BattleRewardModel.CalculateShipDamageAfterDefeat(item), null, out var _);
				if (item.HitPoints > 0f)
				{
					mBList.Add(item);
				}
			}
		}
		MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> mBReadOnlyList = Campaign.Current.Models.BattleRewardModel.DistributeDefeatedPartyShipsAmongWinners(this, mBList, winnerParties);
		MBReadOnlyList<MapEventParty> winnerPartiesThatCanPlunderGoldFromShips = Campaign.Current.Models.BattleRewardModel.GetWinnerPartiesThatCanPlunderGoldFromShips(winnerParties);
		bool flag = Winner.LeaderParty.LeaderHero != null && winnerPartiesThatCanPlunderGoldFromShips.AnyQ();
		int num = 0;
		foreach (KeyValuePair<Ship, MapEventParty> item2 in mBReadOnlyList)
		{
			if (item2.Value != null)
			{
				if (item2.Value.Party == PartyBase.MainParty)
				{
					PlayerEncounter.Current.ReceivedLootShips.Add(item2.Key);
				}
				else
				{
					ChangeShipOwnerAction.ApplyByLooting(item2.Value.Party, item2.Key);
				}
				continue;
			}
			if (flag)
			{
				num += (int)Campaign.Current.Models.ShipCostModel.GetShipTradeValue(item2.Key, Winner.LeaderParty, null);
			}
			DestroyShipAction.Apply(item2.Key);
		}
		if (num <= 0)
		{
			return;
		}
		int num2 = winnerPartiesThatCanPlunderGoldFromShips.SumQ((MapEventParty x) => x.ContributionToBattle);
		foreach (MapEventParty item3 in winnerPartiesThatCanPlunderGoldFromShips)
		{
			int num3 = TaleWorlds.Library.MathF.Floor((float)item3.ContributionToBattle / (float)num2 * (float)num);
			if (item3.Party.MobileParty.ActualClan == Clan.PlayerClan)
			{
				num3 = TaleWorlds.Library.MathF.Floor((float)num3 * Campaign.Current.Models.ShipCostModel.GetShipSellingPenalty());
			}
			item3.PlunderedGold += num3;
		}
	}

	private MapEventParty FindWinnerPartyToGetCurrentLootObjectBasedOnChances(MBReadOnlyList<KeyValuePair<MapEventParty, float>> winnerPartiesLootChances)
	{
		MapEventParty result = null;
		float num = MBRandom.RandomFloat;
		foreach (KeyValuePair<MapEventParty, float> winnerPartiesLootChance in winnerPartiesLootChances)
		{
			num -= winnerPartiesLootChance.Value;
			if (num <= 0f)
			{
				result = winnerPartiesLootChance.Key;
				break;
			}
		}
		return result;
	}

	private void LootCasualtyCharacter(CharacterObject casualtyCharacter, MapEventParty winnerParty, MapEventParty defeatedParty, float aiTradePenalty, int maxLootedItemsPerBodyForMainParty, ItemRoster mainPartyLootFromCasualties)
	{
		Hero leaderHero = winnerParty.Party.LeaderHero;
		if (leaderHero == null)
		{
			return;
		}
		float expectedLootedItemValueFromCasualty = Campaign.Current.Models.BattleRewardModel.GetExpectedLootedItemValueFromCasualty(leaderHero, casualtyCharacter);
		if (expectedLootedItemValueFromCasualty.ApproximatelyEqualsTo(0f))
		{
			return;
		}
		if (leaderHero != Hero.MainHero)
		{
			int num = (int)((float)TaleWorlds.Library.MathF.Round(expectedLootedItemValueFromCasualty) * aiTradePenalty);
			if (num > 0)
			{
				winnerParty.Party.MobileParty.PartyTradeGold += num;
				SkillLevelingManager.OnAIPartyLootCasualties(num, leaderHero, defeatedParty.Party);
			}
		}
		else
		{
			if (maxLootedItemsPerBodyForMainParty <= 0)
			{
				return;
			}
			List<EquipmentElement> list = new List<EquipmentElement>();
			for (int i = 0; i < maxLootedItemsPerBodyForMainParty; i++)
			{
				EquipmentElement lootedItem = Campaign.Current.Models.BattleRewardModel.GetLootedItemFromTroop(casualtyCharacter, expectedLootedItemValueFromCasualty);
				if (lootedItem.Item != null && !list.Exists((EquipmentElement x) => x.Item.Type == lootedItem.Item.Type))
				{
					list.Add(lootedItem);
					mainPartyLootFromCasualties.AddToCounts(lootedItem, 1);
				}
			}
		}
	}

	private void ControlAndUpdateDefeatedPartiesAfterBattle(MBReadOnlyList<MapEventParty> defeatedParties)
	{
		foreach (MapEventParty defeatedParty in defeatedParties)
		{
			PartyBase party = defeatedParty.Party;
			if (!party.IsMobile || !party.IsActive || party.MobileParty.IsMainParty)
			{
				continue;
			}
			party.MobileParty.RecentEventsMorale += Campaign.Current.Models.PartyMoraleModel.GetDefeatMoraleChange(party);
			if (party.NumberOfHealthyMembers > 0 && !party.MobileParty.IsGarrison)
			{
				if (party.MobileParty.CurrentSettlement != null)
				{
					party.MobileParty.Position = (party.MobileParty.IsTargetingPort ? party.MobileParty.CurrentSettlement.PortPosition : party.MobileParty.CurrentSettlement.GatePosition);
				}
				else if (party.MobileParty.AttachedTo == null && (!party.MobileParty.IsCurrentlyAtSea || party.Ships.Any()))
				{
					MobileParty.NavigationType navigationCapability = (party.MobileParty.Position.IsOnLand ? MobileParty.NavigationType.Default : MobileParty.NavigationType.Naval);
					party.MobileParty.Position = NavigationHelper.FindReachablePointAroundPosition(party.MobileParty.Position, navigationCapability, 4f, 3f);
				}
				party.MobileParty.Ai.ForceDefaultBehaviorUpdate();
			}
		}
	}

	private void CommitXPGains()
	{
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].CommitXpGains();
		}
	}

	private void CommitCalculatedMapEventResults()
	{
		CommitXPGains();
		ApplyRenownAndInfluenceChanges();
		ApplyRewardsAndChanges();
	}

	internal void ApplyRenownAndInfluenceChanges()
	{
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].ApplyRenownAndInfluenceChanges();
		}
	}

	private void ApplyRewardsAndChanges()
	{
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].ApplyFinalRewardsAndChanges();
		}
	}

	public void FinalizeEvent()
	{
		FinalizeEventAux();
	}

	private void FinalizeEventAux()
	{
		if (IsFinalized)
		{
			return;
		}
		State = MapEventState.WaitingRemoval;
		CampaignEventDispatcher.Instance.OnMapEventEnded(this);
		bool isWin = false;
		bool flag = false;
		if (MapEventSettlement != null)
		{
			if (BattleState != 0 && (IsSiegeAssault || IsSiegeOutside || IsSallyOut || IsBlockadeSallyOut || IsBlockade) && MapEventSettlement.SiegeEvent != null)
			{
				MapEventSettlement.SiegeEvent.OnBeforeSiegeEventEnd(BattleState, _mapEventType);
			}
			if (!_keepSiegeEvent && (IsSiegeAssault || IsSiegeOutside))
			{
				switch (BattleState)
				{
				case BattleState.AttackerVictory:
					CampaignEventDispatcher.Instance.SiegeCompleted(MapEventSettlement, AttackerSide.LeaderParty.MobileParty, isWin: true, _mapEventType);
					isWin = true;
					break;
				case BattleState.DefenderVictory:
					MapEventSettlement.SiegeEvent?.BesiegerCamp.RemoveAllSiegeParties();
					CampaignEventDispatcher.Instance.SiegeCompleted(MapEventSettlement, AttackerSide.LeaderParty.MobileParty, isWin: false, _mapEventType);
					break;
				}
				if (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory)
				{
					flag = true;
				}
			}
			else if (IsSallyOut || IsBlockadeSallyOut)
			{
				if (MapEventSettlement.Town != null && MapEventSettlement.Town.GarrisonParty != null && MapEventSettlement.Town.GarrisonParty.IsActive)
				{
					MapEventSettlement.Town.GarrisonParty.SetMoveModeHold();
				}
				switch (BattleState)
				{
				case BattleState.DefenderVictory:
					CampaignEventDispatcher.Instance.SiegeCompleted(MapEventSettlement, DefenderSide.LeaderParty.MobileParty, isWin: true, _mapEventType);
					isWin = true;
					break;
				case BattleState.AttackerVictory:
					MapEventSettlement.SiegeEvent?.BesiegerCamp.RemoveAllSiegeParties();
					CampaignEventDispatcher.Instance.SiegeCompleted(MapEventSettlement, DefenderSide.LeaderParty.MobileParty, isWin: false, _mapEventType);
					break;
				}
				if (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory)
				{
					flag = true;
				}
			}
			else if (IsBlockadeSallyOut || IsBlockade)
			{
				BattleState battleState = BattleState;
				if (battleState == BattleState.AttackerVictory)
				{
					MapEventSettlement.SiegeEvent?.BesiegerCamp.RemoveAllSiegeParties();
					CampaignEventDispatcher.Instance.SiegeCompleted(MapEventSettlement, DefenderSide.LeaderParty.MobileParty, isWin: false, _mapEventType);
				}
			}
		}
		Component?.BeforeFinalizeComponent();
		foreach (PartyBase involvedParty in InvolvedParties)
		{
			if (involvedParty.IsMobile)
			{
				involvedParty.MobileParty.EventPositionAdder = Vec2.Zero;
			}
			involvedParty.SetVisualAsDirty();
			if (!involvedParty.IsMobile || involvedParty.MobileParty.Army == null || involvedParty.MobileParty.Army.LeaderParty != involvedParty.MobileParty)
			{
				continue;
			}
			foreach (MobileParty attachedParty in involvedParty.MobileParty.Army.LeaderParty.AttachedParties)
			{
				attachedParty.Party.SetVisualAsDirty();
			}
		}
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].HandleMapEventEnd();
		}
		MapEventVisual?.OnMapEventEnd();
		if (_mapEventType != BattleTypes.Siege && _mapEventType != BattleTypes.SiegeOutside && _mapEventType != BattleTypes.SallyOut)
		{
			foreach (PartyBase involvedParty2 in InvolvedParties)
			{
				if (involvedParty2.IsMobile && involvedParty2 != PartyBase.MainParty && involvedParty2.MobileParty.BesiegedSettlement != null && (involvedParty2.MobileParty.Army == null || involvedParty2.MobileParty.Army.LeaderParty == involvedParty2.MobileParty))
				{
					if (involvedParty2.IsActive)
					{
						EncounterManager.StartSettlementEncounter(involvedParty2.MobileParty, involvedParty2.MobileParty.BesiegedSettlement);
					}
					else
					{
						involvedParty2.MobileParty.BesiegerCamp = null;
					}
				}
			}
		}
		Component?.FinalizeComponent();
		if (flag)
		{
			CampaignEventDispatcher.Instance.AfterSiegeCompleted(MapEventSettlement, AttackerSide.LeaderParty.MobileParty, isWin, _mapEventType);
		}
		sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].Clear();
		}
	}

	public bool HasTroopsOnBothSides()
	{
		bool num = PartiesOnSide(BattleSideEnum.Attacker).Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
		bool flag = PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
		return num && flag;
	}

	public PartyBase GetLeaderParty(BattleSideEnum side)
	{
		return _sides[(int)side].LeaderParty;
	}

	public float GetRenownValue(BattleSideEnum side)
	{
		return _sides[(int)side].RenownValue;
	}

	public void RecalculateRenownAndInfluenceValues(PartyBase party)
	{
		StrengthOfSide[(int)party.Side] += party.GetCustomStrength(party.Side, SimulationContext);
		MapEventSide[] sides = _sides;
		for (int i = 0; i < sides.Length; i++)
		{
			sides[i].CalculateRenownAndInfluenceValues(StrengthOfSide);
		}
	}

	public void RecalculateStrengthOfSides()
	{
		MapEventSide[] sides = _sides;
		foreach (MapEventSide mapEventSide in sides)
		{
			StrengthOfSide[(int)mapEventSide.MissionSide] = mapEventSide.RecalculateStrengthOfSide();
		}
	}

	public void DoSurrender(BattleSideEnum side)
	{
		GetMapEventSide(side).Surrender();
		BattleState = ((side != 0) ? BattleState.DefenderVictory : BattleState.AttackerVictory);
	}

	public void EndByRunAway()
	{
		BattleState = ((RetreatingSide == BattleSideEnum.Attacker) ? BattleState.DefenderVictory : BattleState.AttackerVictory);
	}

	public BattleSideEnum GetOtherSide(BattleSideEnum side)
	{
		if (side != BattleSideEnum.Attacker)
		{
			return BattleSideEnum.Attacker;
		}
		return BattleSideEnum.Defender;
	}

	private void ResetUnsuitablePartiesThatWereTargetingThisMapEvent()
	{
		float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(Position.ToVec2(), getEncounterJoiningRadius * 5f);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (!mobileParty.IsMainParty && mobileParty.ShortTermBehavior == AiBehavior.EngageParty && (mobileParty.ShortTermTargetParty == GetLeaderParty(BattleSideEnum.Attacker).MobileParty || mobileParty.ShortTermTargetParty == GetLeaderParty(BattleSideEnum.Defender).MobileParty) && !CanPartyJoinBattle(mobileParty.Party, BattleSideEnum.Attacker) && !CanPartyJoinBattle(mobileParty.Party, BattleSideEnum.Defender))
			{
				mobileParty.SetMoveModeHold();
			}
		}
	}

	private void CacheSimulationLeaderModifiers()
	{
		_sides[0].CacheLeaderSimulationModifier();
		_sides[1].CacheLeaderSimulationModifier();
	}

	private void CacheSimulationData()
	{
		_eventTerrainType = (TerrainType)Position.Face.FaceGroupIndex;
	}

	public bool CanPartyJoinBattle(PartyBase party, BattleSideEnum side)
	{
		if (GetMapEventSide(side).Parties.All((MapEventParty x) => x.Party.IsActive && !x.Party.MapFaction.IsAtWarWith(party.MapFaction)))
		{
			return GetMapEventSide(GetOtherSide(side)).Parties.All((MapEventParty x) => x.Party.IsActive && x.Party.MapFaction.IsAtWarWith(party.MapFaction));
		}
		return false;
	}

	public void GetStrengthsRelativeToParty(BattleSideEnum partySide, out float partySideStrength, out float opposingSideStrength)
	{
		partySideStrength = 0.1f;
		opposingSideStrength = 0.1f;
		foreach (PartyBase involvedParty in InvolvedParties)
		{
			if (involvedParty.Side == partySide)
			{
				partySideStrength += involvedParty.GetCustomStrength(involvedParty.Side, SimulationContext);
			}
			else
			{
				opposingSideStrength += involvedParty.GetCustomStrength(involvedParty.Side, SimulationContext);
			}
		}
	}

	public bool CheckIfBattleShouldContinueAfterBattleMission(CampaignBattleResult campaignBattleResult)
	{
		if (PlayerEncounter.PlayerSurrender || campaignBattleResult == null || campaignBattleResult.EnemyRetreated)
		{
			return false;
		}
		bool flag = IsSiegeAssault && BattleState == BattleState.AttackerVictory;
		MapEventSide mapEventSide = GetMapEventSide(PlayerSide);
		bool flag2 = !CheckIfOneSideHasLost();
		if (DefeatedSide != BattleSideEnum.None)
		{
			flag2 = ((campaignBattleResult.PlayerDefeat || campaignBattleResult.PlayerVictory) && !IsNavalMapEvent && GetMapEventSide(DefeatedSide).GetTotalHealthyTroopCountOfSide() + GetMapEventSide(DefeatedSide).GetTotalHealthyHeroCountOfSide() >= 1) || (campaignBattleResult.EnemyPulledBack && DefeatedSide != BattleSideEnum.None && GetMapEventSide(DefeatedSide).GetTotalHealthyTroopCountOfSide() + GetMapEventSide(DefeatedSide).GetTotalHealthyHeroCountOfSide() >= 1);
		}
		if (!IsHideoutBattle && !flag && flag2)
		{
			return !mapEventSide.IsSurrendered;
		}
		return false;
	}

	public void SetPositionAfterMapChange(CampaignVec2 newPosition)
	{
		if (MapEventSettlement != null)
		{
			float num = (MapEventSettlement.IsVillage ? Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringVillage : Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringTown);
			if (Position.Distance(newPosition) < num)
			{
				return;
			}
		}
		MobileParty mobileParty = GetLeaderParty(BattleSideEnum.Attacker).MobileParty;
		if (mobileParty == null)
		{
			_ = GetLeaderParty(BattleSideEnum.Defender).MobileParty.NavigationCapability;
		}
		else
		{
			_ = mobileParty.NavigationCapability;
		}
		Position = newPosition;
		if (IsSiegeAssault)
		{
			return;
		}
		foreach (PartyBase involvedParty in InvolvedParties)
		{
			if (!involvedParty.IsMobile)
			{
				continue;
			}
			if (involvedParty.MobileParty.Army != null)
			{
				if (involvedParty.MobileParty.Army.LeaderParty == involvedParty.MobileParty)
				{
					involvedParty.MobileParty.Army.SetPositionAfterMapChange(newPosition);
				}
			}
			else
			{
				involvedParty.MobileParty.SetPositionAfterMapChange(newPosition);
			}
		}
	}

	public void CheckPositionsForMapChangeAndUpdateIfNeeded()
	{
		MobileParty.NavigationType navigationType = ((!GetLeaderParty(BattleSideEnum.Attacker).IsMobile) ? GetLeaderParty(BattleSideEnum.Defender).MobileParty.NavigationCapability : GetLeaderParty(BattleSideEnum.Attacker).MobileParty.NavigationCapability);
		if (NavigationHelper.IsPositionValidForNavigationType(Position, navigationType))
		{
			return;
		}
		CampaignVec2 closestNavMeshFaceCenterPositionForPosition = NavigationHelper.GetClosestNavMeshFaceCenterPositionForPosition(Position, Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType));
		Position = NavigationHelper.FindReachablePointAroundPosition(closestNavMeshFaceCenterPositionForPosition, navigationType, 8f, 1f);
		if (!IsFieldBattle && !IsSallyOut && !IsSiegeOutside && !IsSiegeAmbush && !IsBlockade && !IsBlockadeSallyOut)
		{
			return;
		}
		foreach (PartyBase involvedParty in InvolvedParties)
		{
			if (involvedParty.IsMobile && involvedParty.MobileParty.CurrentSettlement == null && involvedParty.MobileParty.BesiegerCamp == null)
			{
				involvedParty.MobileParty.SetPositionAfterMapChange(Position);
			}
		}
	}
}
