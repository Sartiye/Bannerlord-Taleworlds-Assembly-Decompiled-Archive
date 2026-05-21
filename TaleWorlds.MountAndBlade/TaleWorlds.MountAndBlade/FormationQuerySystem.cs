using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FormationQuerySystem
{
	public readonly Formation Formation;

	private readonly QueryData<float> _formationPower;

	private readonly QueryData<float> _formationMeleeFightingPower;

	private readonly QueryData<Vec2> _estimatedDirection;

	private readonly QueryData<float> _estimatedInterval;

	private readonly QueryData<Vec2> _averageAllyPosition;

	private readonly QueryData<float> _idealAverageDisplacement;

	private readonly QueryData<MBList<Agent>> _localAllyUnits;

	private readonly QueryData<MBList<Agent>> _localEnemyUnits;

	private readonly QueryData<FormationClass> _mainClass;

	private readonly QueryData<float> _infantryUnitRatio;

	private readonly QueryData<float> _hasShieldUnitRatio;

	private readonly QueryData<float> _hasThrowingUnitRatio;

	private readonly QueryData<float> _rangedUnitRatio;

	private readonly QueryData<int> _insideCastleUnitCountIncludingUnpositioned;

	private readonly QueryData<int> _insideCastleUnitCountPositioned;

	private readonly QueryData<float> _cavalryUnitRatio;

	private readonly QueryData<float> _rangedCavalryUnitRatio;

	private readonly QueryData<bool> _isMeleeFormation;

	private readonly QueryData<bool> _isInfantryFormation;

	private readonly QueryData<bool> _hasShield;

	private readonly QueryData<bool> _hasThrowing;

	private readonly QueryData<bool> _isRangedFormation;

	private readonly QueryData<bool> _isCavalryFormation;

	private readonly QueryData<bool> _isRangedCavalryFormation;

	private readonly QueryData<float> _movementSpeedMaximum;

	private readonly QueryData<float> _maximumMissileRange;

	private readonly QueryData<float> _missileRangeAdjusted;

	private readonly QueryData<float> _localInfantryUnitRatio;

	private readonly QueryData<float> _localRangedUnitRatio;

	private readonly QueryData<float> _localCavalryUnitRatio;

	private readonly QueryData<float> _localRangedCavalryUnitRatio;

	private readonly QueryData<float> _localAllyPower;

	private readonly QueryData<float> _localEnemyPower;

	private readonly QueryData<float> _localPowerRatio;

	private readonly QueryData<float> _casualtyRatio;

	private readonly QueryData<bool> _isUnderRangedAttack;

	private readonly QueryData<float> _underRangedAttackRatio;

	private readonly QueryData<float> _makingRangedAttackRatio;

	private readonly QueryData<Formation> _mainFormation;

	private readonly QueryData<float> _mainFormationReliabilityFactor;

	private readonly QueryData<Vec2> _weightedAverageEnemyPosition;

	private readonly QueryData<Agent> _closestEnemyAgent;

	private readonly QueryData<Formation> _closestSignificantlyLargeEnemyFormation;

	private readonly QueryData<Formation> _fastestSignificantlyLargeEnemyFormation;

	private readonly QueryData<Vec2> _highGroundCloseToForeseenBattleGround;

	private readonly QueryData<bool> _isUnderCavalryChargeFromFront;

	public TeamQuerySystem Team => Formation.Team.QuerySystem;

	public float FormationPower => _formationPower.Value;

	public float FormationPowerReadOnly => _formationPower.GetCachedValueUnlessTooOld();

	public float FormationMeleeFightingPower => _formationMeleeFightingPower.Value;

	public float FormationMeleeFightingPowerReadOnly => _formationMeleeFightingPower.GetCachedValueUnlessTooOld();

	public Vec2 EstimatedDirection => _estimatedDirection.Value;

	public Vec2 EstimatedDirectionReadOnly => _estimatedDirection.GetCachedValueUnlessTooOld();

	public float EstimatedInterval => _estimatedInterval.Value;

	public float EstimatedIntervalReadOnly => _estimatedInterval.GetCachedValueUnlessTooOld();

	public Vec2 AverageAllyPosition => _averageAllyPosition.Value;

	public Vec2 AverageAllyPositionReadOnly => _averageAllyPosition.GetCachedValueUnlessTooOld();

	public float IdealAverageDisplacement => _idealAverageDisplacement.Value;

	public float IdealAverageDisplacementReadOnly => _idealAverageDisplacement.GetCachedValueUnlessTooOld();

	public MBList<Agent> LocalAllyUnits => _localAllyUnits.Value;

	public MBList<Agent> LocalAllyUnitsReadOnly => _localAllyUnits.GetCachedValueUnlessTooOld();

	public MBList<Agent> LocalEnemyUnits => _localEnemyUnits.Value;

	public MBList<Agent> LocalEnemyUnitsReadOnly => _localEnemyUnits.GetCachedValueUnlessTooOld();

	public FormationClass MainClass => _mainClass.Value;

	public FormationClass MainClassReadOnly => _mainClass.GetCachedValueUnlessTooOld();

	public float InfantryUnitRatio => _infantryUnitRatio.Value;

	public float InfantryUnitRatioReadOnly => _infantryUnitRatio.GetCachedValueUnlessTooOld();

	public float HasShieldUnitRatio => _hasShieldUnitRatio.Value;

	public float HasShieldUnitRatioReadOnly => _hasShieldUnitRatio.GetCachedValueUnlessTooOld();

	public float HasThrowingUnitRatio => _hasThrowingUnitRatio.Value;

	public float HasThrowingUnitRatioReadOnly => _hasThrowingUnitRatio.GetCachedValueUnlessTooOld();

	public float RangedUnitRatio => _rangedUnitRatio.Value;

	public float RangedUnitRatioReadOnly => _rangedUnitRatio.GetCachedValueUnlessTooOld();

	public int InsideCastleUnitCountIncludingUnpositioned => _insideCastleUnitCountIncludingUnpositioned.Value;

	public int InsideCastleUnitCountIncludingUnpositionedReadOnly => _insideCastleUnitCountIncludingUnpositioned.GetCachedValueUnlessTooOld();

	public int InsideCastleUnitCountPositioned => _insideCastleUnitCountPositioned.Value;

	public int InsideCastleUnitCountPositionedReadOnly => _insideCastleUnitCountPositioned.GetCachedValueUnlessTooOld();

	public float CavalryUnitRatio => _cavalryUnitRatio.Value;

	public float CavalryUnitRatioReadOnly => _cavalryUnitRatio.GetCachedValueUnlessTooOld();

	public float RangedCavalryUnitRatio => _rangedCavalryUnitRatio.Value;

	public float RangedCavalryUnitRatioReadOnly => _rangedCavalryUnitRatio.GetCachedValueUnlessTooOld();

	public bool IsMeleeFormation => _isMeleeFormation.Value;

	public bool IsMeleeFormationReadOnly => _isMeleeFormation.GetCachedValueUnlessTooOld();

	public bool IsInfantryFormation => _isInfantryFormation.Value;

	public bool IsInfantryFormationReadOnly => _isInfantryFormation.GetCachedValueUnlessTooOld();

	public bool HasShield => _hasShield.Value;

	public bool HasShieldReadOnly => _hasShield.GetCachedValueUnlessTooOld();

	public bool HasThrowing => _hasThrowing.Value;

	public bool HasThrowingReadOnly => _hasThrowing.GetCachedValueUnlessTooOld();

	public bool IsRangedFormation => _isRangedFormation.Value;

	public bool IsRangedFormationReadOnly => _isRangedFormation.GetCachedValueUnlessTooOld();

	public bool IsCavalryFormation => _isCavalryFormation.Value;

	public bool IsCavalryFormationReadOnly => _isCavalryFormation.GetCachedValueUnlessTooOld();

	public bool IsRangedCavalryFormation => _isRangedCavalryFormation.Value;

	public bool IsRangedCavalryFormationReadOnly => _isRangedCavalryFormation.GetCachedValueUnlessTooOld();

	public float MovementSpeedMaximum => _movementSpeedMaximum.Value;

	public float MovementSpeedMaximumReadOnly => _movementSpeedMaximum.GetCachedValueUnlessTooOld();

	public float MaximumMissileRange => _maximumMissileRange.Value;

	public float MaximumMissileRangeReadOnly => _maximumMissileRange.GetCachedValueUnlessTooOld();

	public float MissileRangeAdjusted => _missileRangeAdjusted.Value;

	public float MissileRangeAdjustedReadOnly => _missileRangeAdjusted.GetCachedValueUnlessTooOld();

	public float LocalInfantryUnitRatio => _localInfantryUnitRatio.Value;

	public float LocalInfantryUnitRatioReadOnly => _localInfantryUnitRatio.GetCachedValueUnlessTooOld();

	public float LocalRangedUnitRatio => _localRangedUnitRatio.Value;

	public float LocalRangedUnitRatioReadOnly => _localRangedUnitRatio.GetCachedValueUnlessTooOld();

	public float LocalCavalryUnitRatio => _localCavalryUnitRatio.Value;

	public float LocalCavalryUnitRatioReadOnly => _localCavalryUnitRatio.GetCachedValueUnlessTooOld();

	public float LocalRangedCavalryUnitRatio => _localRangedCavalryUnitRatio.Value;

	public float LocalRangedCavalryUnitRatioReadOnly => _localRangedCavalryUnitRatio.GetCachedValueUnlessTooOld();

	public float LocalAllyPower => _localAllyPower.Value;

	public float LocalAllyPowerReadOnly => _localAllyPower.GetCachedValueUnlessTooOld();

	public float LocalEnemyPower => _localEnemyPower.Value;

	public float LocalEnemyPowerReadOnly => _localEnemyPower.GetCachedValueUnlessTooOld();

	public float LocalPowerRatio => _localPowerRatio.Value;

	public float LocalPowerRatioReadOnly => _localPowerRatio.GetCachedValueUnlessTooOld();

	public float CasualtyRatio => _casualtyRatio.Value;

	public float CasualtyRatioReadOnly => _casualtyRatio.GetCachedValueUnlessTooOld();

	public bool IsUnderRangedAttack => _isUnderRangedAttack.Value;

	public bool IsUnderRangedAttackReadOnly => _isUnderRangedAttack.GetCachedValueUnlessTooOld();

	public float UnderRangedAttackRatio => _underRangedAttackRatio.Value;

	public float UnderRangedAttackRatioReadOnly => _underRangedAttackRatio.GetCachedValueUnlessTooOld();

	public float MakingRangedAttackRatio => _makingRangedAttackRatio.Value;

	public float MakingRangedAttackRatioReadOnly => _makingRangedAttackRatio.GetCachedValueUnlessTooOld();

	public Formation MainFormation => _mainFormation.Value;

	public Formation MainFormationReadOnly => _mainFormation.GetCachedValueUnlessTooOld();

	public float MainFormationReliabilityFactor => _mainFormationReliabilityFactor.Value;

	public float MainFormationReliabilityFactorReadOnly => _mainFormationReliabilityFactor.GetCachedValueUnlessTooOld();

	public Vec2 WeightedAverageEnemyPosition => _weightedAverageEnemyPosition.Value;

	public Vec2 WeightedAverageEnemyPositionReadOnly => _weightedAverageEnemyPosition.GetCachedValueUnlessTooOld();

	public Agent ClosestEnemyAgent => _closestEnemyAgent.Value;

	public Agent ClosestEnemyAgentReadOnly => _closestEnemyAgent.GetCachedValueUnlessTooOld();

	public FormationQuerySystem ClosestSignificantlyLargeEnemyFormation
	{
		get
		{
			if (_closestSignificantlyLargeEnemyFormation.Value == null || _closestSignificantlyLargeEnemyFormation.Value.CountOfUnits == 0)
			{
				_closestSignificantlyLargeEnemyFormation.Expire();
			}
			return _closestSignificantlyLargeEnemyFormation.Value?.QuerySystem;
		}
	}

	public FormationQuerySystem ClosestSignificantlyLargeEnemyFormationReadOnly => _closestSignificantlyLargeEnemyFormation.GetCachedValueUnlessTooOld()?.QuerySystem;

	public FormationQuerySystem FastestSignificantlyLargeEnemyFormation
	{
		get
		{
			if (_fastestSignificantlyLargeEnemyFormation.Value == null || _fastestSignificantlyLargeEnemyFormation.Value.CountOfUnits == 0)
			{
				_fastestSignificantlyLargeEnemyFormation.Expire();
			}
			return _fastestSignificantlyLargeEnemyFormation.Value?.QuerySystem;
		}
	}

	public FormationQuerySystem FastestSignificantlyLargeEnemyFormationReadOnly => _fastestSignificantlyLargeEnemyFormation.GetCachedValueUnlessTooOld()?.QuerySystem;

	public Vec2 HighGroundCloseToForeseenBattleGround => _highGroundCloseToForeseenBattleGround.Value;

	public Vec2 HighGroundCloseToForeseenBattleGroundReadOnly => _highGroundCloseToForeseenBattleGround.GetCachedValueUnlessTooOld();

	public bool IsUnderCavalryChargeFromFront => _isUnderCavalryChargeFromFront.Value;

	public FormationQuerySystem(Formation formation)
	{
		FormationQuerySystem formationQuerySystem = this;
		Formation = formation;
		Mission mission = Mission.Current;
		_formationPower = new QueryData<float>(formation.GetFormationPower, 2.5f);
		_formationMeleeFightingPower = new QueryData<float>(formation.GetFormationMeleeFightingPower, 2.5f);
		_estimatedDirection = new QueryData<Vec2>(delegate
		{
			if (formation.CountOfUnitsWithoutDetachedOnes > 0)
			{
				Vec2 averagePositionOfUnits = formation.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: true);
				float num23 = 0f;
				float num24 = 0f;
				Vec2 orderLocalAveragePosition = formation.OrderLocalAveragePosition;
				int num25 = 0;
				foreach (Agent unitsWithoutLooseDetachedOne in formation.UnitsWithoutLooseDetachedOnes)
				{
					Vec2? localPositionOfUnitOrDefault2 = formation.Arrangement.GetLocalPositionOfUnitOrDefault(unitsWithoutLooseDetachedOne);
					if (localPositionOfUnitOrDefault2.HasValue)
					{
						Vec2 value = localPositionOfUnitOrDefault2.Value;
						Vec2 asVec = unitsWithoutLooseDetachedOne.Position.AsVec2;
						num23 += (value.x - orderLocalAveragePosition.x) * (asVec.x - averagePositionOfUnits.x) + (value.y - orderLocalAveragePosition.y) * (asVec.y - averagePositionOfUnits.y);
						num24 += (value.x - orderLocalAveragePosition.x) * (asVec.y - averagePositionOfUnits.y) - (value.y - orderLocalAveragePosition.y) * (asVec.x - averagePositionOfUnits.x);
						num25++;
					}
				}
				if (num25 > 0)
				{
					float num26 = 1f / (float)num25;
					num23 *= num26;
					num24 *= num26;
					float num27 = TaleWorlds.Library.MathF.Sqrt(num23 * num23 + num24 * num24);
					if (num27 > 0f)
					{
						float num28 = TaleWorlds.Library.MathF.Acos(MBMath.ClampFloat(num23 / num27, -1f, 1f));
						Vec2 result3 = Vec2.FromRotation(num28);
						Vec2 result4 = Vec2.FromRotation(0f - num28);
						float num29 = 0f;
						float num30 = 0f;
						foreach (Agent unitsWithoutLooseDetachedOne2 in formation.UnitsWithoutLooseDetachedOnes)
						{
							Vec2? localPositionOfUnitOrDefault3 = formation.Arrangement.GetLocalPositionOfUnitOrDefault(unitsWithoutLooseDetachedOne2);
							if (localPositionOfUnitOrDefault3.HasValue)
							{
								Vec2 vec2 = result3.TransformToParentUnitF(localPositionOfUnitOrDefault3.Value - orderLocalAveragePosition);
								Vec2 vec3 = result4.TransformToParentUnitF(localPositionOfUnitOrDefault3.Value - orderLocalAveragePosition);
								Vec2 asVec2 = unitsWithoutLooseDetachedOne2.Position.AsVec2;
								num29 += (vec2 - asVec2 + averagePositionOfUnits).LengthSquared;
								num30 += (vec3 - asVec2 + averagePositionOfUnits).LengthSquared;
							}
						}
						if (!(num29 < num30))
						{
							return result4;
						}
						return result3;
					}
				}
			}
			return new Vec2(0f, 1f);
		}, 0.2f);
		_estimatedInterval = new QueryData<float>(delegate
		{
			if (formation.CountOfUnitsWithoutDetachedOnes > 0)
			{
				Vec2 estimatedDirection = formation.QuerySystem.EstimatedDirection;
				Vec2 currentPosition = formation.CurrentPosition;
				float num19 = 0f;
				float num20 = 0f;
				foreach (Agent unitsWithoutLooseDetachedOne3 in formation.UnitsWithoutLooseDetachedOnes)
				{
					Vec2? localPositionOfUnitOrDefault = formation.Arrangement.GetLocalPositionOfUnitOrDefault(unitsWithoutLooseDetachedOne3);
					if (localPositionOfUnitOrDefault.HasValue)
					{
						Vec2 vec = estimatedDirection.TransformToLocalUnitF(unitsWithoutLooseDetachedOne3.Position.AsVec2 - currentPosition);
						Vec2 va = localPositionOfUnitOrDefault.Value - vec;
						Vec2 vb = formation.Arrangement.GetLocalPositionOfUnitOrDefaultWithAdjustment(unitsWithoutLooseDetachedOne3, 1f).Value - localPositionOfUnitOrDefault.Value;
						if (vb.IsNonZero())
						{
							float num21 = vb.Normalize();
							float num22 = Vec2.DotProduct(va, vb);
							num19 += num22 * num21;
							num20 += num21 * num21;
						}
					}
				}
				if (num20 != 0f)
				{
					return Math.Max(0f, (0f - num19) / num20 + formation.Interval);
				}
			}
			return formation.Interval;
		}, 0.2f);
		_averageAllyPosition = new QueryData<Vec2>(delegate
		{
			int num18 = 0;
			Vec2 zero = Vec2.Zero;
			foreach (Team team in mission.Teams)
			{
				if (team.IsFriendOf(formation.Team))
				{
					foreach (Formation item in formationQuerySystem.Formation.Team.FormationsIncludingSpecialAndEmpty)
					{
						if (item.CountOfUnits > 0 && item != formation)
						{
							num18 += item.CountOfUnits;
							zero += item.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false) * item.CountOfUnits;
						}
					}
				}
			}
			return (num18 > 0) ? (zero * (1f / (float)num18)) : formationQuerySystem.Formation.CachedAveragePosition;
		}, 5f);
		_idealAverageDisplacement = new QueryData<float>(() => TaleWorlds.Library.MathF.Sqrt(formation.Width * formation.Width * 0.5f * 0.5f + formation.Depth * formation.Depth * 0.5f * 0.5f) / 2f, 5f);
		_localAllyUnits = new QueryData<MBList<Agent>>(() => mission.GetNearbyAllyAgents(formationQuerySystem.Formation.CachedAveragePosition, 30f, formation.Team, formationQuerySystem._localAllyUnits.GetCachedValue()), 5f, new MBList<Agent>());
		_localEnemyUnits = new QueryData<MBList<Agent>>(() => mission.GetNearbyEnemyAgents(formationQuerySystem.Formation.CachedAveragePosition, 30f, formation.Team, formationQuerySystem._localEnemyUnits.GetCachedValue()), 5f, new MBList<Agent>());
		_infantryUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsBelongingToPhysicalClass(FormationClass.Infantry, excludeBannerBearers: false) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_hasShieldUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsWithCondition(QueryLibrary.HasShield) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_hasThrowingUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsWithCondition(QueryLibrary.HasThrown) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_rangedUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsBelongingToPhysicalClass(FormationClass.Ranged, excludeBannerBearers: false) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_cavalryUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsBelongingToPhysicalClass(FormationClass.Cavalry, excludeBannerBearers: false) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_rangedCavalryUnitRatio = new QueryData<float>(() => (formation.CountOfUnits > 0) ? ((float)formation.GetCountOfUnitsBelongingToPhysicalClass(FormationClass.HorseArcher, excludeBannerBearers: false) / (float)formation.CountOfUnits) : 0f, 2.5f);
		_isMeleeFormation = new QueryData<bool>(() => formationQuerySystem.InfantryUnitRatio + formationQuerySystem.CavalryUnitRatio > formationQuerySystem.RangedUnitRatio + formationQuerySystem.RangedCavalryUnitRatio, 5f);
		_isInfantryFormation = new QueryData<bool>(() => formationQuerySystem.InfantryUnitRatio >= formationQuerySystem.RangedUnitRatio && formationQuerySystem.InfantryUnitRatio >= formationQuerySystem.CavalryUnitRatio && formationQuerySystem.InfantryUnitRatio >= formationQuerySystem.RangedCavalryUnitRatio, 5f);
		_hasShield = new QueryData<bool>(() => formationQuerySystem.HasShieldUnitRatio >= 0.4f, 5f);
		_hasThrowing = new QueryData<bool>(() => formationQuerySystem.HasThrowingUnitRatio >= 0.5f, 5f);
		_isRangedFormation = new QueryData<bool>(() => formationQuerySystem.RangedUnitRatio > formationQuerySystem.InfantryUnitRatio && formationQuerySystem.RangedUnitRatio >= formationQuerySystem.CavalryUnitRatio && formationQuerySystem.RangedUnitRatio >= formationQuerySystem.RangedCavalryUnitRatio, 5f);
		_isCavalryFormation = new QueryData<bool>(() => formationQuerySystem.CavalryUnitRatio > formationQuerySystem.InfantryUnitRatio && formationQuerySystem.CavalryUnitRatio > formationQuerySystem.RangedUnitRatio && formationQuerySystem.CavalryUnitRatio >= formationQuerySystem.RangedCavalryUnitRatio, 5f);
		_isRangedCavalryFormation = new QueryData<bool>(() => formationQuerySystem.RangedCavalryUnitRatio > formationQuerySystem.InfantryUnitRatio && formationQuerySystem.RangedCavalryUnitRatio > formationQuerySystem.RangedUnitRatio && formationQuerySystem.RangedCavalryUnitRatio > formationQuerySystem.CavalryUnitRatio, 5f);
		QueryData<float>.SetupSyncGroup(_infantryUnitRatio, _hasShieldUnitRatio, _rangedUnitRatio, _cavalryUnitRatio, _rangedCavalryUnitRatio, _isMeleeFormation, _isInfantryFormation, _hasShield, _isRangedFormation, _isCavalryFormation, _isRangedCavalryFormation);
		_movementSpeedMaximum = new QueryData<float>(formation.GetAverageMaximumMovementSpeedOfUnits, 10f);
		_maximumMissileRange = new QueryData<float>(delegate
		{
			if (formation.CountOfUnits == 0)
			{
				return 0f;
			}
			float maximumRange = 0f;
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				if (agent.MaximumMissileRange > maximumRange)
				{
					maximumRange = agent.MaximumMissileRange;
				}
			});
			return maximumRange;
		}, 10f);
		_missileRangeAdjusted = new QueryData<float>(delegate
		{
			if (formation.CountOfUnits == 0)
			{
				return 0f;
			}
			float sum = 0f;
			formation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				sum += agent.MissileRangeAdjusted;
			});
			return sum / (float)formation.CountOfUnits;
		}, 10f);
		_localInfantryUnitRatio = new QueryData<float>(() => (formationQuerySystem.LocalAllyUnits.Count != 0) ? (1f * (float)formationQuerySystem.LocalAllyUnits.Count(QueryLibrary.IsInfantry) / (float)formationQuerySystem.LocalAllyUnits.Count) : 0f, 15f);
		_localRangedUnitRatio = new QueryData<float>(() => (formationQuerySystem.LocalAllyUnits.Count != 0) ? (1f * (float)formationQuerySystem.LocalAllyUnits.Count(QueryLibrary.IsRanged) / (float)formationQuerySystem.LocalAllyUnits.Count) : 0f, 15f);
		_localCavalryUnitRatio = new QueryData<float>(() => (formationQuerySystem.LocalAllyUnits.Count != 0) ? (1f * (float)formationQuerySystem.LocalAllyUnits.Count(QueryLibrary.IsCavalry) / (float)formationQuerySystem.LocalAllyUnits.Count) : 0f, 15f);
		_localRangedCavalryUnitRatio = new QueryData<float>(() => (formationQuerySystem.LocalAllyUnits.Count != 0) ? (1f * (float)formationQuerySystem.LocalAllyUnits.Count(QueryLibrary.IsRangedCavalry) / (float)formationQuerySystem.LocalAllyUnits.Count) : 0f, 15f);
		QueryData<float>.SetupSyncGroup(_localInfantryUnitRatio, _localRangedUnitRatio, _localCavalryUnitRatio, _localRangedCavalryUnitRatio);
		_localAllyPower = new QueryData<float>(() => formationQuerySystem.LocalAllyUnits.Sum((Agent lau) => lau.CharacterPowerCached), 5f);
		_localEnemyPower = new QueryData<float>(() => formationQuerySystem.LocalEnemyUnits.Sum((Agent leu) => leu.CharacterPowerCached), 5f);
		_localPowerRatio = new QueryData<float>(() => MBMath.ClampFloat(TaleWorlds.Library.MathF.Sqrt((formationQuerySystem.LocalAllyUnits.Sum((Agent lau) => lau.CharacterPowerCached) + 1f) * 1f / (formationQuerySystem.LocalEnemyUnits.Sum((Agent leu) => leu.CharacterPowerCached) + 1f)), 0.5f, 1.75f), 5f);
		_casualtyRatio = new QueryData<float>(delegate
		{
			if (formation.CountOfUnits == 0)
			{
				return 0f;
			}
			int num17 = mission.GetMissionBehavior<CasualtyHandler>()?.GetCasualtyCountOfFormation(formation) ?? 0;
			return 1f - (float)num17 * 1f / (float)(num17 + formation.CountOfUnits);
		}, 10f);
		_isUnderRangedAttack = new QueryData<bool>(() => formation.GetUnderAttackTypeOfUnits(10f) == Agent.UnderAttackType.UnderRangedAttack, 3f);
		_underRangedAttackRatio = new QueryData<float>(delegate
		{
			float currentTime2 = MBCommon.GetTotalMissionTime();
			int countOfUnitsWithCondition2 = formation.GetCountOfUnitsWithCondition((Agent agent) => currentTime2 - agent.LastRangedHitTime < 10f);
			return (formation.CountOfUnits <= 0) ? 0f : ((float)countOfUnitsWithCondition2 / (float)formation.CountOfUnits);
		}, 3f);
		_makingRangedAttackRatio = new QueryData<float>(delegate
		{
			float currentTime = MBCommon.GetTotalMissionTime();
			int countOfUnitsWithCondition = formation.GetCountOfUnitsWithCondition((Agent agent) => currentTime - agent.LastRangedAttackTime < 10f);
			return (formation.CountOfUnits <= 0) ? 0f : ((float)countOfUnitsWithCondition / (float)formation.CountOfUnits);
		}, 3f);
		_closestEnemyAgent = new QueryData<Agent>(delegate
		{
			float num15 = float.MaxValue;
			Agent result2 = null;
			foreach (Team team2 in mission.Teams)
			{
				if (team2.IsEnemyOf(formation.Team))
				{
					foreach (Agent activeAgent in team2.ActiveAgents)
					{
						float num16 = activeAgent.Position.DistanceSquared(new Vec3(formationQuerySystem.Formation.CachedAveragePosition, formationQuerySystem.Formation.CachedMedianPosition.GetNavMeshZ()));
						if (num16 < num15)
						{
							num15 = num16;
							result2 = activeAgent;
						}
					}
				}
			}
			return result2;
		}, 1.5f);
		_closestSignificantlyLargeEnemyFormation = new QueryData<Formation>(delegate
		{
			float num11 = float.MaxValue;
			Formation formation4 = null;
			float num12 = float.MaxValue;
			Formation formation5 = null;
			foreach (Team team3 in mission.Teams)
			{
				if (team3.IsEnemyOf(formation.Team))
				{
					foreach (Formation item2 in team3.FormationsIncludingSpecialAndEmpty)
					{
						if (item2.CountOfUnits > 0)
						{
							if (item2.QuerySystem.FormationPower / formationQuerySystem.FormationPower > 0.2f || item2.QuerySystem.FormationPower * formationQuerySystem.Team.TeamPower / (item2.Team.QuerySystem.TeamPower * formationQuerySystem.FormationPower) > 0.2f)
							{
								float num13 = item2.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(new Vec3(formationQuerySystem.Formation.CachedAveragePosition, formationQuerySystem.Formation.CachedMedianPosition.GetNavMeshZ()));
								if (num13 < num11)
								{
									num11 = num13;
									formation4 = item2;
								}
							}
							else if (formation4 == null)
							{
								float num14 = item2.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(new Vec3(formationQuerySystem.Formation.CachedAveragePosition, formationQuerySystem.Formation.CachedMedianPosition.GetNavMeshZ()));
								if (num14 < num12)
								{
									num12 = num14;
									formation5 = item2;
								}
							}
						}
					}
				}
			}
			return formation4 ?? formation5;
		}, 1.5f);
		_fastestSignificantlyLargeEnemyFormation = new QueryData<Formation>(delegate
		{
			float num7 = float.MaxValue;
			Formation formation2 = null;
			float num8 = float.MaxValue;
			Formation formation3 = null;
			foreach (Team team4 in mission.Teams)
			{
				if (team4.IsEnemyOf(formation.Team))
				{
					foreach (Formation item3 in team4.FormationsIncludingSpecialAndEmpty)
					{
						if (item3.CountOfUnits > 0)
						{
							if (item3.QuerySystem.FormationPower / formationQuerySystem.FormationPower > 0.2f || item3.QuerySystem.FormationPower * formationQuerySystem.Team.TeamPower / (item3.Team.QuerySystem.TeamPower * formationQuerySystem.FormationPower) > 0.2f)
							{
								float num9 = item3.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(new Vec3(formationQuerySystem.Formation.CachedAveragePosition, formationQuerySystem.Formation.CachedMedianPosition.GetNavMeshZ())) / (item3.CachedMovementSpeed * item3.CachedMovementSpeed);
								if (num9 < num7)
								{
									num7 = num9;
									formation2 = item3;
								}
							}
							else if (formation2 == null)
							{
								float num10 = item3.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(new Vec3(formationQuerySystem.Formation.CachedAveragePosition, formationQuerySystem.Formation.CachedMedianPosition.GetNavMeshZ())) / (item3.CachedMovementSpeed * item3.CachedMovementSpeed);
								if (num10 < num8)
								{
									num8 = num10;
									formation3 = item3;
								}
							}
						}
					}
				}
			}
			return formation2 ?? formation3;
		}, 1.5f);
		_mainClass = new QueryData<FormationClass>(delegate
		{
			FormationClass result = FormationClass.Infantry;
			float num6 = formationQuerySystem.InfantryUnitRatio;
			if (formationQuerySystem.RangedUnitRatio > num6)
			{
				result = FormationClass.Ranged;
				num6 = formationQuerySystem.RangedUnitRatio;
			}
			if (formationQuerySystem.CavalryUnitRatio > num6)
			{
				result = FormationClass.Cavalry;
				num6 = formationQuerySystem.CavalryUnitRatio;
			}
			if (formationQuerySystem.RangedCavalryUnitRatio > num6)
			{
				result = FormationClass.HorseArcher;
			}
			return result;
		}, 15f);
		_mainFormation = new QueryData<Formation>(() => formation.Team.FormationsIncludingSpecialAndEmpty.FirstOrDefault((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation && f != formation), 15f);
		_mainFormationReliabilityFactor = new QueryData<float>(delegate
		{
			if (formationQuerySystem.MainFormation == null)
			{
				return 0f;
			}
			float num4 = ((formationQuerySystem.MainFormation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Charge || formationQuerySystem.MainFormation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget || formationQuerySystem.MainFormation.GetReadonlyMovementOrderReference() == MovementOrder.MovementOrderRetreat) ? 0.5f : 1f);
			float num5 = ((formationQuerySystem.MainFormation.GetUnderAttackTypeOfUnits(10f) == Agent.UnderAttackType.UnderMeleeAttack) ? 0.8f : 1f);
			return num4 * num5;
		}, 5f);
		_weightedAverageEnemyPosition = new QueryData<Vec2>(() => formationQuerySystem.Formation.Team.GetWeightedAverageOfEnemies(formationQuerySystem.Formation.CurrentPosition), 0.5f);
		_highGroundCloseToForeseenBattleGround = new QueryData<Vec2>(delegate
		{
			WorldPosition center = formationQuerySystem.Formation.CachedMedianPosition;
			center.SetVec2(formationQuerySystem.Formation.CachedAveragePosition);
			WorldPosition referencePosition = formationQuerySystem.Team.MedianTargetFormationPosition;
			return mission.FindPositionWithBiggestSlopeTowardsDirectionInSquare(ref center, formationQuerySystem.Formation.CachedAveragePosition.Distance(formationQuerySystem.Team.MedianTargetFormationPosition.AsVec2) * 0.5f, ref referencePosition).AsVec2;
		}, 10f);
		_insideCastleUnitCountIncludingUnpositioned = new QueryData<int>(() => formationQuerySystem.Formation.CountUnitsOnNavMeshIDMod10(1, includeOnlyPositionedUnits: false), 3f);
		_insideCastleUnitCountPositioned = new QueryData<int>(() => formationQuerySystem.Formation.CountUnitsOnNavMeshIDMod10(1, includeOnlyPositionedUnits: true), 3f);
		_isUnderCavalryChargeFromFront = new QueryData<bool>(delegate
		{
			FormationQuerySystem closestSignificantlyLargeEnemyFormation = formationQuerySystem.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
			if (closestSignificantlyLargeEnemyFormation != null && closestSignificantlyLargeEnemyFormation.IsCavalryFormationReadOnly)
			{
				Vec2 cachedCurrentVelocity = closestSignificantlyLargeEnemyFormation.Formation.CachedCurrentVelocity;
				float num = cachedCurrentVelocity.Normalize();
				Vec2 v = formationQuerySystem.Formation.CachedMedianPosition.AsVec2 - closestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2;
				float num2 = v.Normalize();
				bool num3 = cachedCurrentVelocity.DotProduct(v) > 0.75f;
				bool flag = formationQuerySystem.Formation.Arrangement is CircularFormation || formationQuerySystem.Formation.Arrangement is SquareFormation || v.DotProduct(formationQuerySystem.Formation.Direction) < -0.75f;
				if (num3 && flag)
				{
					return num2 / num < 15f;
				}
			}
			return false;
		}, 2f);
		InitializeTelemetryScopeNames();
	}

	public void EvaluateAllPreliminaryQueryData()
	{
		float currentTime = Mission.Current.CurrentTime;
		_infantryUnitRatio.Evaluate(currentTime);
		_hasShieldUnitRatio.Evaluate(currentTime);
		_rangedUnitRatio.Evaluate(currentTime);
		_cavalryUnitRatio.Evaluate(currentTime);
		_rangedCavalryUnitRatio.Evaluate(currentTime);
		_isInfantryFormation.Evaluate(currentTime);
		_hasShield.Evaluate(currentTime);
		_isRangedFormation.Evaluate(currentTime);
		_isCavalryFormation.Evaluate(currentTime);
		_isRangedCavalryFormation.Evaluate(currentTime);
		_isMeleeFormation.Evaluate(currentTime);
	}

	public void ForceExpireCavalryUnitRatio()
	{
		_cavalryUnitRatio.Expire();
	}

	public void Expire()
	{
		_formationPower.Expire();
		_formationMeleeFightingPower.Expire();
		_estimatedDirection.Expire();
		_averageAllyPosition.Expire();
		_idealAverageDisplacement.Expire();
		_localAllyUnits.Expire();
		_localEnemyUnits.Expire();
		_mainClass.Expire();
		_infantryUnitRatio.Expire();
		_hasShieldUnitRatio.Expire();
		_rangedUnitRatio.Expire();
		_cavalryUnitRatio.Expire();
		_rangedCavalryUnitRatio.Expire();
		_isMeleeFormation.Expire();
		_isInfantryFormation.Expire();
		_hasShield.Expire();
		_isRangedFormation.Expire();
		_isCavalryFormation.Expire();
		_isRangedCavalryFormation.Expire();
		_movementSpeedMaximum.Expire();
		_maximumMissileRange.Expire();
		_missileRangeAdjusted.Expire();
		_localInfantryUnitRatio.Expire();
		_localRangedUnitRatio.Expire();
		_localCavalryUnitRatio.Expire();
		_localRangedCavalryUnitRatio.Expire();
		_localAllyPower.Expire();
		_localEnemyPower.Expire();
		_localPowerRatio.Expire();
		_casualtyRatio.Expire();
		_isUnderRangedAttack.Expire();
		_underRangedAttackRatio.Expire();
		_makingRangedAttackRatio.Expire();
		_mainFormation.Expire();
		_mainFormationReliabilityFactor.Expire();
		_weightedAverageEnemyPosition.Expire();
		_closestSignificantlyLargeEnemyFormation.Expire();
		_fastestSignificantlyLargeEnemyFormation.Expire();
		_highGroundCloseToForeseenBattleGround.Expire();
	}

	public void ExpireAfterUnitAddRemove()
	{
		_formationPower.Expire();
		float currentTime = Mission.Current.CurrentTime;
		_infantryUnitRatio.Evaluate(currentTime);
		_hasShieldUnitRatio.Evaluate(currentTime);
		_rangedUnitRatio.Evaluate(currentTime);
		_cavalryUnitRatio.Evaluate(currentTime);
		_rangedCavalryUnitRatio.Evaluate(currentTime);
		_isMeleeFormation.Evaluate(currentTime);
		_isInfantryFormation.Evaluate(currentTime);
		_hasShield.Evaluate(currentTime);
		_isRangedFormation.Evaluate(currentTime);
		_isCavalryFormation.Evaluate(currentTime);
		_isRangedCavalryFormation.Evaluate(currentTime);
		_mainClass.Evaluate(currentTime);
		if (Formation.CountOfUnits == 0)
		{
			_infantryUnitRatio.SetValue(0f, currentTime);
			_hasShieldUnitRatio.SetValue(0f, currentTime);
			_rangedUnitRatio.SetValue(0f, currentTime);
			_cavalryUnitRatio.SetValue(0f, currentTime);
			_rangedCavalryUnitRatio.SetValue(0f, currentTime);
			_isMeleeFormation.SetValue(value: false, currentTime);
			_isInfantryFormation.SetValue(value: true, currentTime);
			_hasShield.SetValue(value: false, currentTime);
			_isRangedFormation.SetValue(value: false, currentTime);
			_isCavalryFormation.SetValue(value: false, currentTime);
			_isRangedCavalryFormation.SetValue(value: false, currentTime);
		}
	}

	private void InitializeTelemetryScopeNames()
	{
	}

	public float GetClassWeightedFactor(float infantryWeight, float rangedWeight, float cavalryWeight, float rangedCavalryWeight)
	{
		return InfantryUnitRatio * infantryWeight + RangedUnitRatio * rangedWeight + CavalryUnitRatio * cavalryWeight + RangedCavalryUnitRatio * rangedCavalryWeight;
	}
}
