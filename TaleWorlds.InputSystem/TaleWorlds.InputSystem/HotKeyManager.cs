using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public static class HotKeyManager
{
	public delegate void OnKeybindsChangedEvent();

	private static readonly Dictionary<string, GameKeyContext> _categories = new Dictionary<string, GameKeyContext>();

	private static readonly List<string> _serializeIgnoredCategories = new List<string>();

	private static readonly float _versionOfHotkeys = 5f;

	private static bool _hotkeyEditEnabled = false;

	private static bool _notifyDocumentVersionDifferent = false;

	private static PlatformFilePath _savePath;

	public static event OnKeybindsChangedEvent OnKeybindsChanged;

	public static string GetHotKeyId(string categoryName, string hotKeyId)
	{
		if (_categories.TryGetValue(categoryName, out var value))
		{
			return value.GetHotKeyId(hotKeyId);
		}
		Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 29);
		return "";
	}

	public static string GetHotKeyId(string categoryName, int hotKeyId)
	{
		if (_categories.TryGetValue(categoryName, out var value))
		{
			return value.GetHotKeyId(hotKeyId);
		}
		Debug.FailedAssert("Key category with id \"" + categoryName + "\" doesn't exsist.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "GetHotKeyId", 40);
		return "invalid";
	}

	public static GameKeyContext GetCategory(string categoryName)
	{
		return _categories[categoryName];
	}

	public static Dictionary<string, GameKeyContext>.ValueCollection GetAllCategories()
	{
		return _categories.Values;
	}

	public static void Tick(float dt)
	{
	}

	public static void Initialize(PlatformFilePath savePath, bool isRDownSwappedWithRRight)
	{
		GameKeyContext.SetIsRDownSwappedWithRRight(isRDownSwappedWithRRight);
		_savePath = savePath;
	}

	public static void RegisterInitialContexts(IEnumerable<GameKeyContext> contexts, bool loadKeys)
	{
		_categories.Clear();
		foreach (GameKeyContext context in contexts)
		{
			RegisterContext(context, context.Type == GameKeyContext.GameKeyContextType.AuxiliaryNotSerialized);
		}
		if (loadKeys)
		{
			LoadAsync();
		}
	}

	public static void RegisterContext(GameKeyContext context, bool ignoreSerialize = false, bool loadKeys = false)
	{
		if (!_categories.ContainsKey(context.GameKeyCategoryId))
		{
			_categories.Add(context.GameKeyCategoryId, context);
		}
		if (ignoreSerialize && !_serializeIgnoredCategories.Contains(context.GameKeyCategoryId))
		{
			_serializeIgnoredCategories.Add(context.GameKeyCategoryId);
		}
		if (loadKeys)
		{
			LoadAsync();
		}
	}

	public static bool ShouldNotifyDocumentVersionDifferent()
	{
		bool notifyDocumentVersionDifferent = _notifyDocumentVersionDifferent;
		_notifyDocumentVersionDifferent = false;
		return notifyDocumentVersionDifferent;
	}

	public static void Reset()
	{
		foreach (GameKeyContext value in _categories.Values)
		{
			foreach (GameKey registeredGameKey in value.RegisteredGameKeys)
			{
				if (registeredGameKey != null)
				{
					registeredGameKey.ControllerKey?.ChangeKey(registeredGameKey.DefaultControllerKey?.InputKey ?? InputKey.Invalid);
					registeredGameKey.KeyboardKey?.ChangeKey(registeredGameKey.DefaultKeyboardKey?.InputKey ?? InputKey.Invalid);
				}
			}
			foreach (HotKey registeredHotKey in value.RegisteredHotKeys)
			{
				if (registeredHotKey == null)
				{
					continue;
				}
				registeredHotKey.Keys.Clear();
				foreach (Key defaultKey in registeredHotKey.DefaultKeys)
				{
					registeredHotKey.Keys.Add(new Key(defaultKey.InputKey));
				}
			}
			foreach (GameAxisKey registeredGameAxisKey in value.RegisteredGameAxisKeys)
			{
				registeredGameAxisKey.AxisKey.ChangeKey(registeredGameAxisKey.DefaultAxisKey.InputKey);
			}
		}
	}

	public static async void LoadAsync()
	{
		if (!FileHelper.FileExists(_savePath))
		{
			return;
		}
		try
		{
			XmlDocument document = new XmlDocument();
			await document.LoadAsync(_savePath);
			XmlElement documentElement = document.DocumentElement;
			float num = 0f;
			if (documentElement.HasAttribute("hotkeyEditEnabled"))
			{
				_hotkeyEditEnabled = Convert.ToBoolean(documentElement.GetAttribute("hotkeyEditEnabled"));
			}
			if (documentElement.HasAttribute("version") && float.TryParse(documentElement.GetAttribute("version"), out var result))
			{
				num = result;
			}
			if (num != _versionOfHotkeys)
			{
				_notifyDocumentVersionDifferent = true;
				SaveAsync(throwEvent: false);
				return;
			}
			foreach (XmlNode childNode in documentElement.ChildNodes)
			{
				string attribute = ((XmlElement)childNode).GetAttribute("name");
				if (!_categories.TryGetValue(attribute, out var value))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					string name = ((XmlElement)childNode2).Name;
					if (name == "GameKey")
					{
						string innerText = childNode2["Id"].InnerText;
						GameKey gameKey = value.GetGameKey(innerText);
						if (gameKey == null)
						{
							continue;
						}
						XmlElement xmlElement2 = childNode2["Keys"]["KeyboardKey"];
						if (xmlElement2 != null)
						{
							if (Enum.TryParse<InputKey>(xmlElement2.InnerText, out var result2))
							{
								if (gameKey.KeyboardKey != null)
								{
									gameKey.KeyboardKey.ChangeKey(result2);
								}
								else
								{
									gameKey.KeyboardKey = new Key(result2);
								}
							}
						}
						else if (gameKey.DefaultKeyboardKey != null && gameKey.DefaultKeyboardKey.InputKey != InputKey.Invalid)
						{
							gameKey.KeyboardKey = new Key(gameKey.DefaultKeyboardKey.InputKey);
						}
						else
						{
							gameKey.KeyboardKey = new Key(InputKey.Invalid);
						}
					}
					else
					{
						if (!_hotkeyEditEnabled && value.Type != GameKeyContext.GameKeyContextType.AuxiliarySerializedAndShownInOptions)
						{
							continue;
						}
						if (name == "GameAxisKey")
						{
							string innerText2 = childNode2["Id"].InnerText;
							GameAxisKey gameAxisKey = value.GetGameAxisKey(innerText2);
							if (gameAxisKey == null)
							{
								continue;
							}
							XmlElement xmlElement3 = childNode2["Keys"];
							if (!gameAxisKey.IsBinded)
							{
								XmlElement xmlElement4 = xmlElement3["PositiveKey"];
								if (xmlElement4 != null)
								{
									if (xmlElement4.InnerText != "None")
									{
										if (Enum.TryParse<InputKey>(xmlElement4.InnerText, out var result3))
										{
											gameAxisKey.PositiveKey = new GameKey(-1, gameAxisKey.Id + "_p", attribute, result3);
										}
									}
									else
									{
										gameAxisKey.PositiveKey = null;
									}
								}
								XmlElement xmlElement5 = xmlElement3["NegativeKey"];
								if (xmlElement5 != null)
								{
									if (xmlElement5.InnerText != "None")
									{
										if (Enum.TryParse<InputKey>(xmlElement5.InnerText, out var result4))
										{
											gameAxisKey.NegativeKey = new GameKey(-1, gameAxisKey.Id + "_n", attribute, result4);
										}
									}
									else
									{
										gameAxisKey.NegativeKey = null;
									}
								}
							}
							XmlElement xmlElement6 = xmlElement3["AxisKey"];
							if (xmlElement6 == null)
							{
								continue;
							}
							if (xmlElement6.InnerText != "None")
							{
								if (Enum.TryParse<InputKey>(xmlElement6.InnerText, out var result5))
								{
									gameAxisKey.AxisKey = new Key(result5);
								}
							}
							else
							{
								gameAxisKey.AxisKey = null;
							}
						}
						else
						{
							if (!(name == "HotKey"))
							{
								continue;
							}
							string innerText3 = childNode2["Id"].InnerText;
							HotKey hotKey = value.GetHotKey(innerText3);
							if (hotKey == null)
							{
								continue;
							}
							new List<HotKey>();
							XmlElement xmlElement7 = childNode2["Keys"];
							hotKey.Keys = new List<Key>();
							for (int i = 0; i < xmlElement7.ChildNodes.Count; i++)
							{
								if (Enum.TryParse<InputKey>(xmlElement7.ChildNodes[i].InnerText, out var result6))
								{
									hotKey.Keys.Add(new Key(result6));
								}
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Couldn't load key bindings: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "LoadAsync", 354);
		}
	}

	public static async void SaveAsync(bool throwEvent)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			XmlDeclaration newChild = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
			XmlElement documentElement = xmlDocument.DocumentElement;
			xmlDocument.InsertBefore(newChild, documentElement);
			XmlComment newChild2 = xmlDocument.CreateComment("To override values other than GameKeys, change hotkeyEditEnabled to True.");
			xmlDocument.InsertBefore(newChild2, documentElement);
			XmlElement xmlElement = xmlDocument.CreateElement("HotKeyCategories");
			xmlElement.SetAttribute("hotkeyEditEnabled", _hotkeyEditEnabled.ToString());
			float versionOfHotkeys = _versionOfHotkeys;
			xmlElement.SetAttribute("version", versionOfHotkeys.ToString());
			xmlDocument.AppendChild(xmlElement);
			foreach (KeyValuePair<string, GameKeyContext> category in _categories)
			{
				if (_serializeIgnoredCategories.Contains(category.Key))
				{
					continue;
				}
				XmlElement xmlElement2 = xmlDocument.CreateElement("HotKeyCategory");
				xmlElement.AppendChild(xmlElement2);
				xmlElement2.SetAttribute("name", category.Key);
				foreach (GameKey registeredGameKey in category.Value.RegisteredGameKeys)
				{
					if (registeredGameKey != null)
					{
						XmlElement xmlElement3 = xmlDocument.CreateElement("GameKey");
						xmlElement2.AppendChild(xmlElement3);
						XmlElement xmlElement4 = xmlDocument.CreateElement("Id");
						xmlElement3.AppendChild(xmlElement4);
						xmlElement4.InnerText = registeredGameKey.StringId;
						XmlElement xmlElement5 = xmlDocument.CreateElement("Keys");
						xmlElement3.AppendChild(xmlElement5);
						XmlElement xmlElement6 = xmlDocument.CreateElement("KeyboardKey");
						xmlElement5.AppendChild(xmlElement6);
						xmlElement6.InnerText = ((registeredGameKey.KeyboardKey != null) ? registeredGameKey.KeyboardKey.InputKey.ToString() : "None");
						XmlElement xmlElement7 = xmlDocument.CreateElement("ControllerKey");
						xmlElement5.AppendChild(xmlElement7);
						xmlElement7.InnerText = ((registeredGameKey.ControllerKey != null) ? registeredGameKey.ControllerKey.InputKey.ToString() : "None");
					}
				}
				foreach (GameAxisKey registeredGameAxisKey in category.Value.RegisteredGameAxisKeys)
				{
					XmlElement xmlElement8 = xmlDocument.CreateElement("GameAxisKey");
					xmlElement2.AppendChild(xmlElement8);
					XmlElement xmlElement9 = xmlDocument.CreateElement("Id");
					xmlElement8.AppendChild(xmlElement9);
					xmlElement9.InnerText = registeredGameAxisKey.Id;
					XmlElement xmlElement10 = xmlDocument.CreateElement("Keys");
					xmlElement8.AppendChild(xmlElement10);
					XmlElement xmlElement11 = xmlDocument.CreateElement("PositiveKey");
					xmlElement10.AppendChild(xmlElement11);
					xmlElement11.InnerText = ((registeredGameAxisKey.PositiveKey != null) ? registeredGameAxisKey.PositiveKey.KeyboardKey.InputKey.ToString() : "None");
					XmlElement xmlElement12 = xmlDocument.CreateElement("NegativeKey");
					xmlElement10.AppendChild(xmlElement12);
					xmlElement12.InnerText = ((registeredGameAxisKey.NegativeKey != null) ? registeredGameAxisKey.NegativeKey.KeyboardKey.InputKey.ToString() : "None");
					XmlElement xmlElement13 = xmlDocument.CreateElement("AxisKey");
					xmlElement10.AppendChild(xmlElement13);
					xmlElement13.InnerText = ((registeredGameAxisKey.AxisKey != null) ? registeredGameAxisKey.AxisKey.InputKey.ToString() : "None");
				}
				foreach (HotKey registeredHotKey in category.Value.RegisteredHotKeys)
				{
					XmlElement xmlElement14 = xmlDocument.CreateElement("HotKey");
					xmlElement2.AppendChild(xmlElement14);
					XmlElement xmlElement15 = xmlDocument.CreateElement("Id");
					xmlElement14.AppendChild(xmlElement15);
					xmlElement15.InnerText = registeredHotKey.Id;
					XmlElement xmlElement16 = xmlDocument.CreateElement("Keys");
					xmlElement14.AppendChild(xmlElement16);
					foreach (Key key in registeredHotKey.Keys)
					{
						XmlElement xmlElement17 = xmlDocument.CreateElement("Key");
						xmlElement16.AppendChild(xmlElement17);
						xmlElement17.InnerText = key.InputKey.ToString();
					}
				}
			}
			await xmlDocument.SaveAsync(_savePath);
			if (throwEvent)
			{
				HotKeyManager.OnKeybindsChanged?.Invoke();
			}
		}
		catch
		{
			Debug.FailedAssert("Couldn't save key bindings.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\HotkeyManager.cs", "SaveAsync", 466);
		}
	}
}
