using System.Drawing;
using Miniscript;
using IronKernel.Userland.Gfx;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Userland.Morphic;

public sealed class MiniScriptMorph : Morph
{
	private readonly Interpreter _interpreter;

	private Rectangle _rect = new Rectangle(8, 8, 32, 16);
	private RadialColor _color = RadialColor.Orange;

	static MiniScriptMorph()
	{
		// --- morph_setRect ---
		var setRect = Intrinsic.Create("morph_setRect");
		setRect.AddParam("x");
		setRect.AddParam("y");
		setRect.AddParam("w");
		setRect.AddParam("h");
		setRect.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is MiniScriptMorph m)
			{
				m._rect = new Rectangle(
					ctx.GetVar("x").IntValue(),
					ctx.GetVar("y").IntValue(),
					ctx.GetVar("w").IntValue(),
					ctx.GetVar("h").IntValue()
				);
				m.Invalidate();
			}
			return Intrinsic.Result.Null;
		};

		// --- morph_setColor ---
		var setColor = Intrinsic.Create("morph_setColor");
		setColor.AddParam("r");
		setColor.AddParam("g");
		setColor.AddParam("b");
		setColor.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is MiniScriptMorph m)
			{
				m._color = new RadialColor(
					(byte)ctx.GetVar("r").IntValue(),
					(byte)ctx.GetVar("g").IntValue(),
					(byte)ctx.GetVar("b").IntValue()
				);
				m.Invalidate();
			}
			return Intrinsic.Result.Null;
		};

		// --- morph() constructor intrinsic ---
		var morphCtor = Intrinsic.Create("morph");
		morphCtor.code = (ctx, _) =>
		{
			var map = new ValMap();
			map["setRect"] = setRect.GetFunc();
			map["setColor"] = setColor.GetFunc();
			return new Intrinsic.Result(map);
		};
	}

	public MiniScriptMorph()
	{
		var scriptSource = @"
m = morph()
m.setRect(12, 12, 64, 24)
m.setColor(5, 3, 1)
";

		_interpreter = new Interpreter(scriptSource);
		_interpreter.hostData = this;
		_interpreter.RunUntilDone();
	}

	protected override void DrawSelf(IRenderingContext rc)
	{
		rc.RenderFilledRect(_rect, _color);
	}
}