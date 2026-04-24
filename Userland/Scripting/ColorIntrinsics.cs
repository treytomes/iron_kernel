using IronKernel.Common.ValueObjects;
using Miniscript;
using Color = IronKernel.Common.ValueObjects.Color;

namespace Userland.Scripting;

public static class ColorIntrinsics
{
	public static void Register()
	{
		var create = Intrinsic.Create("Color_create");
		create.AddParam("r");
		create.AddParam("g");
		create.AddParam("b");

		create.code = (ctx, _) =>
		{
			try
			{
				var r = (float)ctx.GetVar("r").FloatValue();
				var g = (float)ctx.GetVar("g").FloatValue();
				var b = (float)ctx.GetVar("b").FloatValue();
				return new Intrinsic.Result(new Color(r, g, b).ToMiniScriptValue());
			}
			catch (Exception ex)
			{
				ctx.interpreter.errorOutput?.Invoke(
					$"Color.create error: {ex.Message}",
					true
				);
				return Intrinsic.Result.Null;
			}
		};

		var ns = Intrinsic.Create("Color");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap { ["create"] = create.GetFunc() };
			return new Intrinsic.Result(map);
		};
	}
}
