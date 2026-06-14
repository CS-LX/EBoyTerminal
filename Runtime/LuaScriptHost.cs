using Game;
using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>MoonSharp Lua 解释器封装；由 <see cref="ComponentLuaScriptHost"/> 按实体持有实例。</summary>
public sealed class LuaScriptHost {
    const string ModuleRoot = "Lua";

    readonly LuaMachine m_machine = new();
    readonly Dictionary<string, DynValue> m_loadedModules = new();

    Script? m_script;
    Table? m_packageLoadedTable;

    public string? LastError => m_machine.LastError;

    public LuaMachine Machine => m_machine;

    public LuaMachineState State => m_machine.State;

    /// <summary>Lua <c>print</c> 等标准输出回调；由终端 Component 接入显示屏缓冲。</summary>
    public Action<string>? OnOutput { get; set; }

    /// <summary>脚本编译/运行失败回调；与 <see cref="OnOutput"/> 对称，不向游戏主循环抛异常。</summary>
    public Action<string>? OnError { get; set; }

    public Script Script {
        get {
            m_script ??= CreateScript();
            return m_script;
        }
    }

    public int ActiveCoroutineCount => m_machine.ActiveCoroutineCount;

    public void Reset() {
        m_script = CreateScript();
        m_loadedModules.Clear();
        m_packageLoadedTable = null;
        m_machine.Bind(Script);
        m_machine.SetOutputSink(OnOutput);
        m_machine.OnError = OnError;
        RegisterModuleApi(Script);
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
            if (m_packageLoadedTable == null) {
                RegisterModuleApi(Script);
            }
            result = Script.DoString(source, null, chunkName ?? "chunk");
            return true;
        }
        catch (Exception ex) {
            string message = FormatException(ex);
            m_machine.ReportRuntimeError(message);
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

    public void RegisterApiTable(
        string name,
        IReadOnlyDictionary<string, DynValue> members,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, DynValue>>? subTables) {
        m_machine.SetTable(name, members, subTables);
    }

    void RegisterModuleApi(Script script) {
        Table packageTable = new(script);
        m_packageLoadedTable = new Table(script);
        packageTable.Set("loaded", DynValue.NewTable(m_packageLoadedTable));
        packageTable.Set("path", DynValue.NewString("Assets/Lua/?.lua;Assets/Lua/?/init.lua"));
        script.Globals["package"] = packageTable;
        script.Globals["require"] = DynValue.NewCallback(Require);
    }

    DynValue Require(ScriptExecutionContext context, CallbackArguments args) {
        string moduleName = args.AsStringUsingMeta(context, 0, "require");
        return RequireModule(context.OwnerScript, moduleName);
    }

    DynValue RequireModule(Script script, string moduleName) {
        if (!IsValidModuleName(moduleName)) {
            throw new ScriptRuntimeException($"invalid module name '{moduleName}'");
        }
        if (m_loadedModules.TryGetValue(moduleName, out DynValue cached)) {
            return cached;
        }
        string logical = moduleName.Replace('.', '/');
        if (!TryLoadModuleSource(logical, out string source, out string chunkName)) {
            throw new ScriptRuntimeException($"module '{moduleName}' not found");
        }
        try {
            DynValue chunk = script.LoadString(source, null, chunkName);
            DynValue result = script.Call(chunk);
            m_loadedModules[moduleName] = result;
            m_packageLoadedTable?.Set(moduleName, result);
            return result;
        }
        catch (InterpreterException ex) {
            throw new ScriptRuntimeException(ex.DecoratedMessage ?? ex.Message);
        }
    }

    static bool TryLoadModuleSource(string logical, out string source, out string chunkName) {
        if (TryReadModuleAsset($"{ModuleRoot}/{logical}", out source)) {
            chunkName = $"{ModuleRoot}/{logical}.lua";
            return true;
        }
        if (TryReadModuleAsset($"{ModuleRoot}/{logical}/init", out source)) {
            chunkName = $"{ModuleRoot}/{logical}/init.lua";
            return true;
        }
        source = string.Empty;
        chunkName = string.Empty;
        return false;
    }

    static bool TryReadModuleAsset(string assetPath, out string source) {
        source = string.Empty;
        try {
            string? content = ContentManager.Get<string>(assetPath, ".lua", false);
            if (!string.IsNullOrEmpty(content)) {
                source = content;
                return true;
            }
        }
        catch {
            // 单元测试等未走模组 Content 管线时视为资源不存在。
        }
        return false;
    }

    static bool IsValidModuleName(string moduleName) {
        if (string.IsNullOrWhiteSpace(moduleName)) {
            return false;
        }
        if (moduleName.StartsWith('.') || moduleName.EndsWith('.') || moduleName.Contains("..")) {
            return false;
        }
        foreach (char character in moduleName) {
            if (char.IsAsciiLetterOrDigit(character) || character is '.' or '_') {
                continue;
            }
            return false;
        }
        return true;
    }

    static Script CreateScript() => new(CoreModules.Preset_SoftSandbox);

    static string FormatException(Exception ex) => ex switch {
        InterpreterException interpreterException => interpreterException.DecoratedMessage ?? interpreterException.Message,
        _ => ex.Message
    };
}
