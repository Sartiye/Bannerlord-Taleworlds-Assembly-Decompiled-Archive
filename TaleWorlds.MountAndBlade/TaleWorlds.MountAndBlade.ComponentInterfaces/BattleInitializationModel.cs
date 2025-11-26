using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class BattleInitializationModel : MBGameModel<BattleInitializationModel>
{
	public const int MinimumTroopCountForPlayerDeployment = 20;

	private bool _canPlayerSideDeployWithOOB;

	private bool _isCanPlayerSideDeployWithOOBCached;

	private bool _isInitialized;

	public abstract List<FormationClass> GetAllAvailableTroopTypes();

	protected abstract bool CanPlayerSideDeployWithOrderOfBattleAux();

	public bool CanPlayerSideDeployWithOrderOfBattle()
	{
		if (!_isCanPlayerSideDeployWithOOBCached)
		{
			_canPlayerSideDeployWithOOB = CanPlayerSideDeployWithOrderOfBattleAux();
			_isCanPlayerSideDeployWithOOBCached = true;
		}
		return _canPlayerSideDeployWithOOB;
	}

	public void InitializeModel()
	{
		_isCanPlayerSideDeployWithOOBCached = false;
		_isInitialized = true;
	}

	public void FinalizeModel()
	{
		_isInitialized = false;
	}
}
