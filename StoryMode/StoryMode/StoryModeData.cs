using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace StoryMode;

public static class StoryModeData
{
	public static CampaignTime StorylineQuestHideoutHiddenDuration = CampaignTime.Hours(12f);

	private static Kingdom _northernEmpireKingdom;

	private static Kingdom _westernEmpireKingdom;

	private static Kingdom _southernEmpireKingdom;

	private static Kingdom _sturgiaKingdom;

	private static Kingdom _aseraiKingdom;

	private static Kingdom _vlandiaKingdom;

	private static Kingdom _battaniaKingdom;

	private static Kingdom _khuzaitKingdom;

	public static CultureObject ImperialCulture => NorthernEmpireKingdom.Culture;

	public static Kingdom NorthernEmpireKingdom
	{
		get
		{
			if (_northernEmpireKingdom != null)
			{
				return _northernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire")
				{
					_northernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "NorthernEmpireKingdom", 51);
			return null;
		}
	}

	public static Kingdom WesternEmpireKingdom
	{
		get
		{
			if (_westernEmpireKingdom != null)
			{
				return _westernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire_w")
				{
					_westernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "WesternEmpireKingdom", 76);
			return null;
		}
	}

	public static Kingdom SouthernEmpireKingdom
	{
		get
		{
			if (_southernEmpireKingdom != null)
			{
				return _southernEmpireKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "empire_s")
				{
					_southernEmpireKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "SouthernEmpireKingdom", 101);
			return null;
		}
	}

	public static Kingdom SturgiaKingdom
	{
		get
		{
			if (_sturgiaKingdom != null)
			{
				return _sturgiaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "sturgia")
				{
					_sturgiaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "SturgiaKingdom", 126);
			return null;
		}
	}

	public static Kingdom AseraiKingdom
	{
		get
		{
			if (_aseraiKingdom != null)
			{
				return _aseraiKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "aserai")
				{
					_aseraiKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "AseraiKingdom", 151);
			return null;
		}
	}

	public static Kingdom VlandiaKingdom
	{
		get
		{
			if (_vlandiaKingdom != null)
			{
				return _vlandiaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "vlandia")
				{
					_vlandiaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "VlandiaKingdom", 177);
			return null;
		}
	}

	public static Kingdom BattaniaKingdom
	{
		get
		{
			if (_battaniaKingdom != null)
			{
				return _battaniaKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "battania")
				{
					_battaniaKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "BattaniaKingdom", 204);
			return null;
		}
	}

	public static Kingdom KhuzaitKingdom
	{
		get
		{
			if (_khuzaitKingdom != null)
			{
				return _khuzaitKingdom;
			}
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.StringId == "khuzait")
				{
					_khuzaitKingdom = item;
					return item;
				}
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\StoryModeData.cs", "KhuzaitKingdom", 230);
			return null;
		}
	}

	public static void OnGameEnd()
	{
		_northernEmpireKingdom = null;
		_westernEmpireKingdom = null;
		_southernEmpireKingdom = null;
		_sturgiaKingdom = null;
		_aseraiKingdom = null;
		_vlandiaKingdom = null;
		_battaniaKingdom = null;
		_khuzaitKingdom = null;
	}

	public static bool IsKingdomImperial(Kingdom kingdomToCheck)
	{
		if (kingdomToCheck != null)
		{
			return kingdomToCheck.Culture == ImperialCulture;
		}
		return false;
	}
}
