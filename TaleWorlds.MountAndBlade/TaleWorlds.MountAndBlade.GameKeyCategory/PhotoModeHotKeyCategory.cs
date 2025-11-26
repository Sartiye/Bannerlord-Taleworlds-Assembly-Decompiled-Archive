using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GameKeyCategory;

public sealed class PhotoModeHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "PhotoModeHotKeyCategory";

	public const int HideUI = 93;

	public const int CameraRollLeft = 94;

	public const int CameraRollRight = 95;

	public const int ToggleCameraFollowMode = 98;

	public const int TakePicture = 96;

	public const int TakePictureWithAdditionalPasses = 97;

	public const int ToggleMouse = 99;

	public const int ToggleVignette = 100;

	public const int ToggleCharacters = 101;

	public const int Reset = 108;

	public const string FasterCamera = "FasterCamera";

	public PhotoModeHotKeyCategory()
		: base("PhotoModeHotKeyCategory", 111)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.LeftShift),
			new Key(InputKey.ControllerRTrigger)
		};
		RegisterHotKey(new HotKey("FasterCamera", "PhotoModeHotKeyCategory", keys));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(93, "HideUI", "PhotoModeHotKeyCategory", InputKey.H, InputKey.ControllerRUp, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(94, "CameraRollLeft", "PhotoModeHotKeyCategory", InputKey.Q, InputKey.ControllerLBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(95, "CameraRollRight", "PhotoModeHotKeyCategory", InputKey.E, InputKey.ControllerRBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(98, "ToggleCameraFollowMode", "PhotoModeHotKeyCategory", InputKey.V, InputKey.ControllerRLeft, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(96, "TakePicture", "PhotoModeHotKeyCategory", InputKey.Enter, InputKey.ControllerRDown, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(97, "TakePictureWithAdditionalPasses", "PhotoModeHotKeyCategory", InputKey.BackSpace, InputKey.ControllerRBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(99, "ToggleMouse", "PhotoModeHotKeyCategory", InputKey.C, InputKey.ControllerLThumb, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(100, "ToggleVignette", "PhotoModeHotKeyCategory", InputKey.X, InputKey.ControllerRThumb, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(101, "ToggleCharacters", "PhotoModeHotKeyCategory", InputKey.B, InputKey.ControllerRRight, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(108, "Reset", "PhotoModeHotKeyCategory", InputKey.T, InputKey.ControllerLOption, GameKeyMainCategories.PhotoModeCategory));
	}

	private void RegisterGameAxisKeys()
	{
	}
}
