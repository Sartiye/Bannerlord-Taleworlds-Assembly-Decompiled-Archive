using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

public class GameKeyGroupVM : ViewModel
{
	private readonly Action<KeyOptionVM> _onKeybindRequest;

	private readonly Action<int, InputKey> _setAllKeysOfId;

	private readonly string _categoryId;

	private IEnumerable<GameKey> _keys;

	private string _description;

	private MBBindingList<GameKeyOptionVM> _gameKeys;

	[DataSourceProperty]
	public MBBindingList<GameKeyOptionVM> GameKeys
	{
		get
		{
			return _gameKeys;
		}
		set
		{
			if (value != _gameKeys)
			{
				_gameKeys = value;
				OnPropertyChangedWithValue(value, "GameKeys");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	public GameKeyGroupVM(string categoryId, IEnumerable<GameKey> keys, Action<KeyOptionVM> onKeybindRequest, Action<int, InputKey> setAllKeysOfId)
	{
		_onKeybindRequest = onKeybindRequest;
		_setAllKeysOfId = setAllKeysOfId;
		_categoryId = categoryId;
		_gameKeys = new MBBindingList<GameKeyOptionVM>();
		_keys = keys;
		PopulateGameKeys();
		RefreshValues();
	}

	private void PopulateGameKeys()
	{
		GameKeys.Clear();
		foreach (GameKey key in _keys)
		{
			bool num;
			if (!TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				if (!(key?.DefaultKeyboardKey != null))
				{
					continue;
				}
				if (key != null)
				{
					num = key.DefaultKeyboardKey.InputKey != InputKey.Invalid;
					goto IL_0088;
				}
			}
			else
			{
				if (!(key?.DefaultControllerKey != null))
				{
					continue;
				}
				if (key != null)
				{
					num = key.DefaultControllerKey.InputKey != InputKey.Invalid;
					goto IL_0088;
				}
			}
			goto IL_008a;
			IL_008a:
			GameKeys.Add(new GameKeyOptionVM(key, _onKeybindRequest, SetGameKey));
			continue;
			IL_0088:
			if (!num)
			{
				continue;
			}
			goto IL_008a;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = Module.CurrentModule.GlobalTextManager.FindText("str_key_category_name", _categoryId).ToString();
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM x)
		{
			x.RefreshValues();
		});
	}

	private void SetGameKey(GameKeyOptionVM option, InputKey newKey)
	{
		option.CurrentKey.ChangeKey(newKey);
		option.OptionValueText = Module.CurrentModule.GlobalTextManager.FindText("str_game_key_text", option.CurrentKey.ToString().ToLower()).ToString();
		_setAllKeysOfId(option.CurrentGameKey.Id, newKey);
	}

	internal void Update()
	{
		foreach (GameKeyOptionVM gameKey in GameKeys)
		{
			gameKey.Update();
		}
	}

	public void OnDone()
	{
		foreach (GameKeyOptionVM gameKey in GameKeys)
		{
			gameKey.OnDone();
		}
	}

	internal bool IsChanged()
	{
		for (int i = 0; i < GameKeys.Count; i++)
		{
			if (GameKeys[i].IsChanged())
			{
				return true;
			}
		}
		return false;
	}

	public void OnGamepadActiveStateChanged()
	{
		PopulateGameKeys();
		Update();
		OnDone();
	}

	public void Cancel()
	{
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM g)
		{
			g.Revert();
		});
	}

	public void ApplyValues()
	{
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM g)
		{
			g.Apply();
		});
	}
}
