using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public abstract class OrderSubjectVM : ViewModel
{
	private int _behaviorType;

	private int _underAttackOfType;

	private bool _isSelectable;

	private bool _isSelected;

	private bool _isSelectionHighlightActive;

	private bool _canToggleSelection;

	private string _shortcutText;

	private InputKeyItemVM _selectionKey;

	private MBBindingList<OrderItemVM> _activeOrders;

	[DataSourceProperty]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChangedWithValue(value, "IsSelectable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if ((!value || IsSelectable) && value != _isSelected)
			{
				_isSelected = value;
				OnSelectionStateChanged(value);
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectionHighlightActive
	{
		get
		{
			return _isSelectionHighlightActive;
		}
		set
		{
			if (value != _isSelectionHighlightActive)
			{
				_isSelectionHighlightActive = value;
				OnPropertyChangedWithValue(value, "IsSelectionHighlightActive");
			}
		}
	}

	[DataSourceProperty]
	public bool CanToggleSelection
	{
		get
		{
			return _canToggleSelection;
		}
		set
		{
			if (value != _canToggleSelection)
			{
				_canToggleSelection = value;
				OnPropertyChangedWithValue(value, "CanToggleSelection");
			}
		}
	}

	[DataSourceProperty]
	public int BehaviorType
	{
		get
		{
			return _behaviorType;
		}
		set
		{
			if (value != _behaviorType)
			{
				_behaviorType = value;
				OnPropertyChangedWithValue(value, "BehaviorType");
			}
		}
	}

	[DataSourceProperty]
	public int UnderAttackOfType
	{
		get
		{
			return _underAttackOfType;
		}
		set
		{
			if (value != _underAttackOfType)
			{
				_underAttackOfType = value;
				OnPropertyChangedWithValue(value, "UnderAttackOfType");
			}
		}
	}

	[DataSourceProperty]
	public string ShortcutText
	{
		get
		{
			return _shortcutText;
		}
		set
		{
			if (value != _shortcutText)
			{
				_shortcutText = value;
				OnPropertyChangedWithValue(value, "ShortcutText");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SelectionKey
	{
		get
		{
			return _selectionKey;
		}
		set
		{
			if (value != _selectionKey)
			{
				_selectionKey = value;
				OnPropertyChangedWithValue(value, "SelectionKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderItemVM> ActiveOrders
	{
		get
		{
			return _activeOrders;
		}
		set
		{
			if (value != _activeOrders)
			{
				_activeOrders = value;
				OnPropertyChangedWithValue(value, "ActiveOrders");
			}
		}
	}

	public OrderSubjectVM()
	{
		ActiveOrders = new MBBindingList<OrderItemVM>();
	}

	public void AddActiveOrder(OrderItemVM order)
	{
		ActiveOrders.Add(order);
	}

	public void RemoveActiveOrder(OrderItemVM order)
	{
		ActiveOrders.Remove(order);
	}

	public void ClearActiveOrders()
	{
		ActiveOrders.Clear();
	}

	protected abstract void OnSelectionStateChanged(bool isSelected);
}
