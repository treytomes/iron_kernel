using IronKernel.Common.ValueObjects;
using Userland.Gfx;
using Userland.Morphic.Commands;
using Userland.Morphic.Halo;
using Userland.Morphic.Inspector;
using Userland.Services;
using System.Drawing;
using Miniscript;

namespace Userland.Morphic;

/// <summary>
/// A text morph whose size is derived from its content.
/// Resizing adjusts wrapping width only; height is computed from layout.
/// </summary>
public sealed class LabelMorph : Morph, ISemanticResizeTarget, IValueContentMorph
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

	public LabelMorph()
		: this(Point.Empty)
	{
	}

	public LabelMorph(Point position)
	{
		Position = position;
		IsSelectable = true;
	}

	#endregion

	#region Properties

	public Size TileSize => Style?.DefaultFontStyle.TileSize ?? new Size(1, 1);

	public RadialColor? ForegroundColor
	{
		get => _foregroundColorOverride;
		set
		{
			_foregroundColorOverride = value;
			Invalidate();
			SyncScriptState();
		}
	}

	public RadialColor? BackgroundColor
	{
		get => _backgroundColorOverride;
		set
		{
			_backgroundColorOverride = value;
			Invalidate();
			SyncScriptState();
		}
	}

	private RadialColor EffectiveForegroundColor =>
		_foregroundColorOverride
		?? GetWorld()!.Style.LabelForegroundColor;

	private RadialColor? EffectiveBackgroundColor =>
		_backgroundColorOverride
		?? GetWorld()?.Style.LabelBackgroundColor;

	public string Text
	{
		get => _text;
		set
		{
			if (_text == value) return;
			_text = value;
			UpdateLayout();
			Invalidate();
			SyncScriptState();
		}
	}

	#endregion

	#region Loading / Drawing

	protected override async void OnLoad(IAssetService assets)
	{
		if (Style == null) throw new Exception("Style is null.");

		_font = await assets.LoadFontAsync(
			Style.DefaultFontStyle.Url,
			Style.DefaultFontStyle.TileSize,
			Style.DefaultFontStyle.GlyphOffset
		);
		UpdateLayout();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		if (_font == null) return;

		var lines = ComputeWrappedLines();
		for (var i = 0; i < lines.Count; i++)
		{
			if (string.IsNullOrWhiteSpace(lines[i])) continue;

			var text = lines[i];
			var height = _font.TileSize.Height;
			var position = new Point(0, i * height);

			_font.WriteString(
				rc,
				text,
				position,
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

	private int MinimumWidth => _font!.TileSize.Width; // one glyph wide [2]

	private int MaximumWidth => _font!.MeasureString(_text).Width; // single-line width [2]

	protected override void UpdateLayout()
	{
		if (_font == null)
			return;

		var lines = ComputeWrappedLines();

		var width = _wrapWidth ?? MaximumWidth;
		var height = lines.Count * _font.TileSize.Height;

		Size = new Size(width, height);

		base.UpdateLayout();
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

		var charsPerLine = Math.Max(1, _wrapWidth.Value / _font.TileSize.Width);

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

	public void Refresh(object? value)
	{
		Text = FormatValue(value);
	}

	private string FormatValue(object? value)
	{
		return value?.ToString() ?? "<null>";
	}

	#endregion

	#region Scripting

	protected override ValMap CreateScriptObject()
	{
		var map = base.CreateScriptObject();
		map["text"] = new ValString(Text);
		map["foregroundColor"] = ForegroundColor?.ToMiniScriptValue() ?? (Value)ValNull.instance;
		map["backgroundColor"] = BackgroundColor?.ToMiniScriptValue() ?? (Value)ValNull.instance;
		return map;
	}

	protected override void SyncScriptState()
	{
		base.SyncScriptState();
		ScriptObject["text"] = new ValString(Text);
		ScriptObject["foregroundColor"] = ForegroundColor?.ToMiniScriptValue() ?? (Value)ValNull.instance;
		ScriptObject["backgroundColor"] = BackgroundColor?.ToMiniScriptValue() ?? (Value)ValNull.instance;
	}

	protected override void ApplyScriptState()
	{
		base.ApplyScriptState();

		var text = ScriptObject["text"] as ValString;
		if (text != null) Text = text.ToString();

		var foregroundColor = ScriptObject["foregroundColor"] as ValMap;
		if (foregroundColor != null && foregroundColor.IsColor()) ForegroundColor = foregroundColor.ToColor();

		var backgroundColor = ScriptObject["backgroundColor"] as ValMap;
		if (backgroundColor != null && backgroundColor.IsColor()) BackgroundColor = backgroundColor.ToColor();
	}

	#endregion
}