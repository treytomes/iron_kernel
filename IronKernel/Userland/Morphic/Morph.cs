using System.Drawing;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Morphic.Handles;

namespace IronKernel.Userland.Morphic;

public abstract class Morph : ICommandTarget
{
	#region Fields

	private readonly List<Morph> _submorphs = new();
	private bool _layoutInvalid = true;
	private Point _position;
	private Size _size;

	#endregion

	#region Properties

	public Rectangle Bounds => new Rectangle(Position, Size);

	public Point Position
	{
		get => _position;
		set
		{
			if (_position != value)
			{
				_position = value;
				InvalidateLayout();
			}
		}
	}

	public Size Size
	{
		get => _size;
		set
		{
			if (_size != value)
			{
				_size = value;
				InvalidateLayout();
			}
		}
	}

	public bool Visible { get; set; } = true;
	public bool IsHovered { get; private set; } = false;

	/// <summary>
	/// The mouse hover is over either this morph directly, or one of it's descendants.
	/// </summary>
	public bool IsEffectivelyHovered => IsHovered || Submorphs.Any(x => x.IsEffectivelyHovered);

	public Morph? Owner { get; private set; }

	public IReadOnlyList<Morph> Submorphs => _submorphs;
	public virtual bool WantsKeyboardFocus => false;
	public bool IsSelectable { get; set; } = true;
	public virtual bool IsGrabbable => false;
	public bool IsMarkedForDeletion { get; private set; } = false;
	public bool ShouldClipToBounds { get; set; } = false;

	protected MorphicStyle? Style
	{
		get
		{
			if (TryGetWorld(out var world))
			{
				return world.Style;
			}
			return null;
		}
	}

	#endregion

	#region Methods

	public void MarkForDeletion()
	{
		IsMarkedForDeletion = true;
	}

	internal void ClearDeletionMark()
	{
		IsMarkedForDeletion = false;
	}

	public void AddMorph(Morph morph, int index = -1)
	{
		if (morph == null)
			throw new ArgumentNullException(nameof(morph));
		if (morph.Owner != null)
			morph.Owner.RemoveMorph(morph);
		// if (morph.Owner != null)
		// 	throw new InvalidOperationException("Morph already has an owner");


		morph.Owner = this;
		if (index >= 0)
		{
			_submorphs.Insert(index, morph);
		}
		else
		{
			_submorphs.Add(morph);
		}
		InvalidateLayout();

		// If we're already in a world, load immediately.
		if (TryGetWorld(out var world))
			morph.NotifyAddedToWorld(world);
	}

