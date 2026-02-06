using System.Drawing;
using System.Reflection;

namespace IronKernel.Userland.Morphic.Inspector;

/// <summary>
/// A vertical container for PropertyRowMorph instances.
/// Used by InspectorMorph to display object properties.
/// </summary>
public sealed class PropertyListMorph : Morph
{
	#region Constants

	private const int Padding = 1;

	#endregion

	#region Fields

	private int _rowSpacing = 1;
	private int _nameColumnWidth = 96;

	#endregion

	#region Constructors

	public PropertyListMorph()
	{
		IsSelectable = false;
		ShouldClipToBounds = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Vertical spacing between rows.
	/// </summary>
	public int RowSpacing
	{
		get => _rowSpacing;
		set
		{
			if (_rowSpacing != value)
			{
				_rowSpacing = value;
				InvalidateLayout();
			}
		}
	}

	/// <summary>
	/// Width of the name column shared by all rows.
	/// </summary>
	public int NameColumnWidth
	{
		get => _nameColumnWidth;
		set
		{
			if (_nameColumnWidth != value)
			{
				_nameColumnWidth = value;
				InvalidateLayout();
			}
		}
	}

	#endregion

	#region Methods

	public static PropertyListMorph FromProperties(IEnumerable<PropertyInfo> properties, object target)
	{
		var list = new PropertyListMorph();

		foreach (var prop in properties)
		{
			list.AddMorph(new PropertyRowMorph(prop, target));
		}

		return list;
	}

	#region Layout

	protected override void UpdateLayout()
	{
		// 1. Measure name labels
		var maxNameWidth = Submorphs.OfType<PropertyRowMorph>().Max(x => x.NameLabelWidth);

		// 2. Apply uniform column width (with padding)
		NameColumnWidth = maxNameWidth;

		// 3. Lay out rows vertically
		int y = Padding;
		int maxWidth = 0;

		foreach (var morph in Submorphs)
		{
			if (morph is not PropertyRowMorph row)
				continue;

			row.Position = new Point(Padding, y);
			row.NameColumnWidth = NameColumnWidth;
			y += row.Size.Height + _rowSpacing;
			maxWidth = Math.Max(maxWidth, row.Size.Width);
		}

		Size = new Size(
			maxWidth + Padding * 2,
			y + Padding);

		base.UpdateLayout();
	}

	#endregion

	#endregion
}