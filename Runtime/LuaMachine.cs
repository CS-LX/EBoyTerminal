using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 主线程协作式 Lua 虚拟机：编译/运行/停止、协程调度与内置 API。
/// </summary>
public sealed class LuaMachine {
    const float SleepEpsilon = 0.000001f;

    readonly List<CoroutineEntry> m_entries = new();

    readonly List<DynValue> m_spawnQueue = new();

    readonly Dictionary<string, Func<Script, DynValue>> m_globalFactories = new();

    Script? m_script;

    DynValue? m_mainFunc;

    Action<string>? m_outputSink;

    Action<string>? m_onError;

    public string? LastError { get; private set; }

    public LuaMachineState State { get; private set; } = LuaMachineState.Stopped;

    public int ActiveCoroutineCount => m_entries.Count;

    /// <summary>脚本编译/运行失败时回调；由上层写入终端输出，不向游戏主循环抛异常。</summary>
    public Action<string>? OnError {
        get => m_onError;
        set => m_onError = value;
    }

    /// <summary>MoonSharp 自动让出的指令数；值越小越不容易卡帧，但总吞吐也越低。</summary>
    public int AutoYieldInstructionCount { get; set; } = 8000;

    /// <summary>单帧最多恢复多少个协程，避免大量 <c>spawn</c> 同帧堆积。</summary>
    public int MaxResumesPerTick { get; set; } = 64;

    public void SetOutputSink(Action<string>? sink) => m_outputSink = sink;

    public void SetGlobal(string name, DynValue value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        m_globalFactories[name] = _ => value;
        ApplyGlobal(name);
    }

    public void SetGlobal(string name, object? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        m_globalFactories[name] = script => DynValue.FromObject(script, value);
        ApplyGlobal(name);
    }

    public void SetTable(
        string name,
        IReadOnlyDictionary<string, DynValue> members,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, DynValue>>? subTables = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Dictionary<string, DynValue> memberCopy = new(members);
        Dictionary<string, IReadOnlyDictionary<string, DynValue>> subTableCopy = subTables != null
            ? new Dictionary<string, IReadOnlyDictionary<string, DynValue>>(subTables)
            : [];
        m_globalFactories[name] = script => {
            Table table = new(script);
            foreach ((string memberName, DynValue memberValue) in memberCopy) {
                table.Set(memberName, memberValue);
            }
            foreach ((string subTableName, IReadOnlyDictionary<string, DynValue> subMembers) in subTableCopy) {
                Table subTable = new(script);
                foreach ((string memberName, DynValue memberValue) in subMembers) {
                    subTable.Set(memberName, memberValue);
                }
                table.Set(subTableName, DynValue.NewTable(subTable));
            }
            return DynValue.NewTable(table);
        };
        ApplyGlobal(name);
    }

    public void Bind(Script script) {
        m_script = script;
        m_entries.Clear();
        m_spawnQueue.Clear();
        LastError = null;
        State = LuaMachineState.Stopped;
        RegisterBuiltins(script);
        ApplyGlobals(script);
    }

    /// <summary>只编译玩家脚本并准备主协程；不会立即执行任何玩家代码。</summary>
    public bool LoadMainSource(string source, string? chunkName) {
        if (m_script == null) {
            return false;
        }
        m_entries.Clear();
        m_spawnQueue.Clear();
        LastError = null;
        if (string.IsNullOrWhiteSpace(source)) {
            m_mainFunc = null;
            State = LuaMachineState.Stopped;
            return true;
        }
        try {
            string wrapped = $"local function __eboy_main()\n{source}\nend\nreturn __eboy_main";
            DynValue chunkFunc = m_script.LoadString(wrapped, null, chunkName ?? "terminal");
            m_mainFunc = m_script.Call(chunkFunc);
            RecreateMainCoroutine();
            State = LuaMachineState.Ready;
            return LastError == null;
        }
        catch (Exception ex) {
            ReportCompileError(LuaRuntimeHelpers.FormatException(ex));
            return false;
        }
    }