	public bool RemoveMorph(Morph morph)
	{
		if (morph.Owner == null) return true;
		if (_submorphs.Remove(morph))
		{
			morph.Owner = null;
			InvalidateLayout();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Draw this morph. Coordinates are in world space.
	/// </summary>
	public void Draw(IRenderingContext rc)
	{
		var isRoot = this is WorldMorph;
		var offsetSize = rc.OffsetStackSize;
		var clipSize = rc.ClipStackSize;
		try
		{
			if (!isRoot)
			{
				rc.PushOffset(Position);
				if (ShouldClipToBounds) rc.PushClip(new Rectangle(Point.Empty, Size));
			}

			DrawSelf(rc);

			foreach (var child in _submorphs.ToArray())
			{
				if (!child.Visible) continue;
				child.Draw(rc);
			}
		}
		finally
		{
			rc.PopOffset(offsetSize);
			rc.PopClip(clipSize);
		}
	}

	protected virtual void DrawSelf(IRenderingContext rc) { }

	public virtual void Update(double deltaTime)
	{
		if (_layoutInvalid)
		{
			UpdateLayout();
			_layoutInvalid = false;
		}

		foreach (var child in _submorphs.ToArray())
			child.Update(deltaTime);
	}

	/// <summary>
	/// Hit testing.
	/// </summary>
	private bool ContainsPoint(Point worldPoint)
	{
		var localPoint = new Point(
			worldPoint.X - Position.X,
			worldPoint.Y - Position.Y);

		return new Rectangle(Point.Empty, Size).Contains(localPoint);
	}

	public virtual Morph? FindMorphAt(Point worldPoint)
	{
		var localPoint = new Point(
			worldPoint.X - Position.X,
			worldPoint.Y - Position.Y);

		// Traverse top-down so last added is "on top"
		for (int i = Submorphs.Count - 1; i >= 0; i--)
		{
			var child = Submorphs[i];
			if (!child.Visible) continue;

			var found = child.FindMorphAt(localPoint);
			if (found != null)
				return found;
		}

		return new Rectangle(Point.Empty, Size).Contains(localPoint)
			? this
			: null;
	}

	internal void SetHovered(bool value)
	{
		if (IsHovered == value)
			return;

		IsHovered = value;

		if (value)
			OnPointerEnter();
		else
			OnPointerLeave();
	}

	protected virtual void OnLoad(IAssetService assetService) { }

	protected virtual void OnPointerEnter() { }
	protected virtual void OnPointerLeave() { }

	public virtual void OnPointerDown(PointerDownEvent e) { }
	public virtual void OnPointerUp(PointerUpEvent e) { }
	public virtual void OnPointerMove(PointerMoveEvent e) { }
	public virtual void OnKeyDown(AppKeyboardEvent e) { }

	public void DispatchPointerDown(PointerDownEvent e)
	{
		OnPointerDown(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerDown(e);
	}

	public void DispatchPointerUp(PointerUpEvent e)
	{
		OnPointerUp(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerUp(e);
	}

	public void DispatchPointerMove(PointerMoveEvent e)
	{
		OnPointerMove(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerMove(e);
	}

	/// <summary>
	/// Mark this Morph's rendering area as needing a redraw.
	/// </summary>
	public virtual void Invalidate()
	{
		// no-op for now
	}

	/// <summary>
	/// Marks this morph's layout as invalid.
	/// </summary>
	protected void InvalidateLayout()
	{
		_layoutInvalid = true;
		Owner?.InvalidateLayout();
	}

	/// <summary>
	/// Recomputes layout if invalid.
	/// Subclasses override UpdateLayout to implement layout policy.
	/// </summary>
	protected virtual void UpdateLayout()
	{
		// Default: do nothing
	}

	protected bool TryGetWorld(out WorldMorph world)
	{
		if (this is WorldMorph w)
		{
			world = w;
			return true;
		}

		if (Owner != null)
			return Owner.TryGetWorld(out world);

		world = null!;
		return false;
	}

	protected WorldMorph GetWorld()
	{
		if (this is WorldMorph) return (this as WorldMorph)!;
		return (Owner ?? throw new InvalidOperationException("World is missing.")).GetWorld();
	}

	internal void NotifyAddedToWorld(WorldMorph world)
	{
		OnLoad(world.Assets);

		foreach (var child in _submorphs)
			child.NotifyAddedToWorld(world);
	}

	#region ICommandTarget Implementation

	/// <summary>
	/// Determines whether this morph is willing to execute the given command.
	/// Subclasses should override to restrict behavior.
	/// </summary>
	public virtual bool CanExecute(ICommand command)
	{
		return command switch
		{
			MoveCommand => true,
			ResizeCommand => true,
			DeleteCommand => Owner != null,
			_ => false
		};
	}

	/// <summary>
	/// Executes the given command, applying this morph's rules.
	/// </summary>
	public virtual void Execute(ICommand command)
	{
		switch (command)
		{
			case MoveCommand move:
				ExecuteMove(move);
				break;

			case ResizeCommand resize:
				ExecuteResize(resize);
				break;

			case DeleteCommand delete:
				delete.Execute(); // deletion owns its own logic
				break;
		}
	}

	/// <summary>
	/// Undoes the effects of a previously executed command.
	/// </summary>
	public virtual void Undo(ICommand command)
	{
		switch (command)
		{
			case MoveCommand move:
				UndoMove(move);
				break;

			case ResizeCommand resize:
				UndoResize(resize);
				break;

			case DeleteCommand delete:
				delete.Undo();
				break;
		}
	}

	#endregion

	#region Move handling

	/// <summary>
	/// Applies a move command.
	/// </summary>
	protected virtual void ExecuteMove(MoveCommand command)
	{
		Position = new Point(
			Position.X + command.DeltaX,
			Position.Y + command.DeltaY);

		Invalidate();
	}

	/// <summary>
	/// Reverts a move command.
	/// </summary>
	protected virtual void UndoMove(MoveCommand command)
	{
		Position = new Point(
			Position.X - command.DeltaX,
			Position.Y - command.DeltaY);
		Invalidate();
	}

	#endregion

	#region Resize handling

	/// <summary>
	/// Applies a resize command.
	/// Default behavior is free resize.
	/// </summary>
	protected virtual void ExecuteResize(ResizeCommand command)
	{
		var newSize = Size;
		var newPosition = Position;

		switch (command.Handle)
		{
			case ResizeHandle.TopLeft:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height - command.DeltaY);
				newPosition = new Point(Position.X + command.DeltaX, Position.Y + command.DeltaY);
				break;

			case ResizeHandle.TopRight:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height - command.DeltaY);
				newPosition = new Point(Position.X, Position.Y + command.DeltaY);
				break;

			case ResizeHandle.BottomLeft:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height + command.DeltaY);
				newPosition = new Point(Position.X + command.DeltaX, Position.Y);
				break;

			case ResizeHandle.BottomRight:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height + command.DeltaY);
				break;
		}

		Size = ClampSize(newSize);
		Position = newPosition;

		Invalidate();
	}

	/// <summary>
	/// Reverts a resize command.
	/// </summary>
	protected virtual void UndoResize(ResizeCommand command)
	{
		var newSize = Size;
		var newPosition = Position;

		switch (command.Handle)
		{
			case ResizeHandle.TopLeft:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height + command.DeltaY);
				newPosition = new Point(Position.X - command.DeltaX, Position.Y - command.DeltaY);
				break;

			case ResizeHandle.TopRight:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height + command.DeltaY);
				newPosition = new Point(Position.X, Position.Y - command.DeltaY);
				break;

			case ResizeHandle.BottomLeft:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height - command.DeltaY);
				newPosition = new Point(Position.X - command.DeltaX, Position.Y);
				break;

			case ResizeHandle.BottomRight:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height - command.DeltaY);
				break;
		}

		Size = ClampSize(newSize);
		Position = newPosition;

		Invalidate();
	}

	#endregion

	/// <summary>
	/// Ensures the size remains valid.
	/// Subclasses may override.
	/// </summary>
	protected virtual Size ClampSize(Size size)
	{
		return new Size(
			Math.Max(1, size.Width),
			Math.Max(1, size.Height));
	}

	public void CenterOnOwner()
	{
		if (Owner == null) return;

		var ownerCenterX = (Owner.Bounds.Right - Owner.Bounds.Left) / 2;
		var selfCenterX = (Bounds.Right - Bounds.Left) / 2;

		var ownerCenterY = (Owner.Bounds.Bottom - Owner.Bounds.Top) / 2;
		var selfCenterY = (Bounds.Bottom - Bounds.Top) / 2;
		Position = new Point(Owner.Bounds.X + ownerCenterX - selfCenterX, Owner.Bounds.Y + ownerCenterY - selfCenterY);
	}

	#endregion
}
