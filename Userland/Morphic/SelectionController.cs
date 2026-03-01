namespace Userland.Morphic;

internal sealed class SelectionController<T>
{
	private T? _anchor;
	private T? _caret;
	private bool _hasAnchor;
	private bool _hasCaret;

	private readonly Comparison<T> _compare;

	public SelectionController(Comparison<T> compare)
	{
		_compare = compare ?? throw new ArgumentNullException(nameof(compare));
	}

	/// <summary>
	/// True if a non-empty selection exists.
	/// </summary>
	public bool HasSelection =>
		_hasAnchor &&
		_hasCaret &&
		_compare(_anchor!, _caret!) != 0;

	/// <summary>
	/// Clears selection completely.
	/// </summary>
	public void Clear()
	{
		_anchor = default;
		_caret = default;
		_hasAnchor = false;
		_hasCaret = false;
	}

	/// <summary>
	/// Begins a new selection at the given position.
	/// Anchor and caret are both set.
	/// </summary>
	public void Begin(T position)
	{
		_anchor = position;
		_caret = position;
		_hasAnchor = true;
		_hasCaret = true;
	}

	/// <summary>
	/// Begins a selection only if one does not already exist.
	/// </summary>
	public void BeginIfNeeded(T position)
	{
		if (!_hasAnchor)
			Begin(position);
	}

	/// <summary>
	/// Updates the caret position.
	/// </summary>
	public void Update(T position)
	{
		if (!_hasAnchor)
			Begin(position);
		else
		{
			_caret = position;
			_hasCaret = true;
		}
	}

	/// <summary>
	/// Returns the ordered (start, end) range.
	/// Caller must ensure HasSelection == true.
	/// </summary>
	public (T start, T end) GetRange()
	{
		if (!HasSelection)
			throw new InvalidOperationException("No active selection.");

		return _compare(_anchor!, _caret!) <= 0
			? (_anchor!, _caret!)
			: (_caret!, _anchor!);
	}

	/// <summary>
	/// Normalizes selection against changing content.
	/// Caller provides a validity predicate.
	/// </summary>
	public void Normalize(Func<T, bool> isValid)
	{
		if (!_hasAnchor || !_hasCaret)
			return;

		if (!isValid(_anchor!) || !isValid(_caret!))
			Clear();
	}
}

internal static class SelectionControllerExtensions
{
	public static void SelectAll(this SelectionController<(int, int)> @this, TextDocument document)
	{
		if (document.LineCount == 0)
		{
			@this.Clear();
			return;
		}

		var lastLineIndex = document.LineCount - 1;
		var lastColumn = document.Lines[lastLineIndex].Length;

		@this.Begin((0, 0));
		@this.Update((lastLineIndex, lastColumn));
	}

	public static void BeginIfShift(this SelectionController<(int, int)> @this, bool shift, int line, int column)
	{
		if (!shift)
			return;

		@this.BeginIfNeeded((line, column));
	}
}