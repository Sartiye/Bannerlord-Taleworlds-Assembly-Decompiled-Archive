using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer;

public abstract class ArchivedMap
{
	public const string ZipLockedKey = "ZIP_LOCKED";

	private static readonly Regex ValidMapNamePattern = new Regex("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

	private const int MaxMapNameLength = 64;

	private static Dictionary<string, InactiveArchivedMap> inactiveMapArchives = new Dictionary<string, InactiveArchivedMap>();

	public abstract Stream Stream { get; }

	public string Name { get; private set; }

	internal static bool IsValidMapName(string mapName)
	{
		if (string.IsNullOrEmpty(mapName))
		{
			return false;
		}
		if (mapName.Length > 64)
		{
			return false;
		}
		return ValidMapNamePattern.IsMatch(mapName);
	}

	public ArchivedMap(string name)
	{
		Name = name;
	}

	public static CachedArchivedMap FromCurrent()
	{
		string strValue = MultiplayerOptions.OptionType.PremadeGameType.GetStrValue();
		return new CachedArchivedMap(ZipMap(strValue), strValue);
	}

	public static InactiveArchivedMap FromMap(string mapName)
	{
		if (!IsValidMapName(mapName))
		{
			throw new ArgumentException("Invalid map name rejected: '" + mapName?.Substring(0, Math.Min(mapName?.Length ?? 0, 50)) + "'");
		}
		if (!inactiveMapArchives.TryGetValue(mapName, out var value))
		{
			value = new InactiveArchivedMap(mapName);
			inactiveMapArchives[mapName] = value;
		}
		return value;
	}

	protected static FileStream ZipMap(string mapName)
	{
		if (!IsValidMapName(mapName))
		{
			throw new ArgumentException("Invalid map name rejected in ZipMap: '" + mapName?.Substring(0, Math.Min(mapName?.Length ?? 0, 50)) + "'");
		}
		string? directoryName = System.IO.Path.GetDirectoryName(Utilities.GetFullFilePathOfScene(mapName));
		string tempFileName = GetTempFileName(mapName);
		ModLogger.Log($"Creating archive for map '{mapName}' to '{tempFileName}'");
		ZipFile.CreateFromDirectory(directoryName, tempFileName);
		ModLogger.Log("Created archive for map '" + mapName + "'");
		return new FileStream(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
	}

	protected static string GetTempFileName(string anyIdentifier)
	{
		return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BL_" + anyIdentifier + "_" + Guid.NewGuid());
	}

	public static void DisposeInactiveMaps()
	{
		foreach (InactiveArchivedMap value in inactiveMapArchives.Values)
		{
			value.DisposeTempFile();
		}
	}
}
