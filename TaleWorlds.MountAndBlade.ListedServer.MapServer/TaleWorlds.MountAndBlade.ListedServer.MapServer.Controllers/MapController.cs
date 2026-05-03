using System;
using System.Linq;
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

	[HttpGet("list/{mapName}")]
	public IActionResult GetMap(string mapName)
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

	[HttpGet("list")]
	public IActionResult GetMapList()
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
}
