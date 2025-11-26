using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class LobbyPracticeState : GameState
{
	private bool _practiceOpened;

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_practiceOpened)
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_practiceOpened)
		{
			OpenPracticeMission();
			_practiceOpened = true;
		}
	}

	private void OpenPracticeMission()
	{
		BasicCharacterObject @object = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_heavy_cavalry_empire_hero");
		BasicCharacterObject object2 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_skirmisher_battania_troop");
		BasicCharacterObject object3 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_light_ranged_khuzait_troop");
		Game.Current.PlayerTroop = @object;
		BasicCultureObject object4;
		BasicCultureObject basicCultureObject = (object4 = Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire"));
		Banner banner = object4.Banner;
		Banner banner2 = basicCultureObject.Banner;
		CustomBattleCombatant customBattleCombatant = new CustomBattleCombatant(new TextObject("{=sSJSTe5p}Player Party"), object4, banner);
		CustomBattleCombatant customBattleCombatant2 = new CustomBattleCombatant(new TextObject("{=0xC75dN6}Enemy Party"), basicCultureObject, banner2);
		customBattleCombatant.AddCharacter(@object, 1);
		customBattleCombatant2.AddCharacter(@object, 1);
		customBattleCombatant.AddCharacter(object2, 3);
		customBattleCombatant2.AddCharacter(object2, 3);
		customBattleCombatant.AddCharacter(object3, 8);
		customBattleCombatant2.AddCharacter(object3, 8);
		customBattleCombatant.SetGeneral(@object);
		customBattleCombatant2.SetGeneral(@object);
		customBattleCombatant.Side = BattleSideEnum.Attacker;
		customBattleCombatant2.Side = BattleSideEnum.Defender;
		MultiplayerPracticeMissions.OpenMultiplayerPracticeMission("mp_practice_battle", @object, customBattleCombatant, customBattleCombatant2, isPlayerGeneral: true, null, "", "summer");
	}
}
