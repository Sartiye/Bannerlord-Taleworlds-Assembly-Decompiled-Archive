using System;
using System.Collections.Generic;
using SandBox.AdvancedStartOptions;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class CampaignStartingOptionsVM : ViewModel
{
	private readonly Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> _onConfirm;

	private readonly Action _onClose;

	private readonly SandBox.AdvancedStartOptions.AdvancedStartOptions _stagedOptions;

	private bool _isASOEnabled;

	private bool _isInitialized;

	private MBBindingList<StartingOptionCategoryVM> _categories;

	private string _titleLabel;

	private string _startGameLabel;

	private MBBindingList<StartingOptionTitleDescriptionTupleVM> _relevantOptionTexts;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	public StartingOptionVM FocusedOption { get; private set; }

	[DataSourceProperty]
	public MBBindingList<StartingOptionCategoryVM> Categories
	{
		get
		{
			return _categories;
		}
		set
		{
			if (value != _categories)
			{
				_categories = value;
				OnPropertyChangedWithValue(value, "Categories");
			}
		}
	}

	[DataSourceProperty]
	public string TitleLabel
	{
		get
		{
			return _titleLabel;
		}
		set
		{
			if (value != _titleLabel)
			{
				_titleLabel = value;
				OnPropertyChangedWithValue(value, "TitleLabel");
			}
		}
	}

	[DataSourceProperty]
	public string StartGameLabel
	{
		get
		{
			return _startGameLabel;
		}
		set
		{
			if (value != _startGameLabel)
			{
				_startGameLabel = value;
				OnPropertyChangedWithValue(value, "StartGameLabel");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StartingOptionTitleDescriptionTupleVM> RelevantOptionTexts
	{
		get
		{
			return _relevantOptionTexts;
		}
		set
		{
			if (value != _relevantOptionTexts)
			{
				_relevantOptionTexts = value;
				OnPropertyChangedWithValue(value, "RelevantOptionTexts");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	public CampaignStartingOptionsVM(SandBox.AdvancedStartOptions.AdvancedStartOptions startOptions, Action<SandBox.AdvancedStartOptions.AdvancedStartOptions> onConfirm, Action onClose)
	{
		_isASOEnabled = startOptions.HasAnyChange();
		_onConfirm = onConfirm;
		_onClose = onClose;
		_stagedOptions = startOptions;
		Categories = new MBBindingList<StartingOptionCategoryVM>();
		BuildCategories();
		RelevantOptionTexts = new MBBindingList<StartingOptionTitleDescriptionTupleVM>();
		StartingOptionVM.OnOptionFocusBegin += OnOptionFocusBegin;
		StartingOptionVM.OnOptionFocusEnd += OnOptionFocusEnd;
		StartingOptionVM.OnOptionChanged += OnOptionChanged;
		OnOptionChanged();
		RefreshValues();
		_isInitialized = true;
	}

	private void BuildCategories()
	{
		Dictionary<string, StartingOptionCategoryVM> dictionary = new Dictionary<string, StartingOptionCategoryVM>();
		TextObject categoryName = Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_category_name", "general");
		StartingOptionCategoryVM startingOptionCategoryVM = CreateCategory("general", categoryName);
		startingOptionCategoryVM.Options.Add(new StartingOptionVM("EnableAdvancedStartingOptions", () => _isASOEnabled, delegate(bool value)
		{
			OnASOOptionToggled(value);
		}));
		dictionary.Add("general", startingOptionCategoryVM);
		Categories.Add(startingOptionCategoryVM);
		IReadOnlyList<AdvancedStartOption> allOptions = _stagedOptions.GetAllOptions();
		for (int i = 0; i < allOptions.Count; i++)
		{
			AdvancedStartOption advancedStartOption = allOptions[i];
			string categoryId = advancedStartOption.CategoryId;
			if (string.IsNullOrEmpty(categoryId))
			{
				Debug.FailedAssert("Empty category id", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\CampaignStartingOptions\\CampaignStartingOptionsVM.cs", "BuildCategories", 67);
				continue;
			}
			if (!dictionary.TryGetValue(categoryId, out var value2))
			{
				TextObject categoryName2 = Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_category_name", categoryId);
				value2 = CreateCategory(categoryId, categoryName2);
				dictionary.Add(categoryId, value2);
				Categories.Add(value2);
			}
			StartingOptionVM startingOptionVM = new StartingOptionVM(advancedStartOption, _stagedOptions, () => !_isASOEnabled);
			if (advancedStartOption.StringId == "Seed")
			{
				startingOptionVM.AllowRandomization = true;
				if (!_stagedOptions.HasAnyChange())
				{
					startingOptionVM.ExecuteRandomize();
				}
			}
			value2.Options.Add(startingOptionVM);
		}
	}

	private static StartingOptionCategoryVM CreateCategory(string categoryId, TextObject categoryName)
	{
		if (!(categoryId == "general"))
		{
			if (categoryId == "globalmodifiers")
			{
				return new GlobalModifiersCategoryVM(categoryId, categoryName);
			}
			return new StartingOptionCategoryVM(categoryId, categoryName);
		}
		return new GeneralCategoryVM(categoryId, categoryName);
	}

	private void OnASOOptionToggled(bool newValue)
	{
		if (_isInitialized)
		{
			_isASOEnabled = newValue;
			if (newValue)
			{
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=LW8QCm22}Notice").ToString(), new TextObject("{=7AZYcnV4}Using the Advanced Starting Options can affect game balance, progression, AI behavior, difficulty etc. Recommended for experienced players only.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=DM6luo3c}Continue").ToString(), null, null, null));
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleLabel = new TextObject("{=LuaC2Lz2}Advanced Starting Options").ToString();
		StartGameLabel = new TextObject("{=lBQXP6Wj}Start Game").ToString();
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.RefreshValues();
		});
		RefreshRelevantOptionTexts();
	}

	public void SetRandomizeInputKey(HotKey hotkey)
	{
		for (int i = 0; i < Categories.Count; i++)
		{
			StartingOptionCategoryVM startingOptionCategoryVM = Categories[i];
			for (int j = 0; j < startingOptionCategoryVM.Options.Count; j++)
			{
				StartingOptionVM startingOptionVM = startingOptionCategoryVM.Options[j];
				if (startingOptionVM.AllowRandomization)
				{
					startingOptionVM.SetRandomizeInputKey(hotkey);
				}
			}
		}
	}

	private void OnOptionFocusBegin(StartingOptionVM optionVM)
	{
		if (optionVM != null)
		{
			FocusedOption = optionVM;
		}
	}

	private void OnOptionFocusEnd(StartingOptionVM optionVM)
	{
		if (optionVM != null && FocusedOption == optionVM)
		{
			FocusedOption = null;
		}
	}

	public void ExecuteConfirm()
	{
		_onConfirm?.Invoke(_isASOEnabled ? _stagedOptions : new SandBox.AdvancedStartOptions.AdvancedStartOptions());
	}

	public void ExecuteCancel()
	{
		_onClose?.Invoke();
	}

	private void OnOptionChanged()
	{
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.UpdateOptionStates();
		});
		RefreshRelevantOptionTexts();
	}

	private void RefreshRelevantOptionTexts()
	{
		RelevantOptionTexts.Clear();
		for (int i = 0; i < Categories.Count; i++)
		{
			StartingOptionCategoryVM startingOptionCategoryVM = Categories[i];
			if (!string.IsNullOrEmpty(startingOptionCategoryVM.DescriptionText))
			{
				RelevantOptionTexts.Add(new StartingOptionTitleDescriptionTupleVM(startingOptionCategoryVM.Name, startingOptionCategoryVM.DescriptionText));
			}
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StartingOptionVM.OnOptionFocusBegin -= OnOptionFocusBegin;
		StartingOptionVM.OnOptionFocusEnd -= OnOptionFocusEnd;
		StartingOptionVM.OnOptionChanged -= OnOptionChanged;
		Categories.ApplyActionOnAllItems(delegate(StartingOptionCategoryVM x)
		{
			x.OnFinalize();
		});
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
