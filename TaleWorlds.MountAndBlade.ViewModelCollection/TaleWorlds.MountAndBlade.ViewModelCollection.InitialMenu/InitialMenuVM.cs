using System.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;

public class InitialMenuVM : ViewModel
{
	private string _dlcStorePageLink;

	private MBBindingList<InitialMenuOptionVM> _menuOptions;

	private bool _isProfileSelectionEnabled;

	private bool _isDownloadingContent;

	private bool _isNavalDLCEnabled;

	private string _selectProfileText;

	private string _profileName;

	private string _downloadingText;

	private bool _isUpsellButtonVisible;

	private bool _isUpsellButtonActive;

	private string _currentLanguageString;

	[DataSourceProperty]
	public MBBindingList<InitialMenuOptionVM> MenuOptions
	{
		get
		{
			return _menuOptions;
		}
		set
		{
			if (value != _menuOptions)
			{
				_menuOptions = value;
				OnPropertyChangedWithValue(value, "MenuOptions");
			}
		}
	}

	[DataSourceProperty]
	public string DownloadingText
	{
		get
		{
			return _downloadingText;
		}
		set
		{
			if (value != _downloadingText)
			{
				_downloadingText = value;
				OnPropertyChangedWithValue(value, "DownloadingText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectProfileText
	{
		get
		{
			return _selectProfileText;
		}
		set
		{
			if (value != _selectProfileText)
			{
				_selectProfileText = value;
				OnPropertyChangedWithValue(value, "SelectProfileText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfileName
	{
		get
		{
			return _profileName;
		}
		set
		{
			if (value != _profileName)
			{
				_profileName = value;
				OnPropertyChangedWithValue(value, "ProfileName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsProfileSelectionEnabled
	{
		get
		{
			return _isProfileSelectionEnabled;
		}
		set
		{
			if (value != _isProfileSelectionEnabled)
			{
				_isProfileSelectionEnabled = value;
				OnPropertyChangedWithValue(value, "IsProfileSelectionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDownloadingContent
	{
		get
		{
			return _isDownloadingContent;
		}
		set
		{
			if (value != _isDownloadingContent)
			{
				_isDownloadingContent = value;
				OnPropertyChangedWithValue(value, "IsDownloadingContent");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNavalDLCEnabled
	{
		get
		{
			return _isNavalDLCEnabled;
		}
		set
		{
			if (value != _isNavalDLCEnabled)
			{
				_isNavalDLCEnabled = value;
				OnPropertyChangedWithValue(value, "IsNavalDLCEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpsellButtonVisible
	{
		get
		{
			return _isUpsellButtonVisible;
		}
		set
		{
			if (value != _isUpsellButtonVisible)
			{
				_isUpsellButtonVisible = value;
				OnPropertyChangedWithValue(value, "IsUpsellButtonVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUpsellButtonActive
	{
		get
		{
			return _isUpsellButtonActive;
		}
		set
		{
			if (value != _isUpsellButtonActive)
			{
				_isUpsellButtonActive = value;
				OnPropertyChangedWithValue(value, "IsUpsellButtonActive");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentLanguageString
	{
		get
		{
			return _currentLanguageString;
		}
		set
		{
			if (value != _currentLanguageString)
			{
				_currentLanguageString = value;
				OnPropertyChangedWithValue(value, "CurrentLanguageString");
			}
		}
	}

	public InitialMenuVM(InitialState initialState)
	{
		MenuOptions = new MBBindingList<InitialMenuOptionVM>();
		RefreshUpsellButtonState();
		if (HotKeyManager.ShouldNotifyDocumentVersionDifferent())
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=0Itt3bZM}Current keybind document version is outdated. Keybinds have been reverted to defaults."));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		MenuOptions?.ApplyActionOnAllItems(delegate(InitialMenuOptionVM o)
		{
			o.RefreshValues();
		});
		SelectProfileText = new TextObject("{=wubDWOlh}Select Profile").ToString();
		DownloadingText = new TextObject("{=i4Oo6aoM}Downloading Content...").ToString();
		CurrentLanguageString = BannerlordConfig.Language;
	}

	public void RefreshMenuOptions()
	{
		MenuOptions.ApplyActionOnAllItems(delegate(InitialMenuOptionVM x)
		{
			x.OnFinalize();
		});
		MenuOptions.Clear();
		_ = GameStateManager.Current.ActiveState;
		foreach (InitialStateOption initialStateOption in Module.CurrentModule.GetInitialStateOptions())
		{
			MenuOptions.Add(new InitialMenuOptionVM(initialStateOption));
		}
		IsDownloadingContent = Utilities.IsOnlyCoreContentEnabled();
		IsNavalDLCEnabled = ModuleHelper.IsModuleActive("NavalDLC");
		RefreshUpsellButtonState();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		MenuOptions.ApplyActionOnAllItems(delegate(InitialMenuOptionVM x)
		{
			x.OnFinalize();
		});
		MenuOptions.Clear();
	}

	private void RefreshUpsellButtonState()
	{
		RefreshUpsellButtonIsVisible();
		RefreshUpsellButtonLink();
		IsUpsellButtonActive = !string.IsNullOrEmpty(_dlcStorePageLink);
	}

	private void RefreshUpsellButtonIsVisible()
	{
		if (IsNavalDLCEnabled)
		{
			IsUpsellButtonVisible = false;
			return;
		}
		Platform currentPlatform = ApplicationPlatform.CurrentPlatform;
		if ((uint)currentPlatform <= 1u || (uint)(currentPlatform - 7) <= 1u)
		{
			IsUpsellButtonVisible = true;
		}
		else
		{
			IsUpsellButtonVisible = false;
		}
	}

	private void RefreshUpsellButtonLink()
	{
		if (!IsUpsellButtonVisible)
		{
			_dlcStorePageLink = null;
			return;
		}
		switch (ApplicationPlatform.CurrentPlatform)
		{
		case Platform.WindowsSteam:
			_dlcStorePageLink = "https://store.steampowered.com/app/2927200/Mount__Blade_II_Bannerlord__War_Sails/";
			break;
		case Platform.WindowsGOG:
			_dlcStorePageLink = "https://www.gog.com/en/game/mount_blade_ii_bannerlord_war_sails";
			break;
		case Platform.WindowsEpic:
			_dlcStorePageLink = "https://store.epicgames.com/en-US/p/mount-and-blade-2-mount-and-blade-ii-bannerlord-war-sails-597919";
			break;
		case Platform.GDKDesktop:
			_dlcStorePageLink = "https://www.xbox.com/games/store/mount-blade-ii-bannerlord-war-sails/9n205dn89073";
			break;
		default:
			_dlcStorePageLink = null;
			break;
		}
	}

	public void ExecuteNavigateToDLCStorePage()
	{
		if (IsUpsellButtonActive && !string.IsNullOrEmpty(_dlcStorePageLink) && !PlatformServices.Instance.ShowOverlayForWebPage(_dlcStorePageLink).Result)
		{
			Process.Start(new ProcessStartInfo(_dlcStorePageLink)
			{
				UseShellExecute = true
			});
		}
	}
}
