using System.Collections.Generic;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class MapHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "MapHotKeyCategory";

	public const int QuickSave = 54;

	public const int PartyMoveUp = 50;

	public const int PartyMoveLeft = 53;

	public const int PartyMoveDown = 51;

	public const int PartyMoveRight = 52;

	public const int MapMoveUp = 46;

	public const int MapMoveDown = 47;

	public const int MapMoveLeft = 49;

	public const int MapMoveRight = 48;

	public const string MovementAxisX = "MapMovementAxisX";

	public const string MovementAxisY = "MapMovementAxisY";

	public const int MapFastMove = 55;

	public const int MapZoomIn = 56;

	public const int MapZoomOut = 57;

	public const int MapRotateLeft = 58;

	public const int MapRotateRight = 59;

	public const int MapCameraFollowMode = 64;

	public const int MapToggleFastForward = 65;

	public const int MapTrackSettlement = 66;

	public const int MapGoToEncylopedia = 67;

	public const string MapClick = "MapClick";

	public const string MapFollowModifier = "MapFollowModifier";

	public const string MapChangeCursorMode = "MapChangeCursorMode";

	public const int MapTimeStop = 60;

	public const int MapTimeNormal = 61;

	public const int MapTimeFastForward = 62;

	public const int MapTimeTogglePause = 63;

	public MapHotKeyCategory()
		: base("MapHotKeyCategory", 111)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> list = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) != 0f)
		{
			list.Add(new Key(InputKey.ControllerLOptionTap));
		}
		RegisterHotKey(new HotKey("MapClick", "MapHotKeyCategory", list));
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.LeftAlt),
			new Key(InputKey.ControllerLBumper)
		};
		RegisterHotKey(new HotKey("MapFollowModifier", "MapHotKeyCategory", keys));
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.ControllerRRight)
		};
		RegisterHotKey(new HotKey("MapChangeCursorMode", "MapHotKeyCategory", keys2));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(50, "PartyMoveUp", "MapHotKeyCategory", InputKey.Up, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(51, "PartyMoveDown", "MapHotKeyCategory", InputKey.Down, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(52, "PartyMoveRight", "MapHotKeyCategory", InputKey.Right, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(53, "PartyMoveLeft", "MapHotKeyCategory", InputKey.Left, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(54, "QuickSave", "MapHotKeyCategory", InputKey.F5, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(55, "MapFastMove", "MapHotKeyCategory", InputKey.LeftShift, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(56, "MapZoomIn", "MapHotKeyCategory", InputKey.MouseScrollUp, InputKey.ControllerRTrigger, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(57, "MapZoomOut", "MapHotKeyCategory", InputKey.MouseScrollDown, InputKey.ControllerLTrigger, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(58, "MapRotateLeft", "MapHotKeyCategory", InputKey.Q, InputKey.Invalid, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(59, "MapRotateRight", "MapHotKeyCategory", InputKey.E, InputKey.Invalid, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(60, "MapTimeStop", "MapHotKeyCategory", InputKey.D1, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(61, "MapTimeNormal", "MapHotKeyCategory", InputKey.D2, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(62, "MapTimeFastForward", "MapHotKeyCategory", InputKey.D3, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(63, "MapTimeTogglePause", "MapHotKeyCategory", InputKey.Space, InputKey.ControllerRLeft, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(64, "MapCameraFollowMode", "MapHotKeyCategory", InputKey.Invalid, InputKey.ControllerLThumb, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(65, "MapToggleFastForward", "MapHotKeyCategory", InputKey.Invalid, InputKey.ControllerRBumper, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(66, "MapTrackSettlement", "MapHotKeyCategory", InputKey.Invalid, InputKey.ControllerRThumb, GameKeyMainCategories.CampaignMapCategory));
		RegisterGameKey(new GameKey(67, "MapGoToEncylopedia", "MapHotKeyCategory", InputKey.Invalid, InputKey.ControllerLOption, GameKeyMainCategories.CampaignMapCategory));
	}

	private void RegisterGameAxisKeys()
	{
		GameKey gameKey = new GameKey(46, "MapMoveUp", "MapHotKeyCategory", InputKey.W, GameKeyMainCategories.CampaignMapCategory);
		GameKey gameKey2 = new GameKey(47, "MapMoveDown", "MapHotKeyCategory", InputKey.S, GameKeyMainCategories.CampaignMapCategory);
		GameKey gameKey3 = new GameKey(48, "MapMoveRight", "MapHotKeyCategory", InputKey.D, GameKeyMainCategories.CampaignMapCategory);
		GameKey gameKey4 = new GameKey(49, "MapMoveLeft", "MapHotKeyCategory", InputKey.A, GameKeyMainCategories.CampaignMapCategory);
		RegisterGameKey(gameKey);
		RegisterGameKey(gameKey2);
		RegisterGameKey(gameKey4);
		RegisterGameKey(gameKey3);
		RegisterGameAxisKey(new GameAxisKey("MapMovementAxisX", InputKey.ControllerLStick, gameKey3, gameKey4));
		RegisterGameAxisKey(new GameAxisKey("MapMovementAxisY", InputKey.ControllerLStick, gameKey, gameKey2, GameAxisKey.AxisType.Y));
	}
}
