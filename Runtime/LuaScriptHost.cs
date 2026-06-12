using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>MoonSharp Lua 解释器封装；由 <see cref="ComponentLuaScriptHost"/> 按实体持有实例。</summary>
public sealed class LuaScriptHost {
    readonly LuaMachine m_machine = new();

    Script? m_script;

    public string? LastError => m_machine.LastError;

    public LuaMachine Machine => m_machine;

    public LuaMachineState State => m_machine.State;

    /// <summary>Lua <c>print</c> 等标准输出回调；由终端 Component 接入显示屏缓冲。</summary>
    public Action<string>? OnOutput { get; set; }

    public Script Script {
        get {
            m_script ??= CreateScript();
            return m_script;
        }
    }

    public int ActiveCoroutineCount => m_machine.ActiveCoroutineCount;

    public void Reset() {
        m_script = CreateScript();
        m_machine.Bind(Script);
        m_machine.SetOutputSink(OnOutput);
    }

    public bool TryLoad(string source, string? chunkName = null) {
        Reset();
        return m_machine.LoadMainSource(source, chunkName);
    }

    public bool Start() => m_machine.Start();

    public void Pause() => m_machine.Pause();

    public void Stop() => m_machine.Stop();

    public void Tick(float dt) {
        if (m_script == null) {
            return;
        }
        m_machine.Tick(dt);
    }

    public bool TryExecute(string source, out DynValue? result, string? chunkName = null) {
        result = null;
        try {
            m_script ??= CreateScript();
            result = Script.DoString(source, null, chunkName ?? "chunk");
            return true;
        }
        catch (InterpreterException) {
            return false;
        }
    }

    public void RegisterGlobal(string name, DynValue value) {
        m_machine.SetGlobal(name, value);
    }

    public void RegisterGlobal(string name, object value) {
        m_machine.SetGlobal(name, value);
    }

    public void RegisterApiTable(string name, IReadOnlyDictionary<string, DynValue> members) {
        m_machine.SetTable(name, members);
    }

    static Script CreateScript() => new(CoreModules.Preset_SoftSandbox);
}
