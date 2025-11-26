using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed.General;

public class MPGeneralKillNotificationItemVM : ViewModel
{
	private readonly Action<MPGeneralKillNotificationItemVM> _onRemove;

	private readonly Banner DefaultBanner = Banner.CreateOneColoredEmptyBanner(92);

	private string _murdererName;

	private string _murdererType;

	private string _victimName;

	private string _victimType;

	private MPTeammateCompassTargetVM _murdererCompassElement;

	private MPTeammateCompassTargetVM _victimCompassElement;

	private Color _color1;

	private Color _color2;

	private bool _isPlayerDeath;

	private bool _isItemInitializationOver;

	private bool _isVictimBot;

	private bool _isMurdererBot;

	private bool _isDamageNotification;

	private bool _isDamagedMount;

	private bool _isRelatedToFriendlyTroop;

	private bool _isFriendlyTroopDeath;

	private string _message;

	[DataSourceProperty]
	public string MurdererName
	{
		get
		{
			return _murdererName;
		}
		set
		{
			if (value != _murdererName)
			{
				_murdererName = value;
				OnPropertyChangedWithValue(value, "MurdererName");
			}
		}
	}

	[DataSourceProperty]
	public string MurdererType
	{
		get
		{
			return _murdererType;
		}
		set
		{
			if (value != _murdererType)
			{
				_murdererType = value;
				OnPropertyChangedWithValue(value, "MurdererType");
			}
		}
	}

	[DataSourceProperty]
	public string VictimName
	{
		get
		{
			return _victimName;
		}
		set
		{
			if (value != _victimName)
			{
				_victimName = value;
				OnPropertyChangedWithValue(value, "VictimName");
			}
		}
	}

