using System.Drawing;
using System.Reflection;
using IronKernel.Userland.Morphic.Layout;

namespace IronKernel.Userland.Morphic.Inspector;

public sealed class InspectorMorph : WindowMorph
{
	#region Fields

	private readonly PropertyListMorph _propertyList;
	private readonly DockPanelMorph _layoutPanel;

	#endregion

	#region Constructors

	public InspectorMorph(object target)
		: base(Point.Empty, new Size(256, 192), target.GetType().Name)
	{
		IsSelectable = true;

		_propertyList = BuildPropertyList(target);

		_layoutPanel = new ScrollPaneMorph(_propertyList);

		Content.AddMorph(_layoutPanel);
	}

	#endregion

	#region Methods

	protected override void UpdateLayout()
	{
		_layoutPanel.Size = Content.Size;
		base.UpdateLayout();
	}

	private static PropertyListMorph BuildPropertyList(object target)
	{
		var props = target.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

		return PropertyListMorph.FromProperties(props, target);
	}

	#endregion
}