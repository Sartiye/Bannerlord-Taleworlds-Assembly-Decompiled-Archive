using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class PollHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "PollHotkeyCategory";

	public const int AcceptPoll = 109;

	public const int DeclinePoll = 110;

	public PollHotkeyCategory()
		: base("PollHotkeyCategory", 111)
	{
		RegisterGameKeys();
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(109, "AcceptPoll", "PollHotkeyCategory", InputKey.F10, InputKey.ControllerLBumper, GameKeyMainCategories.PollCategory));
		RegisterGameKey(new GameKey(110, "DeclinePoll", "PollHotkeyCategory", InputKey.F11, InputKey.ControllerRBumper, GameKeyMainCategories.PollCategory));
	}
}
