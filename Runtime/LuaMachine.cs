using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 主线程协作式 Lua 虚拟机：编译/运行/停止、协程调度与内置 API。
/// </summary>
public sealed class LuaMachine {
    const float SleepEpsilon = 0.000001f;

    readonly List<CoroutineEntry> m_entries = new();

    readonly List<DynValue> m_spawnQueue = new();

    Script? m_script;

    Action<string>? m_outputSink;

    public string? LastError { get; private set; }

    public LuaMachineState State { get; private set; } = LuaMachineState.Stopped;

    public int ActiveCoroutineCount => m_entries.Count;

    /// <summary>MoonSharp 自动让出的指令数；值越小越不容易卡帧，但总吞吐也越低。</summary>
    public int AutoYieldInstructionCount { get; set; } = 8000;

    /// <summary>单帧最多恢复多少个协程，避免大量 <c>spawn</c> 同帧堆积。</summary>
    public int MaxResumesPerTick { get; set; } = 64;

    public void SetOutputSink(Action<string>? sink) => m_outputSink = sink;

    public void Bind(Script script) {
        m_script = script;
        m_entries.Clear();
        m_spawnQueue.Clear();
        LastError = null;
        State = LuaMachineState.Stopped;
        RegisterBuiltins(script);
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
            State = LuaMachineState.Stopped;
            return true;
        }
        try {
            string wrapped = $"local function __eboy_main()\n{source}\nend\nreturn __eboy_main";
            DynValue chunkFunc = m_script.LoadString(wrapped, null, chunkName ?? "terminal");
            DynValue mainFunc = m_script.Call(chunkFunc);
            DynValue coroutine = m_script.CreateCoroutine(mainFunc);
            coroutine.Coroutine.AutoYieldCounter = AutoYieldInstructionCount;
            m_entries.Add(new CoroutineEntry(coroutine));
            State = LuaMachineState.Ready;
            return LastError == null;
        }
        catch (InterpreterException ex) {
            LastError = ex.DecoratedMessage ?? ex.Message;
            m_entries.Clear();
            State = LuaMachineState.Error;
            return false;
        }
    }

    public bool Start() {
        if (State == LuaMachineState.Running) {
            return true;
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
            State = LuaMachineState.Stopped;
        }
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
        catch (InterpreterException ex) {
            LastError = ex.DecoratedMessage ?? ex.Message;
            Fail();
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

    void RemoveEntry(CoroutineEntry entry) {
        for (int i = m_entries.Count - 1; i >= 0; i--) {
            if (ReferenceEquals(m_entries[i].Handle, entry.Handle)) {
                m_entries.RemoveAt(i);
                return;
            }
        }
    }

    void RegisterBuiltins(Script script) {
        script.Globals["spawn"] = DynValue.NewCallback(Spawn);
        script.Globals["print"] = DynValue.NewCallback(Print);
    }

    DynValue Print(ScriptExecutionContext context, CallbackArguments args) {
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

    DynValue Spawn(ScriptExecutionContext context, CallbackArguments args) {
        if (m_script == null) {
            return DynValue.Nil;
        }
        DynValue fn = args.AsType(0, "spawn", DataType.Function, false);
        DynValue coroutine = m_script.CreateCoroutine(fn);
        m_spawnQueue.Add(coroutine);
        return DynValue.Nil;
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