    public bool Start() {
        if (State == LuaMachineState.Running) {
            return true;
        }
        if (State == LuaMachineState.Stopped && m_mainFunc != null) {
            RecreateMainCoroutine();
            State = LuaMachineState.Ready;
        }
        if (State != LuaMachineState.Ready && State != LuaMachineState.Paused) {
            return false;
        }
        State = LuaMachineState.Running;
        return true;
    }

    public void Pause() {
        if (State == LuaMachineState.Running) {
            State = LuaMachineState.Paused;
        }
    }

    public void Stop() {
        m_entries.Clear();
        m_spawnQueue.Clear();
        State = LuaMachineState.Stopped;
    }

    /// <summary>每帧调用一次；<paramref name="dt"/> 为秒，用于 <c>sleep(seconds)</c>。</summary>
    public void Tick(float dt) {
        if (m_script == null || State != LuaMachineState.Running) {
            return;
        }
        FlushSpawnQueue();
        int resumesRemaining = Math.Max(1, MaxResumesPerTick);
        for (int i = m_entries.Count - 1; i >= 0 && resumesRemaining > 0; i--) {
            CoroutineEntry entry = m_entries[i];
            if (entry.SleepSecondsRemaining > 0f) {
                entry.SleepSecondsRemaining -= dt;
                if (entry.SleepSecondsRemaining > SleepEpsilon) {
                    m_entries[i] = entry;
                    continue;
                }
                entry.SleepSecondsRemaining = 0f;
            }
            if (entry.TicksRemaining > 0) {
                entry.TicksRemaining--;
                if (entry.TicksRemaining > 0) {
                    m_entries[i] = entry;
                    continue;
                }
            }
            resumesRemaining--;
            ResumeEntry(ref entry);
            if (i < m_entries.Count && ReferenceEquals(m_entries[i].Handle, entry.Handle)) {
                m_entries[i] = entry;
            }
        }
        if (State == LuaMachineState.Running && m_entries.Count == 0 && m_spawnQueue.Count == 0) {
            RecreateMainCoroutine();
            State = LuaMachineState.Ready;
        }
    }

    void RecreateMainCoroutine() {
        m_entries.Clear();
        m_spawnQueue.Clear();
        if (m_script == null || m_mainFunc == null) {
            return;
        }
        DynValue coroutine = m_script.CreateCoroutine(m_mainFunc);
        coroutine.Coroutine.AutoYieldCounter = AutoYieldInstructionCount;
        m_entries.Add(new CoroutineEntry(coroutine));
    }

    void FlushSpawnQueue() {
        if (m_spawnQueue.Count == 0) {
            return;
        }
        foreach (DynValue coroutine in m_spawnQueue) {
            m_entries.Add(new CoroutineEntry(coroutine));
            int index = m_entries.Count - 1;
            CoroutineEntry entry = m_entries[index];
            ResumeEntry(ref entry);
            if (index < m_entries.Count && ReferenceEquals(m_entries[index].Handle, entry.Handle)) {
                m_entries[index] = entry;
            }
        }
        m_spawnQueue.Clear();
    }

    void ResumeEntry(ref CoroutineEntry entry) {
        if (m_script == null) {
            return;
        }
        try {
            DynValue result = entry.Handle.Coroutine.Resume();
            ProcessResumeResult(ref entry, result);
        }
        catch (Exception ex) {
            ReportRuntimeError(LuaRuntimeHelpers.FormatException(ex));
        }
    }

    void ProcessResumeResult(ref CoroutineEntry entry, DynValue result) {
        if (entry.Handle.Coroutine.State == CoroutineState.Dead) {
            RemoveEntry(entry);
            return;
        }
        ApplyYieldResult(ref entry, result);
    }

