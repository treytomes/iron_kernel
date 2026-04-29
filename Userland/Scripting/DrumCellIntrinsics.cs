using System.Drawing;
using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public static class DrumCellIntrinsics
{
	public static void Register()
	{
		CreateSetLitIntrinsic();
		CreateSetHighlightedIntrinsic();
		CreateIsLitIntrinsic();
		CreateNamespace();
		CreateCreateIntrinsic();
	}

	private static void CreateNamespace()
	{
		var ns = Intrinsic.Create("DrumCell");
		ns.code = (ctx, _) =>
		{
			var map = new ValMap
			{
				["create"] = Intrinsic.GetByName("drumcell_create")!.GetFunc()
			};
			return new Intrinsic.Result(map);
		};
	}

	private static void CreateCreateIntrinsic()
	{
		var create = Intrinsic.Create("drumcell_create");
		create.AddParam("pos");
		create.AddParam("size");
		create.AddParam("column", new ValNumber(0));

		create.code = (ctx, _) =>
		{
			try
			{
				if (ctx.interpreter.hostData is not WorldScriptContext world)
					return Intrinsic.Result.Null;

				var posVal = ctx.GetVar("pos");
				if (!posVal.IsPoint())
					return Error(ctx, "DrumCell.create expects pos [x,y]");

				var sizeVal = ctx.GetVar("size");
				if (!sizeVal.IsSize())
					return Error(ctx, "DrumCell.create expects size [w,h]");

				var col = ctx.GetVar("column").IntValue();

				var cell = new DrumCellMorph(posVal.ToPoint(), sizeVal.ToSize(), col);
				world.World.AddMorph(cell);

				var handle = world.Handles.Register(cell);
				handle["destroy"]       = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
				handle["isAlive"]       = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
				handle["props"]         = cell.ScriptObject;
				handle["setLit"]        = Intrinsic.GetByName("drumcell_setLit")!.GetFunc().BindAndCopy(handle);
				handle["setHighlighted"]= Intrinsic.GetByName("drumcell_setHighlighted")!.GetFunc().BindAndCopy(handle);
				handle["isLit"]         = Intrinsic.GetByName("drumcell_isLit")!.GetFunc().BindAndCopy(handle);

				return new Intrinsic.Result(handle);
			}
			catch (Exception ex)
			{
				return Error(ctx, $"DrumCell.create error: {ex.Message}");
			}
		};
	}

	private static void CreateSetLitIntrinsic()
	{
		var fn = Intrinsic.Create("drumcell_setLit");
		fn.AddParam("lit");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not DrumCellMorph cell)
				return Intrinsic.Result.Null;
			cell.SetLit(ctx.GetVar("lit").BoolValue());
			return Intrinsic.Result.Null;
		};
	}

	private static void CreateSetHighlightedIntrinsic()
	{
		var fn = Intrinsic.Create("drumcell_setHighlighted");
		fn.AddParam("highlighted");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.Null;
			if (world.Handles.ResolveAlive(ctx.self) is not DrumCellMorph cell)
				return Intrinsic.Result.Null;
			cell.SetHighlighted(ctx.GetVar("highlighted").BoolValue());
			return Intrinsic.Result.Null;
		};
	}

	private static void CreateIsLitIntrinsic()
	{
		var fn = Intrinsic.Create("drumcell_isLit");
		fn.code = (ctx, _) =>
		{
			if (ctx.interpreter.hostData is not WorldScriptContext world)
				return Intrinsic.Result.False;
			if (world.Handles.ResolveAlive(ctx.self) is not DrumCellMorph cell)
				return Intrinsic.Result.False;
			return cell.IsLit ? Intrinsic.Result.True : Intrinsic.Result.False;
		};
	}

	private static Intrinsic.Result Error(TAC.Context ctx, string message)
	{
		ctx.interpreter.errorOutput?.Invoke(message, true);
		return Intrinsic.Result.Null;
	}
}
