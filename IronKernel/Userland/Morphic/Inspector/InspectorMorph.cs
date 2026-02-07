using System.Drawing;
using System.Reflection;
using IronKernel.Userland.Morphic.Layout;
using IronKernel.Userland.Morphic.Commands;

namespace IronKernel.Userland.Morphic.Inspector;

public sealed class InspectorMorph : WindowMorph
{
	#region Fields

	private readonly List<InspectionContext> _path = new();
	private readonly ToolbarMorph _breadcrumbs;
	private readonly ScrollPaneMorph _scrollPane;

	#endregion

	#region Constructors

	public InspectorMorph(object target)
		: base(Point.Empty, new Size(256, 192), target.GetType().Name)
	{
		IsSelectable = true;

		// Root context
		_path.Add(new InspectionContext(target, target.GetType().Name));

		_breadcrumbs = new ToolbarMorph();
		Content.AddMorph(_breadcrumbs);

		_scrollPane = new ScrollPaneMorph(
			BuildPropertyList(target)
		);
		Content.AddMorph(_scrollPane);

		RebuildBreadcrumbs();
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		_breadcrumbs.Position = Point.Empty;

		_scrollPane.Position = new Point(
			0,
			_breadcrumbs.Visible ? _breadcrumbs.Size.Height : 0
		);

		_scrollPane.Size = new Size(
			Content.Size.Width,
			Content.Size.Height - (_breadcrumbs.Visible ? _breadcrumbs.Size.Height : 0)
		);

		base.UpdateLayout();
	}

	#endregion

	#region Navigation

	private void NavigateForward(object target, string label)
	{
		_path.Add(new InspectionContext(target, label));
		ReplacePropertyList(target);
		RebuildBreadcrumbs();
	}

	private void NavigateBackTo(int index)
	{
		if (index < 0 || index >= _path.Count)
			return;

		// Truncate path
		_path.RemoveRange(index + 1, _path.Count - index - 1);

		var ctx = _path[index];
		ReplacePropertyList(ctx.Target);
		RebuildBreadcrumbs();
	}

	#endregion

	#region UI rebuilding

	private void ReplacePropertyList(object target)
	{
		_scrollPane.SetContent(BuildPropertyList(target));
	}

	private void RebuildBreadcrumbs()
	{
		_breadcrumbs.Clear();

		// Hide breadcrumbs at root
		if (_path.Count <= 1)
		{
			_breadcrumbs.Visible = false;
			return;
		}

		_breadcrumbs.Visible = true;

		for (int i = 0; i < _path.Count; i++)
		{
			int index = i;
			var ctx = _path[i];

			_breadcrumbs.AddItem(
				ctx.Label,
				new ActionCommand(() => NavigateBackTo(index))
			);
		}
	}

	#endregion

	#region Helpers

	private PropertyListMorph BuildPropertyList(object target)
	{
		var inspectorFactory = new InspectorFactory(
			navigate: obj => NavigateForward(
				obj,
				obj.GetType().Name
			)
		);

		var props = target.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

		var list = new PropertyListMorph();

		foreach (var prop in props)
		{
			list.AddMorph(
				new PropertyRowMorph(
					inspectorFactory,
					prop.Name,
					() => prop.GetValue(target),
					prop.CanWrite
						? v => prop.SetValue(target, v)
						: null,
					prop.PropertyType
				)
			);
		}

		list.RecalculateNameColumnWidth();
		return list;
	}

	#endregion
}