    void ApplyYieldResult(ref CoroutineEntry entry, DynValue result) {
        DynValue[]? args = result.Type switch {
            DataType.YieldRequest => result.YieldRequest?.ReturnValues,
            DataType.Tuple => result.Tuple,
            DataType.Void or DataType.Nil => null,
            _ => [result]
        };
        if (args == null || args.Length == 0) {
            // 裸 coroutine.yield() 和 MoonSharp forced yield 都至少让出到下一帧。
            entry.TicksRemaining = 1;
            entry.SleepSecondsRemaining = 0f;
            return;
        }
        if (args[0].Type == DataType.String && args[0].String == "ticks") {
            double ticks = args.Length > 1 && args[1].Type == DataType.Number ? args[1].Number : 1d;
            entry.TicksRemaining = Math.Max(0, (int)Math.Round(ticks));
            entry.SleepSecondsRemaining = 0f;
            return;
        }
        if (args[0].Type != DataType.Number) {
            entry.TicksRemaining = 1;
            entry.SleepSecondsRemaining = 0f;
            return;
        }
        entry.SleepSecondsRemaining = Math.Max(0f, (float)args[0].Number);
        entry.TicksRemaining = 0;
    }

    void Fail() {
        m_entries.Clear();
        m_spawnQueue.Clear();
        State = LuaMachineState.Error;
    }

    void ReportCompileError(string message) {
        LastError = message;
        m_onError?.Invoke(message);
        m_mainFunc = null;
        m_entries.Clear();
        State = LuaMachineState.Error;
    }

    public void ReportRuntimeError(string message) {
        LastError = message;
        m_onError?.Invoke(message);
        Fail();
    }

    void RemoveEntry(CoroutineEntry entry) {
        for (int i = m_entries.Count - 1; i >= 0; i--) {
            if (ReferenceEquals(m_entries[i].Handle, entry.Handle)) {
                m_entries.RemoveAt(i);
                return;
            }
        }
    }

    void ApplyGlobal(string name) {
        if (m_script == null || !m_globalFactories.TryGetValue(name, out Func<Script, DynValue>? factory)) {
            return;
        }
        m_script.Globals[name] = factory(m_script);
    }

    void ApplyGlobals(Script script) {
        foreach ((string name, Func<Script, DynValue> factory) in m_globalFactories) {
            script.Globals[name] = factory(script);
        }
    }

    void RegisterBuiltins(Script script) {
        script.Globals["spawn"] = DynValue.NewCallback(Spawn);
        script.Globals["print"] = DynValue.NewCallback(Print);
    }

    DynValue Print(ScriptExecutionContext context, CallbackArguments args) {
        try {
            if (args.Count == 0) {
                m_outputSink?.Invoke(string.Empty);
                return DynValue.Nil;
            }
            if (args.Count == 1) {
                m_outputSink?.Invoke(args[0].ToPrintString());
                return DynValue.Nil;
            }
            string text = string.Join("\t", args.GetArray().Select(static value => value.ToPrintString()));
            m_outputSink?.Invoke(text);
            return DynValue.Nil;
        }
        catch (Exception ex) {
            ReportRuntimeError(LuaRuntimeHelpers.FormatException(ex));
            return DynValue.Nil;
        }
    }

    DynValue Spawn(ScriptExecutionContext context, CallbackArguments args) {
        try {
            if (m_script == null) {
                return DynValue.Nil;
            }
            DynValue fn = args.AsType(0, "spawn", DataType.Function, false);
            DynValue coroutine = m_script.CreateCoroutine(fn);
            m_spawnQueue.Add(coroutine);
            return DynValue.Nil;
        }
        catch (Exception ex) {
            ReportRuntimeError(LuaRuntimeHelpers.FormatException(ex));
            return DynValue.Nil;
        }
    }

    struct CoroutineEntry {
        public CoroutineEntry(DynValue handle) {
            Handle = handle;
            SleepSecondsRemaining = 0f;
            TicksRemaining = 0;
        }

        public DynValue Handle;

        public float SleepSecondsRemaining;

        public int TicksRemaining;
    }
}
