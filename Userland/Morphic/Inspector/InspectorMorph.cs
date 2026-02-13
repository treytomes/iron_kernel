using System.Drawing;
using System.Reflection;
using Userland.Morphic.Layout;
using Userland.Morphic.Commands;

namespace Userland.Morphic.Inspector;

public sealed class InspectorMorph : WindowMorph
{
	#region Fields

	private readonly List<InspectionContext> _path = new();
	private readonly ToolbarMorph _breadcrumbs;
	private readonly ScrollPaneMorph _scrollPane;

	#endregion

	#region Constructor

	public InspectorMorph(object target)
		: base(Point.Empty, new Size(480, 320), target.GetType().Name)
	{
		IsSelectable = true;

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

		var breadcrumbHeight = _breadcrumbs.Visible
			? _breadcrumbs.Size.Height
			: 0;

		_scrollPane.Position = new Point(0, breadcrumbHeight);
		_scrollPane.Size = new Size(
			Content.Size.Width,
			Content.Size.Height - breadcrumbHeight
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

		_path.RemoveRange(index + 1, _path.Count - index - 1);

		var ctx = _path[^1];
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

	#region Property list builder

	private PropertyListMorph BuildPropertyList(object target)
	{
		var props = target.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

		var list = new PropertyListMorph();

		foreach (var prop in props)
		{
			var localProp = prop;
			var localTarget = target;

			object? value = localProp.GetValue(localTarget);

			// --- Struct properties: live commit ---
			if (value != null && localProp.PropertyType.IsValueType && localProp.CanWrite)
			{
				// Single working copy of the struct
				object workingCopy = value;

				// Commit immediately when setter invoked
				void CommitStruct()
				{
					localProp.SetValue(localTarget, workingCopy);
				}

				var factory = new InspectorFactory(
					navigate: obj =>
					{
						NavigateForward(obj, localProp.Name);
					}
				);

				list.AddMorph(new PropertyRowMorph(
					factory,
					localProp.Name,
					() => workingCopy,
					v =>
					{
						workingCopy = v!;
						CommitStruct(); // ✅ immediate persistence
					},
					localProp.PropertyType
				));

				continue;
			}

			// --- Reference / primitive properties ---
			var defaultFactory = new InspectorFactory(
				navigate: obj => NavigateForward(obj, obj.GetType().Name)
			);

			list.AddMorph(new PropertyRowMorph(
				defaultFactory,
				localProp.Name,
				() => localProp.GetValue(localTarget),
				localProp.CanWrite ? v => localProp.SetValue(localTarget, v) : null,
				localProp.PropertyType
			));
		}

		list.RecalculateNameColumnWidth();
		return list;
	}

	#endregion
}