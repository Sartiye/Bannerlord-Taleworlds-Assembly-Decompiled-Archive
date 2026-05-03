using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.DedicatedCommunityServer.Controllers;

[AllowAnonymous]
public class GameController : Controller
{
	public static ulong GetInt64HashCode(string strText)
	{
		ulong result = 0uL;
		if (!string.IsNullOrEmpty(strText))
		{
			byte[] bytes = Encoding.Unicode.GetBytes(strText);
			SHA256 sHA = SHA256.Create();
			byte[] value = sHA.ComputeHash(bytes);
			ulong num = BitConverter.ToUInt64(value, 0);
			ulong num2 = BitConverter.ToUInt64(value, 8);
			ulong num3 = BitConverter.ToUInt64(value, 16);
			ulong num4 = BitConverter.ToUInt64(value, 24);
			result = num ^ num2 ^ num3 ^ num4;
			sHA.Dispose();
		}
		return result;
	}

	public static PlayerId GetPlayerIdFromUserName(string userName)
	{
		return new PlayerId(1, 0uL, GetInt64HashCode(userName));
	}

	[HttpPost("[controller]/ConnectNewPlayer")]
	public async Task<PlayerJoinGameResponseDataFromHost> ConnectNewPlayer([FromBody] CommunityGameJoinData joinData)
	{
		string name = joinData.Name;
		PlayerId playerId = joinData.PlayerId;
		PlayerData playerData = new PlayerData();
		playerData.FillWithNewPlayer(playerId, playerId, new string[0]);
		playerData.Username = name;
		playerData.LastPlayerName = name;
		PlayerJoinGameData playerJoinGameData = new PlayerJoinGameData();
		playerJoinGameData.PlayerData = playerData;
		playerJoinGameData.Name = name;
		playerJoinGameData.UsedCosmetics = new Dictionary<string, List<string>>();
		ConnectNewPlayerJob job = new ConnectNewPlayerJob(playerJoinGameData);
		Module.CurrentModule.JobManager.AddJob(job);
		while (!job.Finished)
		{
			await Task.Delay(5);
		}
		return job.Response;
	}
}
