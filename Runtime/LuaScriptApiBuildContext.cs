using GameEntitySystem;
using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>实体上各 <see cref="ILuaScriptApiProvider"/> 向 <c>terminal</c> 全局表贡献成员时的构建上下文。</summary>
public sealed class LuaScriptApiBuildContext {
    public required ComponentLuaScriptHost ScriptHost { get; init; }

    public required Project Project { get; init; }

    internal Dictionary<string, DynValue> Root { get; } = new();

    internal Dictionary<string, IReadOnlyDictionary<string, DynValue>> SubTables { get; } = new();

    public void AddMember(string name, DynValue value) => Root[name] = value;

    public void AddSubTable(string name, IReadOnlyDictionary<string, DynValue> members) => SubTables[name] = members;

    public DynValue Callback(Func<ScriptExecutionContext, CallbackArguments, DynValue> handler) {
        return DynValue.NewCallback((context, args) => {
            try {
                return handler(context, args);
            }
            catch (Exception ex) {
                string message = ex is InterpreterException interpreterException
                    ? interpreterException.DecoratedMessage ?? interpreterException.Message
                    : ex.Message;
                ScriptHost.Host.Machine.ReportRuntimeError(message);
                return DynValue.Nil;
            }
        });
    }
}
