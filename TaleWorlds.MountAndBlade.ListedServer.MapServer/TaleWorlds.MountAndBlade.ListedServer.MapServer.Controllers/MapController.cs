using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ListedServer.MapServer.Controllers;

[ApiController]
[Route("maps")]
[AllowAnonymous]
public class MapController : ControllerBase
{
	private static readonly Regex ValidMapNamePattern = new Regex("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

	private const int MaxMapNameLength = 64;

	private static bool IsValidMapName(string mapName)
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

	private void LogRequestCompletion(string requestName, string details = null)
	{
		string text = "Served request successfully for action '" + requestName + "'";
		ModLogger.Log((details == null) ? text : (text + " (details: '" + details + "')"));
	}

	private FileStreamResult GetFileResult(ArchivedMap archivedMap)
	{
		return File(archivedMap.Stream, "application/zip", archivedMap.Name);
	}

	[HttpGet("current")]
	public IActionResult GetCurrentMap()
	{
		try
		{
			CachedArchivedMap currentMapArchive = ListedServerMapServerSubModule.Instance.CurrentMapArchive;
			if (currentMapArchive == null)
			{
				return NotFound("There is no map archive in memory");
			}
			FileStreamResult fileStreamResult = null;
			try
			{
				fileStreamResult = GetFileResult(currentMapArchive);
			}
			catch (Exception ex)
			{
				ModLogger.Warn("Error: " + ex.Message);
				return BadRequest("Failure, see server console for details");
			}
			LogRequestCompletion("GetCurrentMap");
			return fileStreamResult;
		}
		catch (Exception ex2)
		{
			ModLogger.Warn("Unhandled error in GetCurrentMap: " + ex2.Message);
			return StatusCode(500, "Internal server error");
		}
	}

	[HttpGet("list/{mapName}")]
	public IActionResult GetMap(string mapName)
	{
		if (!IsValidMapName(mapName))
		{
			ModLogger.Warn($"Rejected invalid map name request: length={mapName?.Length ?? 0}");
			return BadRequest("Invalid map name");
		}
		if (!ListedServerMapServerSubModule.Instance.MapList.Contains(mapName))
		{
			return NotFound("Map '" + mapName + "' is not available on this server");
		}
		try
		{
			ArchivedMap archivedMap = ListedServerMapServerSubModule.Instance.GetArchivedMap(mapName);
			FileStreamResult fileStreamResult = null;
			try
			{
				fileStreamResult = GetFileResult(archivedMap);
			}
			catch (InvalidOperationException ex) when (ex.Data.Contains("ZIP_LOCKED"))
			{
				ModLogger.Warn("Warn: Map '" + mapName + "' was requested, but is already being zipped");
				return Conflict("Map '" + mapName + "' is already being prepared, try later");
			}
			catch (Exception ex2)
			{
				ModLogger.Warn("Error: " + ex2.Message);
				return BadRequest("Failure, see server console for details");
			}
			LogRequestCompletion("GetMap", "map=" + mapName);
			return fileStreamResult;
		}
		catch (Exception ex3)
		{
			ModLogger.Warn("Unhandled error in GetMap: " + ex3.Message);
			return StatusCode(500, "Internal server error");
		}
	}

	[HttpGet("list")]
	public IActionResult GetMapList()
	{
		try
		{
			string currentlyPlaying = ListedServerMapServerSubModule.Instance.CurrentMapArchive?.Name;
			var maps = ListedServerMapServerSubModule.Instance.MapList.Select(delegate(string mapName)
			{
				UniqueSceneId cachedUniqueIdForMap = ListedServerMapServerSubModule.Instance.GetCachedUniqueIdForMap(mapName);
				return new
				{
					name = mapName,
					uniqueToken = cachedUniqueIdForMap?.UniqueToken,
					revision = cachedUniqueIdForMap?.Revision
				};
			});
			return Ok(JsonConvert.SerializeObject((object)new { currentlyPlaying, maps }));
		}
		catch (Exception ex)
		{
			ModLogger.Warn("Unhandled error in GetMapList: " + ex.Message);
			return StatusCode(500, "Internal server error");
		}
	}
}
