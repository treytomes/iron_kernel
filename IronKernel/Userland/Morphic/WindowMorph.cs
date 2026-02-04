using System.Drawing;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.ValueObjects;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// A compositional window morph with semantic styling.
/// Structural operations (move/resize/delete) are handled by the Halo.
/// </summary>
public class WindowMorph : Morph
{
	#region Fields

	private readonly DockPanelMorph _rootLayout;
	private readonly DockPanelMorph _header;
	private Morph _content;

	private readonly LabelMorph _titleLabel;
	private readonly ButtonMorph _closeButton;

	private const int HeaderHeight = 6 * 2;

	#endregion

	#region Constructor

	public WindowMorph(Point position, Size size, string title)
	{
		Position = position;
		Size = size;

		IsSelectable = true;
		ShouldClipToBounds = true;

		// Root layout: header (Top) + content (Fill)
		_rootLayout = new DockPanelMorph
		{
			IsSelectable = false,
			ShouldClipToBounds = true
		};
		AddMorph(_rootLayout);

		// Header layout: title (Fill) + close button (Right)
		_header = new DockPanelMorph
		{
			IsSelectable = false,
			ShouldClipToBounds = true,
			Size = new Size(size.Width, HeaderHeight) // semantic invariant
		};

		_titleLabel = new LabelMorph
		{
			Text = title,
			IsSelectable = false,
			BackgroundColor = null
		};

		_closeButton = new ButtonMorph(
			Point.Empty,
			new Size(10, 10),
			"X")
		{
			IsSelectable = false,
			Command = new ActionCommand(MarkForDeletion)
		};

		_header.AddMorph(_titleLabel);
		_header.AddMorph(_closeButton);
		_header.SetDock(_titleLabel, Dock.Fill);
		_header.SetDock(_closeButton, Dock.Right);

		// Content container (passive by design)
		_content = new ContainerMorph
		{
			IsSelectable = false,
			ShouldClipToBounds = true
		};

		// Compose window
		_rootLayout.AddMorph(_header);
		_rootLayout.AddMorph(_content);
		_rootLayout.SetDock(_header, Dock.Top);
		_rootLayout.SetDock(_content, Dock.Fill);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Container for window contents (local coordinates).
	/// </summary>
	public Morph Content
	{
		get => _content;
		protected set
		{
			_rootLayout.RemoveDock(_content);
			_rootLayout.RemoveMorph(_content);
			_content = value;
			if (_content != null)
			{
				_rootLayout.AddMorph(_content);
				_rootLayout.SetDock(_content, Dock.Fill);
			}
		}
	}

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
		// Window provides only outer constraints.
		_rootLayout.Position = Point.Empty;
		_rootLayout.Size = Size;

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