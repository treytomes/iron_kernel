using IronKernel.Common.ValueObjects;
using Miniscript;

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
				var color = new RadialColor(
					(byte)ctx.GetVar("r").IntValue(),
					(byte)ctx.GetVar("g").IntValue(),
					(byte)ctx.GetVar("b").IntValue()
				);
				return new Intrinsic.Result(color.ToMiniScriptValue());
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