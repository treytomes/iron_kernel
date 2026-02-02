using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// A compositional window morph with semantic styling.
/// Structural operations (move/resize/delete) are handled by the Halo.
/// </summary>
public class WindowMorph : Morph
{
	#region Fields

	private readonly ContainerMorph _header;
	private readonly ContainerMorph _content;
	private readonly LabelMorph _titleLabel;
	private readonly ButtonMorph _closeButton;

	private const int HeaderHeight = 6 * 2;
	private const int Padding = 3;

	#endregion

	#region Constructors

	public WindowMorph(Point position, Size size, string title)
	{
		Position = position;
		Size = size;
		IsSelectable = true;
		ShouldClipToBounds = true;

		// --- Header ---
		_header = new ContainerMorph
		{
			IsSelectable = false,
			ShouldClipToBounds = true
		};

		_titleLabel = new LabelMorph()
		{
			IsSelectable = false,
			Text = title,
			BackgroundColor = null
		};

		_closeButton = new ButtonMorph(
			Point.Empty,
			new Size(10, 10),
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
			IsSelectable = false,
			ShouldClipToBounds = true
		};

		AddMorph(_content);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Container for window contents (local coordinates).
	/// </summary>
	public Morph Content => _content;

	public string Title
	{
		get => _titleLabel.Text;
		set => _titleLabel.Text = value;
	}

	private bool IsSelected => GetWorld().SelectedMorph == this;

	#endregion

	#region Layout

	protected override void UpdateLayout()
	{
		// Header
		_header.Position = Point.Empty;
		_header.Size = new Size(Size.Width, HeaderHeight);

		_titleLabel.Position = new Point(
			Padding,
			(HeaderHeight - _titleLabel.Size.Height) / 2);

		_closeButton.Position = new Point(
			_header.Size.Width - _closeButton.Size.Width - Padding,
			(HeaderHeight - _closeButton.Size.Height) / 2);

		// Content
		_content.Position = new Point(0, HeaderHeight);
		_content.Size = new Size(
			Size.Width,
			Size.Height - HeaderHeight);

		base.UpdateLayout();
	}

	#endregion

	#region Drawing

	protected override void DrawSelf(IRenderingContext rc)
	{
		var semantic = GetWorld().Style.Semantic;

		// Window background
		rc.RenderFilledRect(
			new Rectangle(Point.Empty, Size),
			semantic.Surface);

		// Header background (semantic selection)
		var headerColor = IsSelected
			? semantic.Primary
			: semantic.Surface;

		rc.RenderFilledRect(
			new Rectangle(Point.Empty, new Size(Size.Width, HeaderHeight)),
			headerColor);

		// Window border
		rc.RenderRect(
			new Rectangle(Point.Empty, Size),
			semantic.Border);

		// Title text color
		_titleLabel.ForegroundColor = semantic.Text;
	}

	#endregion
}