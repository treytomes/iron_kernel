using System.Drawing;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Morphic;

public sealed class WorldMorph : Morph
{
	public WorldMorph(Size screenSize)
	{
		Position = Point.Empty;
		Size = screenSize;

		Hand = new HandMorph();
		AddMorph(Hand);
	}

	public Morph? PointerFocus { get; private set; }
	public Morph? KeyboardFocus { get; private set; }
	public HandMorph Hand { get; }

	public void PointerButton(MouseButton button, InputAction action)
	{
		if (button != MouseButton.Left)
			return;

		if (action == InputAction.Press)
		{
			var target = FindMorphAt(Hand.Position);
			if (target != null && target != this && target != Hand)
			{
				Hand.Grab(target, Hand.Position);
			}
		}
		else if (action == InputAction.Release)
		{
			Hand.Release();
		}
	}

	public void PointerMove(Point p)
	{
		Hand.MoveTo(p);
		Hand.Update();

		var e = new PointerMoveEvent(p);
		Hand.DispatchPointerMove(e);
	}

	public void PointerDown(Point p)
	{
		var target = FindMorphAt(p);
		if (target == null)
			return;

		PointerFocus = target;

		if (target.WantsKeyboardFocus)
			KeyboardFocus = target;

		var e = new PointerDownEvent(p)
		{
			Target = target,
		};
		target.DispatchPointerDown(e);
	}

	public void PointerUp(Point p)
	{
		var target = FindMorphAt(p);
		if (target == null)
			return;

		var e = new PointerUpEvent(p);
		target.DispatchPointerUp(e);
	}
}
