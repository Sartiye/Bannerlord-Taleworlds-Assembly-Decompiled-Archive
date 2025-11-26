using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using TaleWorlds.AchievementSystem;
using TaleWorlds.ActivitySystem;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Diamond;
using TaleWorlds.Diamond.AccessProvider.Steam;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.Steam;

public class SteamPlatformServices : IPlatformServices
{
	private PlatformInitParams _initParams;

	private SteamFriendListService _steamFriendListService;

	private IFriendListService[] _friendListServices;

	public SteamAchievementService _achievementService;

	private Dictionary<PlayerId, AvatarData> _avatarCache = new Dictionary<PlayerId, AvatarData>();

	private const int CommandRequestTimeOut = 5000;

	private Callback<PersonaStateChange_t> _personaStateChangeT;

	private Callback<AvatarImageLoaded_t> _avatarImageLoadedT;

	private Callback<GamepadTextInputDismissed_t> _gamepadTextInputDismissedT;

	private static List<CSteamID> _avatarUpdates = new List<CSteamID>();

	private static List<CSteamID> _avatarLoadedUpdates = new List<CSteamID>();

	private static List<CSteamID> _nameUpdates = new List<CSteamID>();

	private static SteamPlatformServices Instance => PlatformServices.Instance as SteamPlatformServices;

	internal bool Initialized { get; private set; }

	string IPlatformServices.ProviderName => "Steam";

	string IPlatformServices.UserId => ((ulong)SteamUser.GetSteamID()).ToString();

	PlayerId IPlatformServices.PlayerId => SteamUser.GetSteamID().ToPlayerId();

	bool IPlatformServices.UserLoggedIn
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	string IPlatformServices.UserDisplayName
	{
		get
		{
			if (!Initialized)
			{
				return string.Empty;
			}
			return SteamFriends.GetPersonaName();
		}
	}

	IReadOnlyCollection<PlayerId> IPlatformServices.BlockedUsers => new List<PlayerId>();

	bool IPlatformServices.IsPermanentMuteAvailable => true;

	public event Action<AvatarData> OnAvatarUpdated;

	public event Action<string> OnNameUpdated;

	public event Action<bool, TextObject> OnSignInStateUpdated;

	public event Action OnBlockedUserListUpdated;

	public event Action<string> OnTextEnteredFromPlatform;

	public event Action OnTextCanceledFromPlatform;

	public SteamPlatformServices(PlatformInitParams initParams)
	{
		_initParams = initParams;
		AvatarServices.AddAvatarService(PlayerIdProvidedTypes.Steam, new SteamPlatformAvatarService(this));
		_achievementService = new SteamAchievementService(this);
		_steamFriendListService = new SteamFriendListService(this);
	}

	void IPlatformServices.LoginUser()
	{
		throw new NotImplementedException();
	}

	bool IPlatformServices.Initialize(IFriendListService[] additionalFriendListServices)
	{
		_friendListServices = new IFriendListService[additionalFriendListServices.Length + 1];
		_friendListServices[0] = _steamFriendListService;
		for (int i = 0; i < additionalFriendListServices.Length; i++)
		{
			_friendListServices[i + 1] = additionalFriendListServices[i];
		}
		if (!SteamAPI.Init())
		{
			return false;
		}
		ModuleHelper.InitializePlatformModuleExtension(new SteamModuleExtension(), null);
		InitCallbacks();
		_achievementService.Initialize();
		SteamUserStats.RequestCurrentStats();
		Initialized = true;
		return true;
	}

	void IPlatformServices.Tick(float dt)
	{
		if (Initialized)
		{
			SteamAPI.RunCallbacks();
			_achievementService.Tick(dt);
		}
	}

	void IPlatformServices.Terminate()
	{
		SteamAPI.Shutdown();
	}

	bool IPlatformServices.ShowGamepadTextInput(string descriptionText, string existingText, uint maxChars, bool isObfuscated)
	{
		if (Initialized)
		{
			return SteamUtils.ShowGamepadTextInput((EGamepadTextInputMode)(isObfuscated ? 1 : 0), (EGamepadTextInputLineMode)0, descriptionText, maxChars, existingText);
		}
		return false;
	}

	bool IPlatformServices.IsPlayerProfileCardAvailable(PlayerId providedId)
	{
		return false;
	}

	void IPlatformServices.ShowPlayerProfileCard(PlayerId providedId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		SteamFriends.ActivateGameOverlayToUser("steamid", providedId.ToSteamId());
	}

