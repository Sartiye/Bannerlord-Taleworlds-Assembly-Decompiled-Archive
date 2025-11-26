using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class MapNotificationHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "MapNotificationHotKeyCategory";

	public const string RemoveNotification = "RemoveNotification";

	public MapNotificationHotKeyCategory()
		: base("MapNotificationHotKeyCategory", 111)
	{
		RegisterHotKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("RemoveNotification", "MapNotificationHotKeyCategory", InputKey.ControllerRUp));
	}
}
