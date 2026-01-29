using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Handles;
using System.Drawing;

namespace IronKernel.Userland.Morphic;

/// <summary>
/// A text morph whose size is derived from its content.
/// Resizing adjusts wrapping width only; height is computed from layout.
/// </summary>
public sealed class LabelMorph : Morph, ISemanticResizeTarget
{
	#region Fields

	private Font? _font;
	private string _text = string.Empty;

	/// <summary>
	/// The desired wrapping width in pixels.
	/// Null means single-line (natural width).
	/// </summary>
	private int? _wrapWidth;

	private RadialColor? _foregroundColorOverride;
	private RadialColor? _backgroundColorOverride;

	#endregion

	#region Constructors

	public LabelMorph(Point position, string assetId, Size tileSize)
	{
		Position = position;
		AssetId = assetId;
		TileSize = tileSize;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public string AssetId { get; }
	public Size TileSize { get; }

	public RadialColor? ForegroundColor
	{
		get => _foregroundColorOverride;
		set
		{
			_foregroundColorOverride = value;
			Invalidate();
		}
	}

	public RadialColor? BackgroundColor
	{
		get => _backgroundColorOverride;
		set
		{
			_backgroundColorOverride = value;
			Invalidate();
		}
	}

	private RadialColor EffectiveForegroundColor =>
		_foregroundColorOverride
		?? GetWorld().Style.LabelForegroundColor;

	private RadialColor? EffectiveBackgroundColor =>
		_backgroundColorOverride
		?? GetWorld().Style.LabelBackgroundColor;

	public string Text
	{
		get => _text;
		set
		{
			if (_text == value) return;
			_text = value;
			UpdateLayout();
			Invalidate();
		}
	}

	#endregion

	#region Loading / Drawing

	protected override async void OnLoad(IAssetService assets)
	{
		_font = await assets.LoadFontAsync(AssetId, TileSize);
		UpdateLayout();
	}

	public override void Draw(IRenderingContext rc)
	{
		if (_font == null) return;

		var lines = ComputeWrappedLines();
		for (int i = 0; i < lines.Count; i++)
		{
			_font.WriteString(
				rc,
				lines[i],
				new Point(Position.X, Position.Y + i * TileSize.Height),
				EffectiveForegroundColor,
				EffectiveBackgroundColor);
		}
	}

	#endregion

	#region Resize handling (command-based)

	public override bool CanExecute(ICommand command)
	{
		if (command is ResizeCommand)
			return true;

		return base.CanExecute(command);
	}

	protected override void ExecuteResize(ResizeCommand command)
	{
		if (_font == null)
			return;

		int deltaWidth;
		bool adjustPosition;

		switch (command.Handle)
		{
			case ResizeHandle.TopLeft:
			case ResizeHandle.BottomLeft:
				deltaWidth = -command.DeltaX;
				adjustPosition = true;
				break;

			case ResizeHandle.TopRight:
			case ResizeHandle.BottomRight:
				deltaWidth = command.DeltaX;
				adjustPosition = false;
				break;

			default:
				return;
		}

		if (deltaWidth == 0)
			return;

		var minWidth = MinimumWidth;
		var maxWidth = MaximumWidth;

		var currentWidth = _wrapWidth ?? maxWidth;
		var newWidth = Math.Clamp(currentWidth + deltaWidth, minWidth, maxWidth);

		var appliedDelta = newWidth - currentWidth;

		// Apply left-edge movement if needed
		if (adjustPosition && appliedDelta != 0)
		{
			Position = new Point(Position.X - appliedDelta, Position.Y);
		}

		_wrapWidth = newWidth;
		UpdateLayout();
		Invalidate();
	}

	protected override void UndoResize(ResizeCommand command)
	{
		if (_font == null)
			return;

		int deltaWidth;
		bool adjustPosition;

		switch (command.Handle)
		{
			case ResizeHandle.TopLeft:
			case ResizeHandle.BottomLeft:
				deltaWidth = command.DeltaX;
				adjustPosition = true;
				break;

			case ResizeHandle.TopRight:
			case ResizeHandle.BottomRight:
				deltaWidth = -command.DeltaX;
				adjustPosition = false;
				break;

			default:
				return;
		}

		if (deltaWidth == 0)
			return;

		var minWidth = MinimumWidth;
		var maxWidth = MaximumWidth;

		var currentWidth = _wrapWidth ?? maxWidth;
		var newWidth = Math.Clamp(currentWidth + deltaWidth, minWidth, maxWidth);

		var appliedDelta = newWidth - currentWidth;

		// Apply left-edge movement if needed
		if (adjustPosition && appliedDelta != 0)
		{
			Position = new Point(Position.X - appliedDelta, Position.Y);
		}

		_wrapWidth = newWidth;
		UpdateLayout();
		Invalidate();
	}

	#endregion

	#region Layout logic

	private int MinimumWidth
		=> TileSize.Width; // one glyph wide [2]

	private int MaximumWidth
		=> _font!.MeasureString(_text).Width; // single-line width [2]

	protected override void UpdateLayout()
	{
		if (_font == null)
			return;

		var lines = ComputeWrappedLines();

		var width = _wrapWidth ?? MaximumWidth;
		var height = lines.Count * TileSize.Height;

		Size = new Size(width, height);
	}

	private List<string> ComputeWrappedLines()
	{
		var result = new List<string>();

		if (_font == null || string.IsNullOrEmpty(_text))
		{
			result.Add(string.Empty);
			return result;
		}

		// No wrapping → single line
		if (_wrapWidth == null)
		{
			result.Add(_text);
			return result;
		}

		var charsPerLine = Math.Max(1, _wrapWidth.Value / TileSize.Width);

		for (int i = 0; i < _text.Length; i += charsPerLine)
		{
			var length = Math.Min(charsPerLine, _text.Length - i);
			result.Add(_text.Substring(i, length));
		}

		return result;
	}

	public object CaptureResizeState()
	{
		return _wrapWidth ?? MaximumWidth;
	}

	public void RestoreResizeState(object state)
	{
		_wrapWidth = (int?)state;
		UpdateLayout();
	}

	#endregion
}