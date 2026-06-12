using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>MoonSharp Lua 解释器封装；由 <see cref="ComponentLuaScriptHost"/> 按实体持有实例。</summary>
public sealed class LuaScriptHost {
    readonly LuaCoroutineScheduler m_scheduler = new();

    Script? m_script;

    public string? LastError => m_scheduler.LastError;

    public LuaCoroutineScheduler Scheduler => m_scheduler;

    public LuaMachineState State => m_scheduler.State;

    public Script Script {
        get {
            m_script ??= CreateScript();
            return m_script;
        }
    }

    public int ActiveCoroutineCount => m_scheduler.ActiveCoroutineCount;

    public void Reset() {
        m_script = CreateScript();
        m_scheduler.Bind(Script);
    }

    public bool TryLoad(string source, string? chunkName = null) {
        Reset();
        return m_scheduler.LoadMainSource(source, chunkName);
    }

    public bool Start() {
        return m_scheduler.Start();
    }

    public void Stop() {
        m_scheduler.Stop();
    }

    public void Tick(float dt) {
        if (m_script == null) {
            return;
        }
        m_scheduler.Tick(dt);
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

    public void RegisterGlobal(string name, object value) {
        Script.Globals[name] = UserData.Create(value);
    }

    static Script CreateScript() => new(CoreModules.Preset_SoftSandbox);
}