	async Task<AvatarData> IPlatformServices.GetUserAvatar(PlayerId providedId)
	{
		if (!providedId.IsValid)
		{
			return null;
		}
		if (_avatarCache.ContainsKey(providedId))
		{
			return _avatarCache[providedId];
		}
		if (_avatarCache.Count > 300)
		{
			_avatarCache.Clear();
		}
		long startTime = DateTime.UtcNow.Ticks;
		CSteamID steamId = providedId.ToSteamId();
		if (SteamFriends.RequestUserInformation(steamId, false))
		{
			while (!_avatarUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_avatarUpdates.Remove(steamId);
		}
		int userAvatar = SteamFriends.GetLargeFriendAvatar(steamId);
		if (userAvatar == -1)
		{
			while (!_avatarLoadedUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_avatarLoadedUpdates.Remove(steamId);
			while (userAvatar == -1 && !TimedOut(startTime, 5000L))
			{
				userAvatar = SteamFriends.GetLargeFriendAvatar(steamId);
			}
		}
		if (userAvatar != -1)
		{
			uint num = default(uint);
			uint num2 = default(uint);
			SteamUtils.GetImageSize(userAvatar, ref num, ref num2);
			if (num != 0)
			{
				uint num3 = num * num2 * 4;
				byte[] array = new byte[num3];
				if (SteamUtils.GetImageRGBA(userAvatar, array, (int)num3))
				{
					AvatarData avatarData = new AvatarData(array, num, num2);
					lock (_avatarCache)
					{
						if (!_avatarCache.ContainsKey(providedId))
						{
							_avatarCache.Add(providedId, avatarData);
						}
					}
					return avatarData;
				}
			}
		}
		return null;
	}

	public void ClearAvatarCache()
	{
		_avatarCache.Clear();
	}

	private bool TimedOut(long startUTCTicks, long timeOut)
	{
		return (DateTime.Now - new DateTime(startUTCTicks)).Milliseconds > timeOut;
	}

	internal async Task<string> GetUserName(PlayerId providedId)
	{
		if (!providedId.IsValid || providedId.ProvidedType != PlayerIdProvidedTypes.Steam)
		{
			return null;
		}
		long startTime = DateTime.UtcNow.Ticks;
		CSteamID steamId = providedId.ToSteamId();
		if (SteamFriends.RequestUserInformation(steamId, false))
		{
			while (!_nameUpdates.Contains(steamId) && !TimedOut(startTime, 5000L))
			{
				await Task.Delay(5);
			}
			_nameUpdates.Remove(steamId);
		}
		string friendPersonaName = SteamFriends.GetFriendPersonaName(steamId);
		if (!string.IsNullOrEmpty(friendPersonaName))
		{
			return friendPersonaName;
		}
		return null;
	}

	PlatformInitParams IPlatformServices.GetInitParams()
	{
		return _initParams;
	}

	IAchievementService IPlatformServices.GetAchievementService()
	{
		return _achievementService;
	}

	IActivityService IPlatformServices.GetActivityService()
	{
		return new TestActivityService();
	}

	async Task<bool> IPlatformServices.ShowOverlayForWebPage(string url)
	{
		await Task.Delay(0);
		SteamFriends.ActivateGameOverlayToWebPage(url, (EActivateGameOverlayToWebPageMode)0);
		return true;
	}

	void IPlatformServices.CheckPrivilege(Privilege privilege, bool displayResolveUI, PrivilegeResult callback)
	{
		callback(result: true);
	}

	void IPlatformServices.CheckPermissionWithUser(Permission privilege, PlayerId targetPlayerId, PermissionResult callback)
	{
		callback(result: true);
	}

	bool IPlatformServices.RegisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
	{
		return false;
	}

	bool IPlatformServices.UnregisterPermissionChangeEvent(PlayerId targetPlayerId, Permission permission, PermissionChanged callback)
	{
		return false;
	}

	void IPlatformServices.ShowRestrictedInformation()
	{
	}

	Task<bool> IPlatformServices.VerifyString(string content)
	{
		return Task.FromResult(result: true);
	}

	void IPlatformServices.GetPlatformId(PlayerId playerId, Action<object> callback)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		callback(playerId.ToSteamId());
	}

	void IPlatformServices.OnFocusGained()
	{
	}

	internal Task<bool> GetUserOnlineStatus(PlayerId providedId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		SteamUtils.GetAppID();
		if ((int)SteamFriends.GetFriendPersonaState(new CSteamID(providedId.Part4)) != 0)
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	internal Task<bool> IsPlayingThisGame(PlayerId providedId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		AppId_t appID = SteamUtils.GetAppID();
		FriendGameInfo_t val = default(FriendGameInfo_t);
		if (SteamFriends.GetFriendGamePlayed(new CSteamID(providedId.Part4), ref val) && ((CGameID)(ref val.m_gameID)).AppID() == appID)
		{
			return Task.FromResult(result: true);
		}
		return Task.FromResult(result: false);
	}

	internal async Task<PlayerId> GetUserWithName(string name)
	{
		await Task.Delay(0);
		int friendCount = SteamFriends.GetFriendCount((EFriendFlags)4);
		CSteamID steamId = default(CSteamID);
		int num = 0;
		for (int i = 0; i < friendCount; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, (EFriendFlags)4);
			if (SteamFriends.GetFriendPersonaName(friendByIndex).Equals(name))
			{
				steamId = friendByIndex;
				num++;
			}
		}
		friendCount = SteamFriends.GetCoplayFriendCount();
		for (int j = 0; j < friendCount; j++)
		{
			CSteamID coplayFriend = SteamFriends.GetCoplayFriend(j);
			if (SteamFriends.GetFriendPersonaName(coplayFriend).Equals(name))
			{
				steamId = coplayFriend;
				num++;
			}
		}
		if (num != 1)
		{
			return default(PlayerId);
		}
		return steamId.ToPlayerId();
	}

	private async void OnAvatarUpdateReceived(ulong userId)
	{
		int userAvatar = -1;
		while (userAvatar == -1)
		{
			userAvatar = SteamFriends.GetLargeFriendAvatar(new CSteamID(userId));
			await Task.Delay(5);
		}
		if (userAvatar == -1)
		{
			return;
		}
		uint num = default(uint);
		uint num2 = default(uint);
		SteamUtils.GetImageSize(userAvatar, ref num, ref num2);
		if (num != 0)
		{
			uint num3 = num * num2 * 4;
			byte[] array = new byte[num3];
			if (SteamUtils.GetImageRGBA(userAvatar, array, (int)num3))
			{
				this.OnAvatarUpdated?.Invoke(new AvatarData(array, num, num2));
			}
		}
	}

	private void OnNameUpdateReceived(PlayerId userId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		string friendPersonaName = SteamFriends.GetFriendPersonaName(userId.ToSteamId());
		if (!string.IsNullOrEmpty(friendPersonaName))
		{
			this.OnNameUpdated?.Invoke(friendPersonaName);
		}
	}

	private void Dummy()
	{
		if (this.OnSignInStateUpdated != null)
		{
			this.OnSignInStateUpdated(arg1: false, null);
		}
		if (this.OnBlockedUserListUpdated != null)
		{
			this.OnBlockedUserListUpdated();
		}
	}

	private void InitCallbacks()
	{
		_personaStateChangeT = Callback<PersonaStateChange_t>.Create((DispatchDelegate<PersonaStateChange_t>)UserInformationUpdated);
		_avatarImageLoadedT = Callback<AvatarImageLoaded_t>.Create((DispatchDelegate<AvatarImageLoaded_t>)AvatarLoaded);
		_gamepadTextInputDismissedT = Callback<GamepadTextInputDismissed_t>.Create((DispatchDelegate<GamepadTextInputDismissed_t>)GamepadTextInputDismissed);
	}

	private static void AvatarLoaded(AvatarImageLoaded_t avatarImageLoadedT)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_avatarLoadedUpdates.Add(avatarImageLoadedT.m_steamID);
	}

