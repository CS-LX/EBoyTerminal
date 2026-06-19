using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

internal static class LuaRuntimeHelpers {
    public static string FormatException(Exception ex) => ex switch {
        InterpreterException interpreterException => interpreterException.DecoratedMessage ?? interpreterException.Message,
        _ => ex.Message
    };

    public static void AddContributorReader(
        IDictionary<string, DynValue> members,
        string name,
        LuaScriptApiBuildContext api,
        Func<ScriptExecutionContext, CallbackArguments, DynValue> handler) {
        members[name] = api.Callback(handler);
    }
}
