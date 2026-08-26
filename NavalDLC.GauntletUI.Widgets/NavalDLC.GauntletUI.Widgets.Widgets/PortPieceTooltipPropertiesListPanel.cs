using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortPieceTooltipPropertiesListPanel : ListPanel
{
	private bool _isDirty = true;

	public PortPieceTooltipPropertiesListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.ChildCount == 0 || !_isDirty)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget widget = base.Children[i].Children[0];
			Widget widget2 = base.Children[i].Children[1];
			float num3 = widget.Size.X + widget.ScaledMarginLeft + widget.ScaledMarginRight;
			float num4 = widget2.Size.X + widget2.ScaledMarginLeft + widget2.ScaledMarginRight;
			if (num < num3)
			{
				num = num3;
			}
			if (num2 < num4)
			{
				num2 = num4;
			}
		}
		float num5 = 0.5f;
		if (num2 > 0f || num > 0f)
		{
			num5 = num2 / (num2 + num);
		}
		for (int j = 0; j < base.ChildCount; j++)
		{
			Widget widget3 = base.Children[j].Children[0];
			Widget widget4 = base.Children[j].Children[1];
			widget3.WidthSizePolicy = SizePolicy.StretchToParent;
			widget4.WidthSizePolicy = SizePolicy.Fixed;
			widget4.ScaledSuggestedWidth = base.Size.X * num5;
			widget4.MinWidth = base.Size.X * 1f / 6f * base._inverseScaleToUse;
			widget4.MaxWidth = base.Size.X * 2f / 3f * base._inverseScaleToUse;
			if (widget4.IsHidden)
			{
				(widget3 as TextWidget).Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
				(widget3 as TextWidget).Brush.TextColorFactor = 0.9f;
			}
		}
		_isDirty = false;
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		_isDirty = true;
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		_isDirty = true;
	}
}