	private static void UserInformationUpdated(PersonaStateChange_t pCallback)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if ((pCallback.m_nChangeFlags & 0x40) != 0)
		{
			_avatarUpdates.Add(new CSteamID(pCallback.m_ulSteamID));
			Instance.OnAvatarUpdateReceived(pCallback.m_ulSteamID);
		}
		else if ((pCallback.m_nChangeFlags & 1) != 0)
		{
			_nameUpdates.Add(new CSteamID(pCallback.m_ulSteamID));
			Instance.OnNameUpdateReceived(SteamPlayerIdExtensions.ToPlayerId(new CSteamID(pCallback.m_ulSteamID)));
		}
		else if ((pCallback.m_nChangeFlags & 0x10) != 0)
		{
			HandleOnUserStatusChanged(SteamPlayerIdExtensions.ToPlayerId(new CSteamID(pCallback.m_ulSteamID)));
		}
	}

	private void GamepadTextInputDismissed(GamepadTextInputDismissed_t gamepadTextInputDismissedT)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (gamepadTextInputDismissedT.m_bSubmitted)
		{
			string obj = default(string);
			SteamUtils.GetEnteredGamepadTextInput(ref obj, SteamUtils.GetEnteredGamepadTextLength());
			this.OnTextEnteredFromPlatform?.Invoke(obj);
		}
		else
		{
			this.OnTextCanceledFromPlatform?.Invoke();
		}
	}

	private static void HandleOnUserStatusChanged(PlayerId playerId)
	{
		Instance._steamFriendListService.HandleOnUserStatusChanged(playerId);
	}

	Task<ILoginAccessProvider> IPlatformServices.CreateLobbyClientLoginProvider()
	{
		return Task.FromResult((ILoginAccessProvider)new SteamLoginAccessProvider());
	}

	IFriendListService[] IPlatformServices.GetFriendListServices()
	{
		return _friendListServices;
	}

	bool IPlatformServices.UsePlatformInvitationService(PlayerId targetPlayerId)
	{
		return false;
	}
}
