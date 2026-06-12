using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>MoonSharp Lua 解释器封装；由 <see cref="ComponentLuaScriptHost"/> 按实体持有实例。</summary>
public sealed class LuaScriptHost {
    Script? m_script;

    public string? LastError { get; private set; }

    public Script Script {
        get {
            m_script ??= CreateScript();
            return m_script;
        }
    }

    public void Reset() {
        m_script = CreateScript();
        LastError = null;
    }

    public bool TryLoad(string source, string? chunkName = null) {
        if (string.IsNullOrWhiteSpace(source)) {
            Reset();
            return true;
        }
        try {
            Reset();
            Script.DoString(source, null, chunkName ?? "chunk");
            LastError = null;
            return true;
        }
        catch (InterpreterException ex) {
            LastError = ex.DecoratedMessage ?? ex.Message;
            return false;
        }
    }

    public bool TryExecute(string source, out DynValue? result, string? chunkName = null) {
        result = null;
        try {
            m_script ??= CreateScript();
            result = m_script.DoString(source, null, chunkName ?? "chunk");
            LastError = null;
            return true;
        }
        catch (InterpreterException ex) {
            LastError = ex.DecoratedMessage ?? ex.Message;
            return false;
        }
    }

    public void RegisterGlobal(string name, object value) {
        Script.Globals[name] = UserData.Create(value);
    }

    static Script CreateScript() => new(CoreModules.Preset_SoftSandbox);
}
