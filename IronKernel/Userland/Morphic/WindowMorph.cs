using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// A compositional window morph with a header (title + buttons)
/// and a content area. Structural operations (move/resize/delete)
/// are handled by the Halo, not by this class.
/// </summary>
public sealed class WindowMorph : Morph
{
	#region Fields

	private readonly ContainerMorph _header;
	private readonly ContainerMorph _content;
	private readonly LabelMorph _titleLabel;
	private readonly ButtonMorph _closeButton;

	private const int HeaderHeight = 16;
	private const int Padding = 4;

	#endregion

	#region Constructors

	public WindowMorph(Point position, Size size, string title)
	{
		Position = position;
		Size = size;
		IsSelectable = true;

		// --- Header ---
		_header = new ContainerMorph
		{
			IsSelectable = false
		};

		_titleLabel = new LabelMorph(
			Point.Empty,
			"image.oem437_8",
			new Size(8, 8))
		{
			IsSelectable = false,
			Text = title,
			BackgroundColor = null
		};

		_closeButton = new ButtonMorph(
			Point.Empty,
			new Size(16, 12),
			"X")
		{
			IsSelectable = false,
			Command = new ActionCommand(() => MarkForDeletion())
		};

		_header.AddMorph(_titleLabel);
		_header.AddMorph(_closeButton);
		AddMorph(_header);

		// --- Content ---
		_content = new ContainerMorph
		{
			IsSelectable = false
		};

		AddMorph(_content);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The container morph where clients add their UI.
	/// Coordinates are local to the content area.
	/// </summary>
	public Morph Content => _content;

	public string Title
	{
		get => _titleLabel.Text;
		set => _titleLabel.Text = value;
	}

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		// Header layout (local coordinates)
		_header.Position = Point.Empty;
		_header.Size = new Size(Size.Width, HeaderHeight);

		_titleLabel.Position = new Point(
			Padding,
			(HeaderHeight - _titleLabel.Size.Height) / 2);

		_closeButton.Position = new Point(
			_header.Size.Width - _closeButton.Size.Width - Padding,
			(HeaderHeight - _closeButton.Size.Height) / 2);

		// Content layout (local coordinates)
		_content.Position = new Point(0, HeaderHeight);
		_content.Size = new Size(
			Size.Width,
			Size.Height - HeaderHeight);
	}

	#endregion

	#region Drawing

	protected override void DrawSelf(IRenderingContext rc)
	{
		// Draw window background (local)
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			GetWorld().Style.SelectionTint);

		// Draw header background (local)
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, new Size(Size.Width, HeaderHeight)),
			GetWorld().Style.HaloOutline);

		// Draw window outline (local)
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			RadialColor.Black);
	}

	#endregion
}