using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MissionAgentSpawnLogic : MissionLogic, IMissionAgentSpawnLogic, IMissionBehavior
{
	internal struct FormationSpawnData
	{
		public int FootTroopCount;

		public int MountedTroopCount;

		public int NumTroops => FootTroopCount + MountedTroopCount;
	}

	private class MissionSide
	{
		private readonly MissionAgentSpawnLogic _spawnLogic;

		private readonly BattleSideEnum _side;

		private readonly IMissionTroopSupplier _troopSupplier;

		private BannerBearerLogic _bannerBearerLogic;

		private readonly MBArrayList<Formation> _spawnedFormations;

		private bool _spawnWithHorses;

		private float _reinforcementBatchPriority;

		private int _reinforcementQuotaRequirement;

		private int _reinforcementBatchSize;

		private int _reinforcementsSpawnedInLastBatch;

		private int _numSpawnedTroops;

		private readonly List<IAgentOriginBase> _reservedTroops = new List<IAgentOriginBase>();

		private List<(Team team, List<IAgentOriginBase> origins)> _troopOriginsToSpawnPerTeam;

		private readonly (int currentTroopIndex, int troopCount)[] _reinforcementSpawnedUnitCountPerFormation;

		private readonly Dictionary<IAgentOriginBase, int> _reinforcementTroopFormationAssignments;

		public bool TroopSpawnActive { get; private set; }

		public bool IsPlayerSide { get; }

		public bool ReinforcementSpawnActive { get; private set; }

		public bool SpawnWithHorses => _spawnWithHorses;

		public bool ReinforcementsNotifiedOnLastBatch { get; private set; }

		public int NumberOfActiveTroops => _numSpawnedTroops - _troopSupplier.NumRemovedTroops;

		public int ReinforcementQuotaRequirement => _reinforcementQuotaRequirement;

		public int ReinforcementsSpawnedInLastBatch => _reinforcementsSpawnedInLastBatch;

		public float ReinforcementBatchSize => _reinforcementBatchSize;

		public bool HasReservedTroops => _reservedTroops.Count > 0;

		public float ReinforcementBatchPriority => _reinforcementBatchPriority;

		public int ReservedTroopsCount => _reservedTroops.Count;

		public bool HasSpawnableReinforcements
		{
			get
			{
				if (ReinforcementSpawnActive && HasReservedTroops)
				{
					return ReinforcementBatchSize > 0f;
				}
				return false;
			}
		}

		public int GetNumberOfPlayerControllableTroops()
		{
			return _troopSupplier.GetNumberOfPlayerControllableTroops();
		}

		public MissionSide(MissionAgentSpawnLogic spawnLogic, BattleSideEnum side, IMissionTroopSupplier troopSupplier, bool isPlayerSide)
		{
			_spawnLogic = spawnLogic;
			_side = side;
			_spawnWithHorses = true;
			_spawnedFormations = new MBArrayList<Formation>();
			_troopSupplier = troopSupplier;
			_reinforcementQuotaRequirement = 0;
			_reinforcementBatchSize = 0;
			_reinforcementSpawnedUnitCountPerFormation = new(int, int)[8];
			_reinforcementTroopFormationAssignments = new Dictionary<IAgentOriginBase, int>();
			IsPlayerSide = isPlayerSide;
			ReinforcementsNotifiedOnLastBatch = false;
		}

		public int TryReinforcementSpawn()
		{
			int num = 0;
			if (ReinforcementSpawnActive && TroopSpawnActive && _reservedTroops.Count > 0)
			{
				int num2 = MaxNumberOfAgentsForMission - _spawnLogic.NumberOfAgents;
				int reservedTroopQuota = GetReservedTroopQuota(0);
				if (num2 >= reservedTroopQuota)
				{
					num = SpawnTroops(1, isReinforcement: true);
					if (num > 0)
					{
						_reinforcementQuotaRequirement -= reservedTroopQuota;
						if (_reservedTroops.Count >= _reinforcementBatchSize)
						{
							_reinforcementQuotaRequirement += GetReservedTroopQuota(_reinforcementBatchSize - 1);
						}
						_reinforcementBatchPriority /= 2f;
					}
				}
			}
			_reinforcementsSpawnedInLastBatch += num;
			return num;
		}

		public void GetTeamFormationsSpawnData(out List<(Team team, FormationSpawnData[] formationSpawnData)> teamFormationsSpawnData)
		{
			teamFormationsSpawnData = new List<(Team, FormationSpawnData[])>();
			Mission mission = _spawnLogic.Mission;
			foreach (Team item2 in mission.Teams.Where((Team t) => t.Side == _side && t == mission.PlayerTeam).Concat(mission.Teams.Where((Team t) => t.Side == _side && t != mission.PlayerTeam)))
			{
				if (item2.Side == _side)
				{
					FormationSpawnData[] array = new FormationSpawnData[11];
					for (int i = 0; i < array.Length; i++)
					{
						array[i].FootTroopCount = 0;
						array[i].MountedTroopCount = 0;
					}
					teamFormationsSpawnData.Add((item2, array));
				}
			}
			foreach (IAgentOriginBase reservedTroop in _reservedTroops)
			{
				FormationClass agentTroopClass = Mission.Current.GetAgentTroopClass(_side, reservedTroop.Troop);
				bool isPlayerSide = _side == Mission.Current.PlayerTeam.Side;
				Team troopTeam = Mission.GetAgentTeam(reservedTroop, isPlayerSide);
				FormationSpawnData[] item = teamFormationsSpawnData.FirstOrDefault(((Team team, FormationSpawnData[] formationSpawnData) tf) => tf.team == troopTeam).formationSpawnData;
				if (reservedTroop.Troop.HasMount())
				{
					item[(int)agentTroopClass].MountedTroopCount++;
				}
				else
				{
					item[(int)agentTroopClass].FootTroopCount++;
				}
			}
		}

		public void ReserveTroops(int number)
		{
			if (number > 0 && _troopSupplier.AnyTroopRemainsToBeSupplied)
			{
				_reservedTroops.AddRange(_troopSupplier.SupplyTroops(number));
			}
		}

		public BasicCharacterObject GetGeneralCharacter()
		{
			return _troopSupplier.GetGeneralCharacter();
		}

		public bool CheckReinforcementBatch()
		{
			SpawnPhase spawnPhase = ((_side == BattleSideEnum.Defender) ? _spawnLogic.DefenderActivePhase : _spawnLogic.AttackerActivePhase);
			_reinforcementsSpawnedInLastBatch = 0;
			ReinforcementsNotifiedOnLastBatch = false;
			int val = 0;
			MissionSpawnSettings reinforcementSpawnSettings = _spawnLogic.ReinforcementSpawnSettings;
			switch (reinforcementSpawnSettings.ReinforcementTroopsSpawnMethod)
			{
			case MissionSpawnSettings.ReinforcementSpawnMethod.Balanced:
				val = ComputeBalancedBatch(spawnPhase);
				break;
			case MissionSpawnSettings.ReinforcementSpawnMethod.Wave:
				val = ComputeWaveBatch(spawnPhase);
				break;
			case MissionSpawnSettings.ReinforcementSpawnMethod.Fixed:
				val = ComputeFixedBatch(spawnPhase);
				break;
			}
			val = Math.Min(val, spawnPhase.RemainingSpawnNumber);
			val -= _reservedTroops.Count;
			if (val > 0)
			{
				int count = _reservedTroops.Count;
				ReserveTroops(val);
				if (count < _reinforcementBatchSize)
				{
					int num = Math.Min(_reservedTroops.Count, _reinforcementBatchSize);
					for (int i = count; i < num; i++)
					{
						_reinforcementQuotaRequirement += GetReservedTroopQuota(i);
					}
				}
			}
			_reinforcementBatchPriority = _reservedTroops.Count;
			bool flag = false;
			flag = ((reinforcementSpawnSettings.ReinforcementTroopsSpawnMethod != MissionSpawnSettings.ReinforcementSpawnMethod.Wave) ? (_reservedTroops.Count > 0 && (_reservedTroops.Count >= _reinforcementBatchSize || spawnPhase.RemainingSpawnNumber <= _reinforcementBatchSize)) : (_reservedTroops.Count > 0));
			ReinforcementSpawnActive = flag;
			if (ReinforcementSpawnActive)
			{
				ResetReinforcementSpawnedUnitCountsPerFormation();
				foreach (Team team in _spawnLogic.Mission.Teams)
				{
					if (team.Side == _side)
					{
						_spawnLogic.DeploymentPlan.UpdateReinforcementPlan(team);
					}
				}
			}
			return ReinforcementSpawnActive;
		}

		public IEnumerable<IAgentOriginBase> GetAllTroops()
		{
			return _troopSupplier.GetAllTroops();
		}

		public int SpawnTroops(int number, bool isReinforcement)
		{
			if (number <= 0)
			{
				return 0;
			}
			List<IAgentOriginBase> list = new List<IAgentOriginBase>();
			int num = TaleWorlds.Library.MathF.Min(_reservedTroops.Count, number);
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					IAgentOriginBase item = _reservedTroops[i];
					list.Add(item);
				}
				_reservedTroops.RemoveRange(0, num);
			}
			int numberToAllocate = number - num;
			list.AddRange(_troopSupplier.SupplyTroops(numberToAllocate));
			Mission current = Mission.Current;
			if (_troopOriginsToSpawnPerTeam == null)
			{
				_troopOriginsToSpawnPerTeam = new List<(Team, List<IAgentOriginBase>)>();
				foreach (Team team in current.Teams)
				{
					bool flag = team.Side == current.PlayerTeam.Side;
					if ((IsPlayerSide && flag) || (!IsPlayerSide && !flag))
					{
						_troopOriginsToSpawnPerTeam.Add((team, new List<IAgentOriginBase>()));
					}
				}
			}
			else
			{
				foreach (var item2 in _troopOriginsToSpawnPerTeam)
				{
					item2.origins.Clear();
				}
			}
			int num2 = 0;
			foreach (IAgentOriginBase item3 in list)
			{
				Team agentTeam = Mission.GetAgentTeam(item3, IsPlayerSide);
				foreach (var item4 in _troopOriginsToSpawnPerTeam)
				{
					if (agentTeam == item4.team)
					{
						num2++;
						item4.origins.Add(item3);
					}
				}
			}
			int num3 = 0;
			List<IAgentOriginBase> list2 = new List<IAgentOriginBase>();
			foreach (var item5 in _troopOriginsToSpawnPerTeam)
			{
				if (item5.origins.IsEmpty())
				{
					continue;
				}
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				List<(IAgentOriginBase, int)> list3 = null;
				if (isReinforcement)
				{
					list3 = new List<(IAgentOriginBase, int)>();
					foreach (IAgentOriginBase item6 in item5.origins)
					{
						_reinforcementTroopFormationAssignments.TryGetValue(item6, out var value);
						list3.Add((item6, value));
					}
				}
				else
				{
					list3 = MissionGameModels.Current.BattleSpawnModel.GetInitialSpawnAssignments(_side, item5.origins);
				}
				for (int j = 0; j < 8; j++)
				{
					list2.Clear();
					IAgentOriginBase agentOriginBase = null;
					foreach (var (agentOriginBase2, num7) in list3)
					{
						if (j != num7)
						{
							continue;
						}
						if (agentOriginBase2.Troop == Game.Current.PlayerTroop)
						{
							agentOriginBase = agentOriginBase2;
							continue;
						}
						if (agentOriginBase2.Troop.HasMount())
						{
							num5++;
						}
						else
						{
							num6++;
						}
						list2.Add(agentOriginBase2);
					}
					if (agentOriginBase != null)
					{
						if (agentOriginBase.Troop.HasMount())
						{
							num5++;
						}
						else
						{
							num6++;
						}
						list2.Add(agentOriginBase);
					}
					int count = list2.Count;
					if (count <= 0)
					{
						continue;
					}
					bool isMounted = _spawnWithHorses && DefaultMissionDeploymentPlan.HasSignificantMountedTroops(num6, num5);
					int num8 = 0;
					int num9 = count;
					if (ReinforcementSpawnActive)
					{
						num8 = _reinforcementSpawnedUnitCountPerFormation[j].currentTroopIndex;
						num9 = _reinforcementSpawnedUnitCountPerFormation[j].troopCount;
					}
					Formation formation = item5.team.GetFormation((FormationClass)j);
					if (!formation.HasBeenPositioned)
					{
						formation.BeginSpawn(num9, isMounted);
						current.SetFormationPositioningFromDeploymentPlan(formation);
						_spawnedFormations.Add(formation);
					}
					foreach (IAgentOriginBase item7 in list2)
					{
						if (!item7.Troop.IsHero && _bannerBearerLogic != null && current.Mode != MissionMode.Deployment && _bannerBearerLogic.GetMissingBannerCount(formation) > 0)
						{
							_bannerBearerLogic.SpawnBannerBearer(item7, IsPlayerSide, formation, _spawnWithHorses, isReinforcement, num9, num8, isAlarmed: true, wieldInitialWeapons: true, forceDismounted: false, null, null, null, current.IsSallyOutBattle);
						}
						else
						{
							current.SpawnTroop(item7, IsPlayerSide, hasFormation: true, _spawnWithHorses, isReinforcement, num9, num8, isAlarmed: true, wieldInitialWeapons: true, forceDismounted: false, null, null, null, null, formation.FormationIndex, current.IsSallyOutBattle);
						}
						_numSpawnedTroops++;
						num8++;
						num4++;
					}
					if (ReinforcementSpawnActive)
					{
						_reinforcementSpawnedUnitCountPerFormation[j].currentTroopIndex = num8;
					}
				}
				if (num4 > 0)
				{
					item5.team.QuerySystem.Expire();
				}
				num3 += num4;
				foreach (Formation item8 in item5.team.FormationsIncludingEmpty)
				{
					if (item8.CountOfUnits > 0 && item8.IsSpawning)
					{
						item8.EndSpawn();
					}
				}
			}
			return num3;
		}

		public void SetSpawnWithHorses(bool spawnWithHorses)
		{
			_spawnWithHorses = spawnWithHorses;
		}

		private int ComputeBalancedBatch(SpawnPhase activePhase)
		{
			int result = 0;
			if (activePhase != null && activePhase.RemainingSpawnNumber > 0)
			{
				MissionSpawnSettings reinforcementSpawnSettings = _spawnLogic.ReinforcementSpawnSettings;
				int reinforcementBatchSize = _reinforcementBatchSize;
				_reinforcementBatchSize = (int)((float)_spawnLogic.BattleSize * reinforcementSpawnSettings.ReinforcementBatchPercentage);
				if (reinforcementBatchSize != _reinforcementBatchSize)
				{
					UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
				}
				int num = activePhase.TotalSpawnNumber - activePhase.InitialSpawnedNumber;
				result = TaleWorlds.Library.MathF.Max(1, _reservedTroops.Count + (int)((float)num * reinforcementSpawnSettings.DesiredReinforcementPercentage));
				result = TaleWorlds.Library.MathF.Min(result, activePhase.InitialSpawnedNumber - NumberOfActiveTroops);
			}
			return result;
		}

		private int ComputeFixedBatch(SpawnPhase activePhase)
		{
			int result = 0;
			if (activePhase != null && activePhase.RemainingSpawnNumber > 0)
			{
				MissionSpawnSettings reinforcementSpawnSettings = _spawnLogic.ReinforcementSpawnSettings;
				float num = ((_side == BattleSideEnum.Defender) ? reinforcementSpawnSettings.DefenderReinforcementBatchPercentage : reinforcementSpawnSettings.AttackerReinforcementBatchPercentage);
				int reinforcementBatchSize = _reinforcementBatchSize;
				_reinforcementBatchSize = (int)((float)_spawnLogic.TotalSpawnNumber * num);
				if (reinforcementBatchSize != _reinforcementBatchSize)
				{
					UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
				}
				result = TaleWorlds.Library.MathF.Max(1, _reinforcementBatchSize);
			}
			return result;
		}

		private int ComputeWaveBatch(SpawnPhase activePhase)
		{
			int result = 0;
			if (activePhase != null && activePhase.RemainingSpawnNumber > 0 && _reservedTroops.IsEmpty())
			{
				MissionSpawnSettings reinforcementSpawnSettings = _spawnLogic.ReinforcementSpawnSettings;
				int reinforcementBatchSize = _reinforcementBatchSize;
				int num = (_reinforcementBatchSize = (int)Math.Max(1f, (float)activePhase.InitialSpawnedNumber * reinforcementSpawnSettings.ReinforcementWavePercentage));
				if (reinforcementBatchSize != _reinforcementBatchSize)
				{
					UpdateReinforcementQuotaRequirement(reinforcementBatchSize);
				}
				if (activePhase.InitialSpawnedNumber - activePhase.NumberActiveTroops >= num)
				{
					result = num;
				}
			}
			return result;
		}

		public void SetBannerBearerLogic(BannerBearerLogic bannerBearerLogic)
		{
			_bannerBearerLogic = bannerBearerLogic;
		}

		private void UpdateReinforcementQuotaRequirement(int previousBatchSize)
		{
			if (_reinforcementBatchSize < previousBatchSize)
			{
				for (int num = TaleWorlds.Library.MathF.Min(_reservedTroops.Count - 1, previousBatchSize - 1); num >= _reinforcementBatchSize; num--)
				{
					_reinforcementQuotaRequirement -= GetReservedTroopQuota(num);
				}
			}
			else if (_reinforcementBatchSize > previousBatchSize)
			{
				int num2 = TaleWorlds.Library.MathF.Min(_reservedTroops.Count - 1, _reinforcementBatchSize - 1);
				for (int i = previousBatchSize; i <= num2; i++)
				{
					_reinforcementQuotaRequirement += GetReservedTroopQuota(i);
				}
			}
		}

		public void SetReinforcementsNotifiedOnLastBatch(bool value)
		{
			ReinforcementsNotifiedOnLastBatch = value;
		}

		private void ResetReinforcementSpawnedUnitCountsPerFormation()
		{
			for (int i = 0; i < 8; i++)
			{
				_reinforcementSpawnedUnitCountPerFormation[i].currentTroopIndex = 0;
				_reinforcementSpawnedUnitCountPerFormation[i].troopCount = 0;
			}
			_reinforcementTroopFormationAssignments.Clear();
			foreach (var reinforcementAssignment in MissionGameModels.Current.BattleSpawnModel.GetReinforcementAssignments(_side, _reservedTroops))
			{
				int item = reinforcementAssignment.formationIndex;
				_reinforcementTroopFormationAssignments.Add(reinforcementAssignment.origin, reinforcementAssignment.formationIndex);
				_reinforcementSpawnedUnitCountPerFormation[item].troopCount++;
			}
		}

		public void SetSpawnTroops(bool spawnTroops)
		{
			TroopSpawnActive = spawnTroops;
		}

		private int GetReservedTroopQuota(int index)
		{
			if (!_spawnWithHorses || !_reservedTroops[index].Troop.IsMounted)
			{
				return 1;
			}
			return 2;
		}

		public void OnInitialSpawnOver()
		{
			foreach (Formation spawnedFormation in _spawnedFormations)
			{
				spawnedFormation.EndSpawn();
			}
		}
	}

	private class SpawnPhase
	{
		public int TotalSpawnNumber;

		public int InitialSpawnedNumber;

		public int InitialSpawnNumber;

		public int RemainingSpawnNumber;

		public int NumberActiveTroops;

		public void OnInitialTroopsSpawned()
		{
			InitialSpawnedNumber = InitialSpawnNumber;
			InitialSpawnNumber = 0;
		}
	}

	public delegate void OnPhaseChangedDelegate();

	private static int _maxNumberOfAgentsForMissionCache;

	private readonly OnPhaseChangedDelegate[] _onPhaseChanged = new OnPhaseChangedDelegate[2];

	private readonly List<SpawnPhase>[] _phases;

	private readonly int[] _numberOfTroopsInTotal;

	private readonly int _battleSize;

	private bool _reinforcementSpawnEnabled = true;

	private bool _spawningReinforcements;

	private readonly BasicMissionTimer _globalReinforcementSpawnTimer;

	private ICustomReinforcementSpawnTimer _customReinforcementSpawnTimer;

	private float _globalReinforcementInterval;

	private MissionSpawnSettings _spawnSettings;

	private readonly MissionSide[] _missionSides;

	private BannerBearerLogic _bannerBearerLogic;

	private DefaultMissionDeploymentPlan _deploymentPlan;

	private List<BattleSideEnum> _sidesWhereSpawnOccured = new List<BattleSideEnum>();

	private readonly MissionSide _playerSide;

	public static int MaxNumberOfAgentsForMission
	{
		get
		{
			if (_maxNumberOfAgentsForMissionCache == 0)
			{
				_maxNumberOfAgentsForMissionCache = MBAPI.IMBAgent.GetMaximumNumberOfAgents();
			}
			return _maxNumberOfAgentsForMissionCache;
		}
	}

	private static int MaxNumberOfTroopsForMission => MaxNumberOfAgentsForMission / 2;

	public int NumberOfAgents => base.Mission.AllAgents.Count;

	public int NumberOfRemainingTroops => (DefenderActivePhase?.RemainingSpawnNumber ?? 0) + (AttackerActivePhase?.RemainingSpawnNumber ?? 0);

	public int NumberOfActiveDefenderTroops => DefenderActivePhase?.NumberActiveTroops ?? 0;

	public int NumberOfActiveAttackerTroops => AttackerActivePhase?.NumberActiveTroops ?? 0;

	public int NumberOfRemainingDefenderTroops => DefenderActivePhase?.RemainingSpawnNumber ?? 0;

	public int NumberOfRemainingAttackerTroops => AttackerActivePhase?.RemainingSpawnNumber ?? 0;

	public int BattleSize => _battleSize;

	public bool IsInitialSpawnOver => DefenderActivePhase.InitialSpawnNumber + AttackerActivePhase.InitialSpawnNumber == 0;

	public bool IsDeploymentOver
	{
		get
		{
			if (base.Mission.Mode != MissionMode.Deployment)
			{
				return IsInitialSpawnOver;
			}
			return false;
		}
	}

	public ref readonly MissionSpawnSettings ReinforcementSpawnSettings => ref _spawnSettings;

	internal DefaultMissionDeploymentPlan DeploymentPlan => _deploymentPlan;

	private int TotalSpawnNumber => (DefenderActivePhase?.TotalSpawnNumber ?? 0) + (AttackerActivePhase?.TotalSpawnNumber ?? 0);

	private SpawnPhase DefenderActivePhase => _phases[0].FirstOrDefault();

	private SpawnPhase AttackerActivePhase => _phases[1].FirstOrDefault();

	public event Action<BattleSideEnum, int> OnReinforcementsSpawned;

	public event Action<BattleSideEnum, int> OnInitialTroopsSpawned;

	public override void AfterStart()
	{
		_bannerBearerLogic = base.Mission.GetMissionBehavior<BannerBearerLogic>();
		if (_bannerBearerLogic != null)
		{
			for (int i = 0; i < 2; i++)
			{
				_missionSides[i].SetBannerBearerLogic(_bannerBearerLogic);
			}
		}
		MissionGameModels.Current.BattleSpawnModel.OnMissionStart();
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		return _playerSide?.GetNumberOfPlayerControllableTroops() ?? 0;
	}

	public void InitWithSinglePhase(int defenderTotalSpawn, int attackerTotalSpawn, int defenderInitialSpawn, int attackerInitialSpawn, bool spawnDefenders, bool spawnAttackers, in MissionSpawnSettings spawnSettings)
	{
		AddPhase(BattleSideEnum.Defender, defenderTotalSpawn, defenderInitialSpawn);
		AddPhase(BattleSideEnum.Attacker, attackerTotalSpawn, attackerInitialSpawn);
		Init(spawnDefenders, spawnAttackers, in spawnSettings);
	}

	public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
	{
		return _missionSides[(int)side].GetAllTroops();
	}

	public override void OnMissionTick(float dt)
	{
		if (!GameNetwork.IsClient && !CheckDeployment())
		{
			return;
		}
		PhaseTick();
		if (_reinforcementSpawnEnabled)
		{
			if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer)
			{
				CheckGlobalReinforcementBatch();
			}
			else if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer)
			{
				CheckCustomReinforcementBatch();
			}
		}
		if (_spawningReinforcements)
		{
			CheckReinforcementSpawn();
		}
	}

	public MissionAgentSpawnLogic(IMissionTroopSupplier[] suppliers, BattleSideEnum playerSide, Mission.BattleSizeType battleSizeType)
	{
		switch (battleSizeType)
		{
		case Mission.BattleSizeType.Battle:
			_battleSize = BannerlordConfig.GetRealBattleSize();
			break;
		case Mission.BattleSizeType.Siege:
			_battleSize = BannerlordConfig.GetRealBattleSizeForSiege();
			break;
		case Mission.BattleSizeType.SallyOut:
			_battleSize = BannerlordConfig.GetRealBattleSizeForSallyOut();
			break;
		}
		_battleSize = TaleWorlds.Library.MathF.Min(_battleSize, MaxNumberOfTroopsForMission);
		_globalReinforcementSpawnTimer = new BasicMissionTimer();
		_spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
		_globalReinforcementInterval = _spawnSettings.GlobalReinforcementInterval;
		_missionSides = new MissionSide[2];
		for (int i = 0; i < 2; i++)
		{
			IMissionTroopSupplier troopSupplier = suppliers[i];
			bool flag = i == (int)playerSide;
			MissionSide missionSide = new MissionSide(this, (BattleSideEnum)i, troopSupplier, flag);
			if (flag)
			{
				_playerSide = missionSide;
			}
			_missionSides[i] = missionSide;
		}
		_numberOfTroopsInTotal = new int[2];
		_phases = new List<SpawnPhase>[2];
		for (int j = 0; j < 2; j++)
		{
			_phases[j] = new List<SpawnPhase>();
		}
		_reinforcementSpawnEnabled = false;
	}

	public void SetCustomReinforcementSpawnTimer(ICustomReinforcementSpawnTimer timer)
	{
		_customReinforcementSpawnTimer = timer;
	}

	public void SetSpawnTroops(BattleSideEnum side, bool spawnTroops, bool enforceSpawning = false)
	{
		_missionSides[(int)side].SetSpawnTroops(spawnTroops);
		if (spawnTroops && enforceSpawning)
		{
			CheckDeployment();
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MissionGameModels.Current.BattleInitializationModel.InitializeModel();
	}

	protected override void OnEndMission()
	{
		MissionGameModels.Current.BattleSpawnModel.OnMissionEnd();
		MissionGameModels.Current.BattleInitializationModel.FinalizeModel();
	}

	public void SetSpawnHorses(BattleSideEnum side, bool spawnHorses)
	{
		_missionSides[(int)side].SetSpawnWithHorses(spawnHorses);
	}

	public void StartSpawner(BattleSideEnum side)
	{
		_missionSides[(int)side].SetSpawnTroops(spawnTroops: true);
	}

	public void StopSpawner(BattleSideEnum side)
	{
		_missionSides[(int)side].SetSpawnTroops(spawnTroops: false);
	}

	public bool IsSideSpawnEnabled(BattleSideEnum side)
	{
		return _missionSides[(int)side].TroopSpawnActive;
	}

	public void OnSideDeploymentOver(BattleSideEnum battleSide)
	{
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side == battleSide)
			{
				base.Mission.OnTeamDeployed(team);
			}
		}
		base.Mission.OnBattleSideDeployed(battleSide);
		foreach (Team team2 in base.Mission.Teams)
		{
			if (team2.Side != battleSide)
			{
				continue;
			}
			foreach (Formation item in team2.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.QuerySystem.EvaluateAllPreliminaryQueryData();
				}
			}
			team2.MasterOrderController.OnOrderIssued += OrderController_OnOrderIssued;
			for (int i = 8; i < 10; i++)
			{
				Formation formation = team2.FormationsIncludingSpecialAndEmpty[i];
				if (formation.CountOfUnits > 0)
				{
					team2.MasterOrderController.SelectFormation(formation);
					team2.MasterOrderController.SetOrderWithAgent(OrderType.FollowMe, team2.GeneralAgent);
					team2.MasterOrderController.ClearSelectedFormations();
					formation.SetControlledByAI(isControlledByAI: true);
				}
			}
			team2.MasterOrderController.OnOrderIssued -= OrderController_OnOrderIssued;
		}
	}

	public float GetReinforcementInterval()
	{
		return _globalReinforcementInterval;
	}

	public void SetReinforcementsSpawnEnabled(bool value, bool resetTimers = true)
	{
		if (_reinforcementSpawnEnabled == value)
		{
			return;
		}
		_reinforcementSpawnEnabled = value;
		if (!resetTimers)
		{
			return;
		}
		if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.GlobalTimer)
		{
			_globalReinforcementSpawnTimer.Reset();
		}
		else if (_spawnSettings.ReinforcementTroopsTimingMethod == MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer)
		{
			for (int i = 0; i < 2; i++)
			{
				_customReinforcementSpawnTimer.ResetTimer((BattleSideEnum)i);
			}
		}
	}

	public int GetTotalNumberOfTroopsForSide(BattleSideEnum side)
	{
		return _numberOfTroopsInTotal[(int)side];
	}

	public BasicCharacterObject GetGeneralCharacterOfSide(BattleSideEnum side)
	{
		if (side >= BattleSideEnum.Defender && side < BattleSideEnum.NumSides)
		{
			_missionSides[(int)side].GetGeneralCharacter();
		}
		return null;
	}

	public bool GetSpawnHorses(BattleSideEnum side)
	{
		return _missionSides[(int)side].SpawnWithHorses;
	}

	private bool CheckMinimumBatchQuotaRequirement()
	{
		int num = MaxNumberOfAgentsForMission - NumberOfAgents;
		int num2 = 0;
		for (int i = 0; i < 2; i++)
		{
			num2 += _missionSides[i].ReinforcementQuotaRequirement;
		}
		return num >= num2;
	}

	private void CheckGlobalReinforcementBatch()
	{
		if (_globalReinforcementSpawnTimer.ElapsedTime >= _globalReinforcementInterval)
		{
			bool flag = false;
			for (int i = 0; i < 2; i++)
			{
				BattleSideEnum battleSide = (BattleSideEnum)i;
				NotifyReinforcementTroopsSpawned(battleSide);
				bool flag2 = _missionSides[i].CheckReinforcementBatch();
				flag = flag || flag2;
			}
			_spawningReinforcements = flag && CheckMinimumBatchQuotaRequirement();
			_globalReinforcementSpawnTimer.Reset();
		}
	}

	private void CheckCustomReinforcementBatch()
	{
		bool flag = false;
		for (int i = 0; i < 2; i++)
		{
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			if (_customReinforcementSpawnTimer.Check(battleSideEnum))
			{
				flag = true;
				NotifyReinforcementTroopsSpawned(battleSideEnum);
				_missionSides[i].CheckReinforcementBatch();
			}
		}
		if (flag)
		{
			bool flag2 = false;
			for (int j = 0; j < 2; j++)
			{
				flag2 = flag2 || _missionSides[j].ReinforcementSpawnActive;
			}
			_spawningReinforcements = flag2 && CheckMinimumBatchQuotaRequirement();
		}
	}

	public bool IsSideDepleted(BattleSideEnum side)
	{
		if (_phases[(int)side].Count == 1 && _missionSides[(int)side].NumberOfActiveTroops == 0)
		{
			return GetActivePhaseForSide(side).RemainingSpawnNumber == 0;
		}
		return false;
	}

	public void AddPhaseChangeAction(BattleSideEnum side, OnPhaseChangedDelegate onPhaseChanged)
	{
		ref OnPhaseChangedDelegate reference = ref _onPhaseChanged[(int)side];
		reference = (OnPhaseChangedDelegate)Delegate.Combine(reference, onPhaseChanged);
	}

	private void Init(bool spawnDefenders, bool spawnAttackers, in MissionSpawnSettings reinforcementSpawnSettings)
	{
		base.Mission.GetDeploymentPlan<DefaultMissionDeploymentPlan>(out _deploymentPlan);
		List<SpawnPhase>[] phases = _phases;
		for (int i = 0; i < phases.Length; i++)
		{
			if (phases[i].Count <= 0)
			{
				return;
			}
		}
		foreach (Team team in base.Mission.Teams)
		{
			BattleSideEnum side = team.Side;
			bool spawnWithHorses = _missionSides[(int)side].SpawnWithHorses;
			_deploymentPlan.SetSpawnWithHorses(team, spawnWithHorses);
		}
		_spawnSettings = reinforcementSpawnSettings;
		int num = 0;
		int num2 = 1;
		_globalReinforcementInterval = _spawnSettings.GlobalReinforcementInterval;
		int[] array = new int[2]
		{
			_phases[num].Sum((SpawnPhase p) => p.TotalSpawnNumber),
			_phases[num2].Sum((SpawnPhase p) => p.TotalSpawnNumber)
		};
		int num3 = array.Sum();
		if (_spawnSettings.InitialTroopsSpawnMethod == MissionSpawnSettings.InitialSpawnMethod.BattleSizeAllocating)
		{
			float[] array2 = new float[2]
			{
				(float)array[num] / (float)num3,
				(float)array[num2] / (float)num3
			};
			array2[num] = TaleWorlds.Library.MathF.Min(_spawnSettings.MaximumBattleSideRatio, array2[num] * _spawnSettings.DefenderAdvantageFactor);
			array2[num2] = 1f - array2[num];
			bool num4 = !(array2[num] < array2[num2]);
			int oppositeSide = (int)(num4 ? BattleSideEnum.Attacker : BattleSideEnum.Defender).GetOppositeSide();
			int num5 = (num4 ? 1 : 0);
			if (array2[oppositeSide] > _spawnSettings.MaximumBattleSideRatio)
			{
				array2[oppositeSide] = _spawnSettings.MaximumBattleSideRatio;
				array2[num5] = 1f - _spawnSettings.MaximumBattleSideRatio;
			}
			int[] array3 = new int[2];
			int val = TaleWorlds.Library.MathF.Ceiling(array2[num5] * (float)_battleSize);
			array3[num5] = Math.Min(val, array[num5]);
			array3[oppositeSide] = _battleSize - array3[num5];
			for (int j = 0; j < 2; j++)
			{
				foreach (SpawnPhase item in _phases[j])
				{
					if (item.InitialSpawnNumber > array3[j])
					{
						int num6 = array3[j];
						int num7 = item.InitialSpawnNumber - num6;
						item.InitialSpawnNumber = num6;
						item.RemainingSpawnNumber += num7;
					}
				}
			}
		}
		else if (_spawnSettings.InitialTroopsSpawnMethod == MissionSpawnSettings.InitialSpawnMethod.FreeAllocation)
		{
			_phases[num].Max((SpawnPhase p) => p.InitialSpawnNumber);
			_phases[num2].Max((SpawnPhase p) => p.InitialSpawnNumber);
		}
		if (_spawnSettings.ReinforcementTroopsSpawnMethod == MissionSpawnSettings.ReinforcementSpawnMethod.Wave)
		{
			for (int k = 0; k < 2; k++)
			{
				foreach (SpawnPhase item2 in _phases[k])
				{
					int num8 = (int)Math.Max(1f, (float)item2.InitialSpawnNumber * _spawnSettings.ReinforcementWavePercentage);
					if (_spawnSettings.MaximumReinforcementWaveCount > 0)
					{
						int num9 = Math.Min(item2.RemainingSpawnNumber, num8 * _spawnSettings.MaximumReinforcementWaveCount);
						int num10 = Math.Max(0, item2.RemainingSpawnNumber - num9);
						_numberOfTroopsInTotal[k] -= num10;
						array[k] -= num10;
						item2.RemainingSpawnNumber = num9;
						item2.TotalSpawnNumber = item2.RemainingSpawnNumber + item2.InitialSpawnNumber;
					}
				}
			}
		}
		base.Mission.SetBattleAgentCount(TaleWorlds.Library.MathF.Min(DefenderActivePhase.InitialSpawnNumber, AttackerActivePhase.InitialSpawnNumber));
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Defender, array[num]);
		base.Mission.SetInitialAgentCountForSide(BattleSideEnum.Attacker, array[num2]);
		_missionSides[num].SetSpawnTroops(spawnDefenders);
		_missionSides[num2].SetSpawnTroops(spawnAttackers);
	}

	private void AddPhase(BattleSideEnum side, int totalSpawn, int initialSpawn)
	{
		SpawnPhase item = new SpawnPhase
		{
			TotalSpawnNumber = totalSpawn,
			InitialSpawnNumber = initialSpawn,
			RemainingSpawnNumber = totalSpawn - initialSpawn
		};
		_phases[(int)side].Add(item);
		_numberOfTroopsInTotal[(int)side] += totalSpawn;
	}

	private void PhaseTick()
	{
		for (int i = 0; i < 2; i++)
		{
			SpawnPhase activePhaseForSide = GetActivePhaseForSide((BattleSideEnum)i);
			activePhaseForSide.NumberActiveTroops = _missionSides[i].NumberOfActiveTroops;
			if (activePhaseForSide.NumberActiveTroops != 0 || activePhaseForSide.RemainingSpawnNumber != 0 || _phases[i].Count <= 1)
			{
				continue;
			}
			_phases[i].Remove(activePhaseForSide);
			BattleSideEnum battleSideEnum = (BattleSideEnum)i;
			if (GetActivePhaseForSide(battleSideEnum) == null)
			{
				continue;
			}
			if (_onPhaseChanged[i] != null)
			{
				_onPhaseChanged[i]();
			}
			foreach (Team team in base.Mission.Teams)
			{
				if (team.Side == battleSideEnum)
				{
					if (_deploymentPlan.IsPlanMade(team))
					{
						_deploymentPlan.ClearAddedTroops(team);
						_deploymentPlan.ClearDeploymentPlan(team);
					}
					if (_deploymentPlan.IsReinforcementPlanMade(team))
					{
						_deploymentPlan.ClearAddedTroops(team, isReinforcement: true);
						_deploymentPlan.ClearReinforcementPlan(team);
					}
				}
			}
			Debug.Print("New spawn phase!", 0, Debug.DebugColor.Green, 64uL);
		}
	}

	private bool CheckDeployment()
	{
		bool isDeploymentOver = IsDeploymentOver;
		if (!isDeploymentOver)
		{
			int battleSizeForActivePhase = GetBattleSizeForActivePhase();
			for (int i = 0; i < 2; i++)
			{
				BattleSideEnum battleSideEnum = (BattleSideEnum)i;
				SpawnPhase activePhaseForSide = GetActivePhaseForSide(battleSideEnum);
				if (activePhaseForSide.InitialSpawnNumber <= 0)
				{
					continue;
				}
				if (activePhaseForSide.InitialSpawnNumber > _missionSides[i].ReservedTroopsCount)
				{
					int number = activePhaseForSide.InitialSpawnNumber - _missionSides[i].ReservedTroopsCount;
					_missionSides[i].ReserveTroops(number);
				}
				if (_missionSides[i].ReservedTroopsCount < activePhaseForSide.InitialSpawnNumber)
				{
					continue;
				}
				bool flag = true;
				int num = 0;
				foreach (Team team in base.Mission.Teams)
				{
					if (team.Side == battleSideEnum)
					{
						flag = flag && _deploymentPlan.IsPlanMade(team) && _deploymentPlan.IsReinforcementPlanMade(team);
						num++;
					}
				}
				if (num <= 0 || flag)
				{
					continue;
				}
				_missionSides[i].GetTeamFormationsSpawnData(out List<(Team, FormationSpawnData[])> teamFormationsSpawnData);
				if (base.Mission.HasSpawnPath)
				{
					Path initialSpawnPath = base.Mission.GetInitialSpawnPath();
					float battleSizeOffset = Mission.GetBattleSizeOffset(battleSizeForActivePhase, initialSpawnPath);
					SpawnPathData initialSpawnPathData = base.Mission.GetInitialSpawnPathData(battleSideEnum);
					float offsetOverflow = initialSpawnPathData.GetOffsetOverflow(battleSizeOffset);
					float offsetOverflow2 = initialSpawnPathData.GetOffsetOverflow(0f - battleSizeOffset);
					float pathOffsetRatio = battleSizeOffset - offsetOverflow2;
					float pathOffsetRatio2 = 0f - (battleSizeOffset + offsetOverflow);
					float num2 = initialSpawnPathData.ClampPathOffset(pathOffsetRatio);
					float targetOffset = initialSpawnPathData.ClampPathOffset(pathOffsetRatio2);
					foreach (var item2 in teamFormationsSpawnData)
					{
						FormationSpawnData[] item = item2.Item2;
						if (item2.Item1 == base.Mission.PlayerTeam || item2.Item1 == base.Mission.PlayerEnemyTeam)
						{
							MakeTeamPlans(item2.Item1, item, num2, targetOffset);
							continue;
						}
						float num3 = item.Max((FormationSpawnData data) => Formation.GetDefaultRankDepth(TaleWorlds.Library.MathF.Ceiling(TaleWorlds.Library.MathF.Sqrt(data.NumTroops)), 3, data.MountedTroopCount > 0));
						float distance = 3f * num3 + 20f;
						num2 += Mission.GetPathOffsetFromDistance(distance, initialSpawnPath);
						MakeTeamPlans(item2.Item1, item, num2, targetOffset);
					}
					continue;
				}
				foreach (var item3 in teamFormationsSpawnData)
				{
					MakeTeamPlans(item3.Item1, item3.Item2);
				}
			}
			for (int j = 0; j < 2; j++)
			{
				BattleSideEnum battleSideEnum2 = (BattleSideEnum)j;
				int initialSpawnNumber = GetActivePhaseForSide(battleSideEnum2).InitialSpawnNumber;
				if (!_missionSides[j].TroopSpawnActive)
				{
					continue;
				}
				int reservedTroopsCount = _missionSides[j].ReservedTroopsCount;
				if (reservedTroopsCount <= 0 || reservedTroopsCount < initialSpawnNumber)
				{
					continue;
				}
				bool flag2 = true;
				int num4 = 0;
				foreach (Team team2 in base.Mission.Teams)
				{
					if (team2.Side == battleSideEnum2)
					{
						flag2 = flag2 && _deploymentPlan.IsPlanMade(team2);
						num4++;
					}
				}
				if (num4 > 0 && flag2)
				{
					_missionSides[j].SpawnTroops(initialSpawnNumber, isReinforcement: false);
					GetActivePhaseForSide(battleSideEnum2).OnInitialTroopsSpawned();
					_missionSides[j].OnInitialSpawnOver();
					if (!_sidesWhereSpawnOccured.Contains(battleSideEnum2))
					{
						_sidesWhereSpawnOccured.Add(battleSideEnum2);
					}
					this.OnInitialTroopsSpawned?.Invoke(battleSideEnum2, initialSpawnNumber);
				}
			}
			isDeploymentOver = IsDeploymentOver;
			if (isDeploymentOver)
			{
				foreach (BattleSideEnum item4 in _sidesWhereSpawnOccured)
				{
					OnSideDeploymentOver(item4);
				}
			}
		}
		return isDeploymentOver;
	}

	private void MakeTeamPlans(Team team, FormationSpawnData[] formationsSpawnData, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		if (!_deploymentPlan.IsPlanMade(team))
		{
			for (int i = 0; i < formationsSpawnData.Length; i++)
			{
				if (formationsSpawnData[i].NumTroops > 0)
				{
					_deploymentPlan.AddTroops(team, (FormationClass)i, formationsSpawnData[i].FootTroopCount, formationsSpawnData[i].MountedTroopCount);
				}
			}
			_deploymentPlan.MakeDeploymentPlan(team, spawnPathOffset, targetOffset);
		}
		if (_deploymentPlan.IsReinforcementPlanMade(team))
		{
			return;
		}
		int num = Math.Max(_battleSize / (2 * formationsSpawnData.Length), 1);
		for (int j = 0; j < formationsSpawnData.Length; j++)
		{
			if (((FormationClass)j).IsMounted())
			{
				_deploymentPlan.AddTroops(team, (FormationClass)j, 0, num, isReinforcement: true);
			}
			else
			{
				_deploymentPlan.AddTroops(team, (FormationClass)j, num, 0, isReinforcement: true);
			}
		}
		_deploymentPlan.MakeReinforcementDeploymentPlan(team);
	}

	private void CheckReinforcementSpawn()
	{
		int num = 0;
		int num2 = 1;
		MissionSide missionSide = _missionSides[num];
		MissionSide missionSide2 = _missionSides[num2];
		bool flag = missionSide.HasSpawnableReinforcements && ((float)missionSide.ReinforcementsSpawnedInLastBatch < missionSide.ReinforcementBatchSize || missionSide.ReinforcementBatchPriority >= missionSide2.ReinforcementBatchPriority);
		bool flag2 = missionSide2.HasSpawnableReinforcements && ((float)missionSide2.ReinforcementsSpawnedInLastBatch < missionSide2.ReinforcementBatchSize || missionSide2.ReinforcementBatchPriority >= missionSide.ReinforcementBatchPriority);
		int num3 = 0;
		int num4 = 0;
		if (flag && flag2)
		{
			if (missionSide.ReinforcementBatchPriority >= missionSide2.ReinforcementBatchPriority)
			{
				num3 = missionSide.TryReinforcementSpawn();
				DefenderActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
				num3 = missionSide2.TryReinforcementSpawn();
				AttackerActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
			}
			else
			{
				num3 = missionSide2.TryReinforcementSpawn();
				AttackerActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
				num3 = missionSide.TryReinforcementSpawn();
				DefenderActivePhase.RemainingSpawnNumber -= num3;
				num4 += num3;
			}
		}
		else if (flag)
		{
			num3 = missionSide.TryReinforcementSpawn();
			DefenderActivePhase.RemainingSpawnNumber -= num3;
			num4 += num3;
		}
		else if (flag2)
		{
			num3 = missionSide2.TryReinforcementSpawn();
			AttackerActivePhase.RemainingSpawnNumber -= num3;
			num4 += num3;
		}
		if (num4 > 0)
		{
			for (int i = 0; i < 2; i++)
			{
				NotifyReinforcementTroopsSpawned((BattleSideEnum)i, checkEmptyReserves: true);
			}
		}
	}

	private void NotifyReinforcementTroopsSpawned(BattleSideEnum battleSide, bool checkEmptyReserves = false)
	{
		MissionSide missionSide = _missionSides[(int)battleSide];
		int reinforcementsSpawnedInLastBatch = missionSide.ReinforcementsSpawnedInLastBatch;
		if (!missionSide.ReinforcementsNotifiedOnLastBatch && reinforcementsSpawnedInLastBatch > 0 && (!checkEmptyReserves || (checkEmptyReserves && !missionSide.HasReservedTroops)))
		{
			this.OnReinforcementsSpawned?.Invoke(battleSide, reinforcementsSpawnedInLastBatch);
			missionSide.SetReinforcementsNotifiedOnLastBatch(value: true);
		}
	}

	private void OrderController_OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		DeploymentHandler.OrderController_OnOrderIssued_Aux(orderType, appliedFormations, orderController, delegateParams);
	}

	private int GetBattleSizeForActivePhase()
	{
		return TaleWorlds.Library.MathF.Max(DefenderActivePhase.TotalSpawnNumber, AttackerActivePhase.TotalSpawnNumber);
	}

	private SpawnPhase GetActivePhaseForSide(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Defender:
			return DefenderActivePhase;
		case BattleSideEnum.Attacker:
			return AttackerActivePhase;
		default:
			Debug.FailedAssert("Wrong Side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\MissionAgentSpawnLogic.cs", "GetActivePhaseForSide", 1694);
			return null;
		}
	}
}
