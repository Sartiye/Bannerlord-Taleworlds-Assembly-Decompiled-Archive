using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout;

public class StackLayout : ILayout
{
	private const int DragHoverAperture = 20;

	private readonly Dictionary<int, LayoutBox> _layoutBoxes;

	private Widget _parallelMeasureBasicChildWidget;

	private Vector2 _parallelMeasureBasicChildMeasureSpec;

	private AlignmentAxis _parallelMeasureBasicChildAlignmentAxis;

	private TWParallel.ParallelForAuxPredicate _parallelMeasureBasicChildDelegate;

	public ContainerItemDescription DefaultItemDescription { get; private set; }

	public LayoutMethod LayoutMethod { get; set; }

	public StackLayout()
	{
		DefaultItemDescription = new ContainerItemDescription();
		_layoutBoxes = new Dictionary<int, LayoutBox>(64);
		_parallelMeasureBasicChildDelegate = ParallelMeasureBasicChild;
	}

	public ContainerItemDescription GetItemDescription(Widget owner, Widget child, int childIndex)
	{
		if (owner is Container container)
		{
			return container.GetItemDescription(child.Id, childIndex);
		}
		return DefaultItemDescription;
	}

	public Vector2 MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
	{
		Container container = widget as Container;
		Vector2 result = default(Vector2);
		if (widget.ChildCount > 0)
		{
			if (LayoutMethod == LayoutMethod.HorizontalLeftToRight || LayoutMethod == LayoutMethod.HorizontalRightToLeft || LayoutMethod == LayoutMethod.HorizontalCentered || LayoutMethod == LayoutMethod.HorizontalSpaced)
			{
				result = MeasureLinear(widget, measureSpec, AlignmentAxis.Horizontal);
				if (container != null && container.IsDragHovering)
				{
					result.X += 20f;
				}
			}
			else if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered)
			{
				result = MeasureLinear(widget, measureSpec, AlignmentAxis.Vertical);
				if (container != null && container.IsDragHovering)
				{
					result.Y += 20f;
				}
			}
		}
		return result;
	}

	public void OnLayout(Widget widget, float left, float bottom, float right, float top)
	{
		if (LayoutMethod == LayoutMethod.HorizontalLeftToRight || LayoutMethod == LayoutMethod.HorizontalRightToLeft || LayoutMethod == LayoutMethod.HorizontalCentered || LayoutMethod == LayoutMethod.HorizontalSpaced)
		{
			LayoutLinearHorizontalLocal(widget, left, bottom, right, top);
		}
		else if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered)
		{
			LayoutLinearVertical(widget, left, bottom, right, top);
		}
	}

	private static float GetData(Vector2 vector2, int row)
	{
		if (row == 0)
		{
			return vector2.X;
		}
		return vector2.Y;
	}

	private static void SetData(ref Vector2 vector2, int row, float data)
	{
		if (row == 0)
		{
			vector2.X = data;
		}
		vector2.Y = data;
	}

	public int GetIndexForDrop(Container widget, Vector2 draggedWidgetPosition)
	{
		int row = 0;
		if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered)
		{
			row = 1;
		}
		bool flag = LayoutMethod == LayoutMethod.HorizontalRightToLeft || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered;
		float data = GetData(draggedWidgetPosition, row);
		int result = 0;
		bool flag2 = false;
		for (int i = 0; i != widget.ChildCount; i++)
		{
			if (flag2)
			{
				break;
			}
			Widget child = widget.GetChild(i);
			if (child == null)
			{
				continue;
			}
			float data2 = GetData(child.GlobalPosition * child.Context.CustomScale, row);
			float num = data2 + GetData(child.Size, row);
			float num2 = (data2 + num) / 2f;
			if (!flag)
			{
				if (data < num2)
				{
					result = i;
					flag2 = true;
				}
			}
			else if (data > num2)
			{
				result = i;
				flag2 = true;
			}
		}
		if (!flag2)
		{
			result = widget.ChildCount;
		}
		return result;
	}

	private void ParallelMeasureBasicChild(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			Widget child = _parallelMeasureBasicChildWidget.GetChild(i);
			if (child == null)
			{
				Debug.FailedAssert("Trying to measure a null child for parent" + _parallelMeasureBasicChildWidget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "ParallelMeasureBasicChild", 184);
			}
			else
			{
				if (!child.IsVisible)
				{
					continue;
				}
				switch (_parallelMeasureBasicChildAlignmentAxis)
				{
				case AlignmentAxis.Horizontal:
					if (child.WidthSizePolicy != SizePolicy.StretchToParent)
					{
						child.Measure(_parallelMeasureBasicChildMeasureSpec);
					}
					break;
				case AlignmentAxis.Vertical:
					if (child.HeightSizePolicy != SizePolicy.StretchToParent)
					{
						child.Measure(_parallelMeasureBasicChildMeasureSpec);
					}
					break;
				}
			}
		}
	}

	private Vector2 MeasureLinear(Widget widget, Vector2 measureSpec, AlignmentAxis alignmentAxis)
	{
		_parallelMeasureBasicChildWidget = widget;
		_parallelMeasureBasicChildMeasureSpec = measureSpec;
		_parallelMeasureBasicChildAlignmentAxis = alignmentAxis;
		TWParallel.For(0, widget.ChildCount, _parallelMeasureBasicChildDelegate, 64);
		_parallelMeasureBasicChildWidget = null;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < widget.ChildCount; i++)
		{
			Widget child = widget.GetChild(i);
			if (child == null)
			{
				Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "MeasureLinear", 234);
			}
			else
			{
				if (!child.IsVisible)
				{
					continue;
				}
				ContainerItemDescription itemDescription = GetItemDescription(widget, child, i);
				switch (alignmentAxis)
				{
				case AlignmentAxis.Horizontal:
					if (child.WidthSizePolicy == SizePolicy.StretchToParent)
					{
						num4++;
						num3 += itemDescription.WidthStretchRatio;
					}
					else
					{
						num2 += child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight;
					}
					num = MathF.Max(num, child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom);
					break;
				case AlignmentAxis.Vertical:
					if (child.HeightSizePolicy == SizePolicy.StretchToParent)
					{
						num4++;
						num3 += itemDescription.HeightStretchRatio;
					}
					else
					{
						num += child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom;
					}
					num2 = MathF.Max(num2, child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight);
					break;
				}
			}
		}
		if (num4 > 0)
		{
			float num5 = 0f;
			switch (alignmentAxis)
			{
			case AlignmentAxis.Horizontal:
				num5 = measureSpec.X - num2;
				break;
			case AlignmentAxis.Vertical:
				num5 = measureSpec.Y - num;
				break;
			}
			float num6 = num5;
			int num7 = num4;
			for (int j = 0; j < widget.ChildCount; j++)
			{
				Widget child2 = widget.GetChild(j);
				if (child2 == null)
				{
					Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "MeasureLinear", 296);
				}
				else
				{
					if (!child2.IsVisible || ((alignmentAxis != 0 || child2.WidthSizePolicy != SizePolicy.StretchToParent) && (alignmentAxis != AlignmentAxis.Vertical || child2.HeightSizePolicy != SizePolicy.StretchToParent)))
					{
						continue;
					}
					ContainerItemDescription itemDescription2 = GetItemDescription(widget, child2, j);
					Vector2 measureSpec2 = new Vector2(0f, 0f);
					if (num6 <= 0f)
					{
						switch (alignmentAxis)
						{
						case AlignmentAxis.Horizontal:
							measureSpec2 = new Vector2(0f, measureSpec.Y);
							break;
						case AlignmentAxis.Vertical:
							measureSpec2 = new Vector2(measureSpec.X, 0f);
							break;
						}
					}
					else
					{
						switch (alignmentAxis)
						{
						case AlignmentAxis.Horizontal:
						{
							float x = num5 * itemDescription2.WidthStretchRatio / num3;
							if (num7 == 1)
							{
								x = num6;
							}
							measureSpec2 = new Vector2(x, measureSpec.Y);
							break;
						}
						case AlignmentAxis.Vertical:
						{
							float y = num5 * itemDescription2.HeightStretchRatio / num3;
							if (num7 == 1)
							{
								y = num6;
							}
							measureSpec2 = new Vector2(measureSpec.X, y);
							break;
						}
						}
					}
					child2.Measure(measureSpec2);
					num7--;
					Vector2 measuredSize = child2.MeasuredSize;
					measuredSize.X += child2.ScaledMarginLeft + child2.ScaledMarginRight;
					measuredSize.Y += child2.ScaledMarginTop + child2.ScaledMarginBottom;
					switch (alignmentAxis)
					{
					case AlignmentAxis.Horizontal:
						num6 -= measuredSize.X;
						num2 += measuredSize.X;
						num = MathF.Max(num, measuredSize.Y);
						break;
					case AlignmentAxis.Vertical:
						num6 -= measuredSize.Y;
						num += measuredSize.Y;
						num2 = MathF.Max(num2, measuredSize.X);
						break;
					}
				}
			}
		}
		float x2 = num2;
		float y2 = num;
		return new Vector2(x2, y2);
	}

	private void ParallelUpdateLayouts(Widget widget)
	{
		TWParallel.For(0, widget.ChildCount, UpdateChildLayoutMT);
		void UpdateChildLayoutMT(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				Widget child = widget.GetChild(i);
				if (child != null && child.IsVisible)
				{
					LayoutBox layoutBox = _layoutBoxes[i];
					child.Layout(layoutBox.Left, layoutBox.Bottom, layoutBox.Right, layoutBox.Top);
				}
			}
		}
	}

	private void LayoutLinearHorizontalLocal(Widget widget, float left, float bottom, float right, float top)
	{
		Container container = widget as Container;
		float num = 0f;
		float top2 = 0f;
		float num2 = right - left;
		float bottom2 = bottom - top;
		if (LayoutMethod != LayoutMethod.HorizontalRightToLeft && LayoutMethod == LayoutMethod.HorizontalCentered)
		{
			float num3 = 0f;
			for (int i = 0; i < widget.ChildCount; i++)
			{
				Widget child = widget.GetChild(i);
				if (child == null)
				{
					Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "LayoutLinearHorizontalLocal", 422);
				}
				else if (child.IsVisible)
				{
					num3 += child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight;
				}
			}
			num = (right - left) / 2f - num3 / 2f;
		}
		_layoutBoxes.Clear();
		int num4 = 0;
		for (int j = 0; j < widget.ChildCount; j++)
		{
			if (widget.Children[j].IsVisible)
			{
				num4++;
			}
		}
		if (num4 > 0)
		{
			for (int k = 0; k < widget.ChildCount; k++)
			{
				Widget widget2 = widget.Children[k];
				if (widget2 == null)
				{
					Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "LayoutLinearHorizontalLocal", 453);
				}
				else if (widget2.IsVisible)
				{
					float num5 = widget2.MeasuredSize.X + widget2.ScaledMarginLeft + widget2.ScaledMarginRight;
					if (container != null && container.IsDragHovering && k == container.DragHoverInsertionIndex)
					{
						num5 += 20f;
					}
					if (LayoutMethod == LayoutMethod.HorizontalRightToLeft)
					{
						num = num2 - num5;
					}
					else if (LayoutMethod == LayoutMethod.HorizontalSpaced)
					{
						if (num4 > 1)
						{
							if (k == 0)
							{
								num = 0f;
								num2 = left + widget2.MeasuredSize.X;
							}
							else if (k == widget.ChildCount - 1)
							{
								num2 = right - left;
								num = num2 - widget2.MeasuredSize.X;
							}
							else
							{
								float num6 = (widget.MeasuredSize.X - widget2.MeasuredSize.X * (float)num4) / (float)(num4 - 1);
								num += widget2.MeasuredSize.X + num6;
								num2 = num + widget2.MeasuredSize.X;
							}
						}
						else
						{
							num = widget.MeasuredSize.X / 2f - widget2.MeasuredSize.X / 2f;
							num2 = num + widget2.MeasuredSize.X / 2f;
						}
					}
					else
					{
						num2 = num + num5;
					}
					if (widget.ChildCount < 64)
					{
						widget2.Layout(num, bottom2, num2, top2);
					}
					else
					{
						LayoutBox value = default(LayoutBox);
						value.Left = num;
						value.Right = num2;
						value.Bottom = bottom2;
						value.Top = top2;
						_layoutBoxes.Add(k, value);
					}
					if (LayoutMethod == LayoutMethod.HorizontalRightToLeft)
					{
						num2 = num;
					}
					else if (LayoutMethod == LayoutMethod.HorizontalLeftToRight || LayoutMethod == LayoutMethod.HorizontalCentered)
					{
						num = num2;
					}
				}
				else
				{
					_layoutBoxes.Add(k, default(LayoutBox));
				}
			}
		}
		if (widget.ChildCount >= 64)
		{
			ParallelUpdateLayouts(widget);
		}
	}

	private void LayoutLinearVertical(Widget widget, float left, float bottom, float right, float top)
	{
		Container container = widget as Container;
		float left2 = 0f;
		float num = 0f;
		float num2 = bottom - top;
		float right2 = right - left;
		if (LayoutMethod != LayoutMethod.VerticalTopToBottom && LayoutMethod == LayoutMethod.VerticalCentered)
		{
			float num3 = 0f;
			for (int i = 0; i < widget.ChildCount; i++)
			{
				Widget child = widget.GetChild(i);
				if (child != null && child.IsVisible)
				{
					num3 += child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom;
				}
			}
			float num4 = (bottom - top) * 0.5f;
			float num5 = num3 * 0.5f;
			num2 = num4 + num5;
			num = num4 - num5;
		}
		_layoutBoxes.Clear();
		for (int j = 0; j < widget.ChildCount; j++)
		{
			Widget child2 = widget.GetChild(j);
			if (child2 != null && child2.IsVisible)
			{
				if (container != null && container.IsDragHovering && j == container.DragHoverInsertionIndex)
				{
					if (LayoutMethod == LayoutMethod.VerticalBottomToTop)
					{
						num += 20f;
					}
					else
					{
						num2 -= 20f;
					}
				}
				float num6 = child2.MeasuredSize.Y + child2.ScaledMarginTop + child2.ScaledMarginBottom;
				if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalCentered)
				{
					num2 = num + num6;
				}
				else if (LayoutMethod == LayoutMethod.VerticalTopToBottom)
				{
					num = num2 - num6;
				}
				if (widget.ChildCount < 64)
				{
					child2.Layout(left2, num2, right2, num);
				}
				else
				{
					LayoutBox value = default(LayoutBox);
					value.Left = left2;
					value.Right = right2;
					value.Bottom = num2;
					value.Top = num;
					_layoutBoxes.Add(j, value);
				}
				if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalCentered)
				{
					num = num2;
				}
				else
				{
					num2 = num;
				}
			}
			else
			{
				_layoutBoxes.Add(j, default(LayoutBox));
			}
		}
		if (widget.ChildCount >= 64)
		{
			ParallelUpdateLayouts(widget);
		}
	}

	public Vector2 GetDropGizmoPosition(Container widget, Vector2 draggedWidgetPosition)
	{
		int row = 0;
		if (LayoutMethod == LayoutMethod.VerticalBottomToTop || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered)
		{
			row = 1;
		}
		bool num = LayoutMethod == LayoutMethod.HorizontalRightToLeft || LayoutMethod == LayoutMethod.VerticalTopToBottom || LayoutMethod == LayoutMethod.VerticalCentered;
		int indexForDrop = GetIndexForDrop(widget, draggedWidgetPosition);
		int num2 = indexForDrop - 1;
		Vector2 vector = widget.GlobalPosition;
		Vector2 vector2 = widget.GlobalPosition;
		if (!num)
		{
			if (num2 >= 0 && num2 < widget.ChildCount)
			{
				Widget child = widget.GetChild(num2);
				SetData(ref vector, row, GetData(child.GlobalPosition, row) + GetData(child.Size, row));
			}
			if (indexForDrop >= 0 && indexForDrop < widget.ChildCount)
			{
				SetData(ref vector2, row, GetData(widget.GetChild(indexForDrop).GlobalPosition, row));
			}
			else if (indexForDrop >= widget.ChildCount && widget.ChildCount > 0)
			{
				SetData(ref vector2, row, GetData(vector, row) + 20f);
			}
		}
		else
		{
			SetData(ref vector, row, GetData(vector, row) + GetData(widget.Size, row));
			SetData(ref vector2, row, GetData(vector2, row) + GetData(widget.Size, row));
			if (num2 >= 0 && num2 < widget.ChildCount)
			{
				Widget child2 = widget.GetChild(num2);
				SetData(ref vector, row, GetData(child2.GlobalPosition, row));
			}
			if (indexForDrop >= 0 && indexForDrop < widget.ChildCount)
			{
				Widget child3 = widget.GetChild(indexForDrop);
				SetData(ref vector2, row, GetData(child3.GlobalPosition, row) + GetData(child3.Size, row));
			}
			else if (indexForDrop >= widget.ChildCount && widget.ChildCount > 0)
			{
				SetData(ref vector2, row, GetData(vector, row) - 20f);
			}
		}
		return new Vector2((vector.X + vector2.X) / 2f, (vector.Y + vector2.Y) / 2f);
	}
}
