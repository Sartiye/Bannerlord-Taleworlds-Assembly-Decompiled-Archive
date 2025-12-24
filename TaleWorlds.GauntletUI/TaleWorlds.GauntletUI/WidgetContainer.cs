using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

internal class WidgetContainer
{
	internal enum ContainerType
	{
		Update,
		ParallelUpdate,
		LateUpdate,
		VisualDefinition,
		TweenPosition,
		UpdateBrushes
	}

	private HashSet<Widget> _backList;

	private MBList<Widget> _frontList;

	private EmptyWidget _emptyWidget;

	private readonly ContainerType _containerType;

	private bool _isFragmented;

	internal int Count => GetActiveList().Count;

	internal WidgetContainer(UIContext context, int initialCapacity, ContainerType containerType)
	{
		_containerType = containerType;
		_emptyWidget = new EmptyWidget(context);
		_backList = new HashSet<Widget>();
		_frontList = new MBList<Widget>(initialCapacity);
	}

	internal void Add(Widget widget)
	{
		_backList.Add(widget);
		_isFragmented = true;
	}

	internal void Remove(Widget widget)
	{
		_backList.Remove(widget);
		_isFragmented = true;
	}

	public void Clear()
	{
		_backList.Clear();
		_frontList.Clear();
		_backList = null;
		_frontList = null;
		_isFragmented = true;
	}

	public MBReadOnlyList<Widget> GetActiveList()
	{
		return _frontList;
	}

	public void Defrag()
	{
		if (!_isFragmented)
		{
			return;
		}
		_frontList.Clear();
		int num = 0;
		foreach (Widget back in _backList)
		{
			if (back != _emptyWidget)
			{
				_frontList.Add(back);
				num++;
			}
		}
		_backList.Clear();
		for (int i = 0; i < _frontList.Count; i++)
		{
			_backList.Add(_frontList[i]);
		}
		_isFragmented = false;
	}
}
