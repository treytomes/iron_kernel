using Miniscript;

namespace Userland.Scripting;

public static class TimeIntrinsics
{
    public static void Register()
    {
        RegisterTime();
        RegisterWait();
        RegisterMouse();
    }

    // time — seconds since kernel startup (monotonic, from AppUpdateTick.TotalTime)
    private static void RegisterTime()
    {
        var fn = Intrinsic.Create("time");
        fn.code = (ctx, _) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return new Intrinsic.Result(0.0);
            return new Intrinsic.Result(new ValNumber(world.TotalTime));
        };
    }

    // wait(seconds) — suspend script without blocking the engine
    private static void RegisterWait()
    {
        var fn = Intrinsic.Create("wait");
        fn.AddParam("seconds", new ValNumber(0));
        fn.code = (ctx, partial) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return Intrinsic.Result.Null;

            var duration = ctx.GetVar("seconds") is ValNumber d ? d.value : 0.0;

            if (partial == null)
            {
                // First call: record the deadline
                var deadline = world.TotalTime + duration;
                var state = new ValMap();
                state["deadline"] = new ValNumber(deadline);
                return new Intrinsic.Result(state, done: false);
            }

            // Subsequent calls: check if time has elapsed
            var map = (ValMap)partial.result;
            var target = map["deadline"] is ValNumber n ? n.value : 0.0;
            if (world.TotalTime < target)
                return partial;

            return Intrinsic.Result.Null;
        };
    }

    // mouse — namespace with x, y, button
    private static void RegisterMouse()
    {
        var fn = Intrinsic.Create("mouse");
        fn.code = (ctx, _) =>
        {
            if (ctx.interpreter.hostData is not WorldScriptContext world)
                return new Intrinsic.Result(new ValMap());

            var pos = world.MousePosition;
            var map = new ValMap();
            map["x"] = new ValNumber(pos.X);
            map["y"] = new ValNumber(pos.Y);
            map["button"] = world.IsMouseButtonDown ? ValNumber.one : ValNumber.zero;
            return new Intrinsic.Result(map);
        };
    }
}
