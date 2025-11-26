using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

internal class WidgetContainer
{
	internal enum ContainerType
	{
		None,
		Update,
		ParallelUpdate,
		LateUpdate,
		VisualDefinition,
		TweenPosition,
		UpdateBrushes
	}

	private int _currentBufferIndex;

	private List<Widget>[] _widgetLists;

	private EmptyWidget _emptyWidget;

	private int _emptyCount;

	private ContainerType _containerType;

	internal int Count => _widgetLists[_currentBufferIndex].Count;

	internal int RealCount => _widgetLists[_currentBufferIndex].Count - _emptyCount;

	internal Widget this[int index]
	{
		get
		{
			return _widgetLists[_currentBufferIndex][index];
		}
		set
		{
			_widgetLists[_currentBufferIndex][index] = value;
		}
	}

	internal WidgetContainer(UIContext context, int initialCapacity, ContainerType type)
	{
		_emptyWidget = new EmptyWidget(context);
		_currentBufferIndex = 0;
		_widgetLists = new List<Widget>[2]
		{
			new List<Widget>(initialCapacity),
			new List<Widget>(initialCapacity)
		};
		_containerType = type;
		_emptyCount = 0;
	}

	internal List<Widget> GetCurrentList()
	{
		return _widgetLists[_currentBufferIndex];
	}

	internal int Add(Widget widget)
	{
		_widgetLists[_currentBufferIndex].Add(widget);
		return _widgetLists[_currentBufferIndex].Count - 1;
	}

	internal void Remove(Widget widget)
	{
		int index = _widgetLists[_currentBufferIndex].IndexOf(widget);
		_widgetLists[_currentBufferIndex][index] = _emptyWidget;
		_emptyCount++;
	}

	public void Clear()
	{
		for (int i = 0; i < _widgetLists.Length; i++)
		{
			_widgetLists[i].Clear();
		}
		_widgetLists = null;
		_emptyCount = 0;
	}

	internal void RemoveFromIndex(int index)
	{
		_widgetLists[_currentBufferIndex][index] = _emptyWidget;
		_emptyCount++;
	}

	internal bool CheckFragmentation()
	{
		int count = _widgetLists[_currentBufferIndex].Count;
		if (count > 32 && (int)((float)count * 0.1f) < _emptyCount)
		{
			return true;
		}
		return false;
	}

	internal void DoDefragmentation()
	{
		int count = _widgetLists[_currentBufferIndex].Count;
		int num = (_currentBufferIndex + 1) % 2;
		List<Widget> list = _widgetLists[_currentBufferIndex];
		List<Widget> list2 = _widgetLists[num];
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			Widget widget = list[i];
			if (widget != _emptyWidget)
			{
				switch (_containerType)
				{
				case ContainerType.Update:
					widget.OnUpdateListIndex = num2;
					break;
				case ContainerType.ParallelUpdate:
					widget.OnParallelUpdateListIndex = num2;
					break;
				case ContainerType.LateUpdate:
					widget.OnLateUpdateListIndex = num2;
					break;
				case ContainerType.VisualDefinition:
					widget.OnVisualDefinitionListIndex = num2;
					break;
				case ContainerType.TweenPosition:
					widget.OnTweenPositionListIndex = num2;
					break;
				case ContainerType.UpdateBrushes:
					widget.OnUpdateBrushesIndex = num2;
					break;
				}
				list2.Add(widget);
				num2++;
			}
		}
		list.Clear();
		_emptyCount = 0;
		_currentBufferIndex = num;
	}
}
