using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public class ListedServerMapServerSubModule : MBSubModuleBase
{
	public const string ModuleName = "Multiplayer";

	public static ListedServerMapServerSubModule Instance;

	private Lazy<string[]> _ownedMaps = new Lazy<string[]>(() => Utilities.GetSingleModuleScenesOfModule("Multiplayer"));

	private readonly Dictionary<string, UniqueSceneId> _mapNameToUniqueIdMapping = new Dictionary<string, UniqueSceneId>();

	public CachedArchivedMap CurrentMapArchive;

	public IEnumerable<string> MapList
	{
		get
		{
			List<string> list = ServerSideIntermissionManager.Instance.AutomatedMapPool?.ToList() ?? new List<string>();
			list.AddRange(ServerSideIntermissionManager.Instance.UsableMaps);
			List<string> list2 = new List<string>();
			if (list.Count > 0)
			{
				foreach (string item in list)
				{
					if (!list2.Contains(item))
					{
						list2.Add(item);
					}
				}
			}
			string strValue = MultiplayerOptions.OptionType.PremadeGameType.GetStrValue();
			if (!list2.Contains(strValue))
			{
				list2.Insert(0, strValue);
			}
			return list2.Where((string mapName) => _ownedMaps.Value.Contains(mapName));
		}
	}

	public ArchivedMap GetArchivedMap(string mapName)
	{
		if (CurrentMapArchive != null && CurrentMapArchive.Name == mapName)
		{
			return CurrentMapArchive;
		}
		return ArchivedMap.FromMap(mapName);
	}

	public UniqueSceneId GetCachedUniqueIdForMap(string mapName)
	{
		if (_mapNameToUniqueIdMapping.TryGetValue(mapName, out var value))
		{
			return value;
		}
		if (Utilities.TryGetUniqueIdentifiersForScene(mapName, out var identifiers))
		{
			_mapNameToUniqueIdMapping[mapName] = identifiers;
			return identifiers;
		}
		_mapNameToUniqueIdMapping[mapName] = null;
		return null;
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		ModLogger.Log("Initialized mission with map '" + MultiplayerOptions.OptionType.PremadeGameType.GetStrValue() + "'");
		try
		{
			if (CurrentMapArchive != null)
			{
				CurrentMapArchive.Dispose();
			}
			CurrentMapArchive = ArchivedMap.FromCurrent();
		}
		catch (Exception ex)
		{
			ModLogger.Warn("Failed to create archive, exception message: " + ex.Message);
			ModLogger.Log(ex.StackTrace, 0, Debug.DebugColor.Red);
			throw;
		}
	}

	public override void OnMultiplayerGameStart(Game game, object starterObject)
	{
		base.OnMultiplayerGameStart(game, starterObject);
	}

	protected override void OnSubModuleLoad()
	{
		Instance = this;
		base.OnSubModuleLoad();
		DedicatedServerConsoleCommandManager.AddType(typeof(ListedServerMapServerCommandManager));
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		ArchivedMap.DisposeInactiveMaps();
		if (CurrentMapArchive != null)
		{
			CurrentMapArchive.Dispose();
		}
	}
}
