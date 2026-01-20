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

	public HandMorph Hand { get; }

	public void PointerMove(Point p)
	{
		Hand.MoveTo(p);
		Hand.Update();
	}

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
}