	[DataSourceProperty]
	public string VictimType
	{
		get
		{
			return _victimType;
		}
		set
		{
			if (value != _victimType)
			{
				_victimType = value;
				OnPropertyChangedWithValue(value, "VictimType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDamageNotification
	{
		get
		{
			return _isDamageNotification;
		}
		set
		{
			if (value != _isDamageNotification)
			{
				_isDamageNotification = value;
				OnPropertyChangedWithValue(value, "IsDamageNotification");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDamagedMount
	{
		get
		{
			return _isDamagedMount;
		}
		set
		{
			if (value != _isDamagedMount)
			{
				_isDamagedMount = value;
				OnPropertyChangedWithValue(value, "IsDamagedMount");
			}
		}
	}

	[DataSourceProperty]
	public Color Color1
	{
		get
		{
			return _color1;
		}
		set
		{
			if (value != _color1)
			{
				_color1 = value;
				OnPropertyChangedWithValue(value, "Color1");
			}
		}
	}

	[DataSourceProperty]
	public Color Color2
	{
		get
		{
			return _color2;
		}
		set
		{
			if (value != _color2)
			{
				_color2 = value;
				OnPropertyChangedWithValue(value, "Color2");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM MurdererCompassElement
	{
		get
		{
			return _murdererCompassElement;
		}
		set
		{
			if (value != _murdererCompassElement)
			{
				_murdererCompassElement = value;
				OnPropertyChangedWithValue(value, "MurdererCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public MPTeammateCompassTargetVM VictimCompassElement
	{
		get
		{
			return _victimCompassElement;
		}
		set
		{
			if (value != _victimCompassElement)
			{
				_victimCompassElement = value;
				OnPropertyChangedWithValue(value, "VictimCompassElement");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerDeath
	{
		get
		{
			return _isPlayerDeath;
		}
		set
		{
			if (value != _isPlayerDeath)
			{
				_isPlayerDeath = value;
				OnPropertyChangedWithValue(value, "IsPlayerDeath");
			}
		}
	}

	[DataSourceProperty]
	public bool IsItemInitializationOver
	{
		get
		{
			return _isItemInitializationOver;
		}
		set
		{
			if (value != _isItemInitializationOver)
			{
				_isItemInitializationOver = value;
				OnPropertyChangedWithValue(value, "IsItemInitializationOver");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVictimBot
	{
		get
		{
			return _isVictimBot;
		}
		set
		{
			if (value != _isVictimBot)
			{
				_isVictimBot = value;
				OnPropertyChangedWithValue(value, "IsVictimBot");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMurdererBot
	{
		get
		{
			return _isMurdererBot;
		}
		set
		{
			if (value != _isMurdererBot)
			{
				_isMurdererBot = value;
				OnPropertyChangedWithValue(value, "IsMurdererBot");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRelatedToFriendlyTroop
	{
		get
		{
			return _isRelatedToFriendlyTroop;
		}
		set
		{
			if (value != _isRelatedToFriendlyTroop)
			{
				_isRelatedToFriendlyTroop = value;
				OnPropertyChangedWithValue(value, "IsRelatedToFriendlyTroop");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFriendlyTroopDeath
	{
		get
		{
			return _isFriendlyTroopDeath;
		}
		set
		{
			if (value != _isFriendlyTroopDeath)
			{
				_isFriendlyTroopDeath = value;
				OnPropertyChangedWithValue(value, "IsFriendlyTroopDeath");
			}
		}
	}

	[DataSourceProperty]
	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			if (value != _message)
			{
				_message = value;
				OnPropertyChangedWithValue(value, "Message");
			}
		}
	}

	public MPGeneralKillNotificationItemVM(Agent affectedAgent, Agent affectorAgent, Agent assistedAgent, Action<MPGeneralKillNotificationItemVM> onRemove)
	{
		_onRemove = onRemove;
		IsDamageNotification = false;
		InitProperties(affectedAgent, affectorAgent);
		InitDeathProperties(affectedAgent, affectorAgent, assistedAgent);
	}

	public virtual void InitProperties(Agent affectedAgent, Agent affectorAgent)
	{
		IsItemInitializationOver = false;
		GetAgentColors(affectorAgent, out var color, out var color2);
		TargetIconType multiplayerAgentType = GetMultiplayerAgentType(affectorAgent);
		Banner agentBanner = GetAgentBanner(affectorAgent);
		bool flag = affectorAgent?.Team?.IsPlayerAlly ?? false;
		GetAgentColors(affectedAgent, out var color3, out var color4);
		TargetIconType multiplayerAgentType2 = GetMultiplayerAgentType(affectedAgent);
		Banner agentBanner2 = GetAgentBanner(affectedAgent);
		bool flag2 = affectedAgent.Team?.IsPlayerAlly ?? false;
		MurdererName = ((affectorAgent == null) ? "" : ((affectorAgent.MissionPeer != null) ? affectorAgent.MissionPeer.DisplayedName : affectorAgent.Name));
		MurdererType = multiplayerAgentType.ToString();
		IsMurdererBot = affectorAgent != null && !affectorAgent.IsPlayerControlled;
		MurdererCompassElement = new MPTeammateCompassTargetVM(multiplayerAgentType, color, color2, agentBanner, flag);
		VictimName = ((affectedAgent.MissionPeer != null) ? affectedAgent.MissionPeer.DisplayedName : affectedAgent.Name);
		VictimType = multiplayerAgentType2.ToString();
		IsVictimBot = !affectedAgent.IsPlayerControlled;
		VictimCompassElement = new MPTeammateCompassTargetVM(multiplayerAgentType2, color3, color4, agentBanner2, flag2);
		IsPlayerDeath = affectedAgent.IsMainAgent;
		if (flag && flag2)
		{
			Color1 = Color.FromUint(4278190080u);
			Color2 = Color.FromUint(uint.MaxValue);
		}
		else if (!flag && !flag2)
		{
			Color1 = Color.FromUint(4281545266u);
			Color2 = Color.FromUint(uint.MaxValue);
		}
		else
		{
			Color1 = Color.FromUint(color);
			Color2 = Color.FromUint(color2);
		}
		if (IsVictimBot && affectedAgent.Formation == Agent.Main?.Formation)
		{
			IsRelatedToFriendlyTroop = true;
			IsFriendlyTroopDeath = true;
		}
		else if (IsMurdererBot && affectorAgent != null && affectorAgent.Formation == Agent.Main?.Formation)
		{
			IsRelatedToFriendlyTroop = true;
		}
		IsItemInitializationOver = true;
	}

	public void InitDeathProperties(Agent affectedAgent, Agent affectorAgent, Agent assistedAgent)
	{
		IsItemInitializationOver = false;
		if (affectorAgent != null && affectorAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", affectedAgent.NameTextObject.ToString());
			Message = GameTexts.FindText("str_kill_feed_message").ToString();
		}
		else if (affectedAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", ((object)affectorAgent)?.ToString());
			Message = GameTexts.FindText("str_death_feed_message").ToString();
		}
		else if (assistedAgent != null && assistedAgent.IsMainAgent)
		{
			MBTextManager.SetTextVariable("TROOP_NAME", affectedAgent.NameTextObject.ToString());
			Message = GameTexts.FindText("str_assist_feed_message").ToString();
		}
		IsItemInitializationOver = true;
	}

	protected TargetIconType GetMultiplayerAgentType(Agent agent)
	{
		if (agent == null)
		{
			return TargetIconType.None;
		}
		if (!agent.IsHuman)
		{
			return TargetIconType.Monster;
		}
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
		if (mPHeroClassForCharacter == null)
		{
			Debug.FailedAssert("Hero class is not set for agent: " + agent.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\KillFeed\\General\\MPGeneralKillNotificationItemVM.cs", "GetMultiplayerAgentType", 116);
			return TargetIconType.None;
		}
		return mPHeroClassForCharacter.IconType;
	}

	private Banner GetAgentBanner(Agent agent)
	{
		Banner result = DefaultBanner;
		if (agent != null)
		{
			MissionPeer missionPeer = agent.MissionPeer?.GetComponent<MissionPeer>();
			if (agent.Team != null && missionPeer != null)
			{
				result = new Banner(missionPeer.Peer.BannerCode, agent.Team.Color, agent.Team.Color2);
			}
			else if (agent.Team != null && agent.Formation != null && !string.IsNullOrEmpty(agent.Formation.BannerCode))
			{
				result = new Banner(agent.Formation.BannerCode, agent.Team.Color, agent.Team.Color2);
			}
			else if (agent.Team != null)
			{
				result = agent.Team.Banner;
			}
		}
		return result;
	}

	private void GetAgentColors(Agent agent, out uint color1, out uint color2)
	{
		if (agent?.Team != null)
		{
			color1 = agent.Team.Color;
			color2 = agent.Team.Color2;
		}
		else
		{
			color1 = 4284111450u;
			color2 = uint.MaxValue;
		}
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
