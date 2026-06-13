using System.Globalization;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using TemplatesDatabase;
using EBoyTerminal.Electric;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    /// <summary>月之终端逻辑电路 IO 缓存与 <c>terminal.electric.*</c> Lua API。</summary>
    public class ComponentMoonTerminalElectric : Component, ILuaScriptApiProvider {
        public const string OutputVoltagesKey = "ElectricOutputVoltages";

        const float VoltageEpsilon = 0.0001f;

        readonly float[] m_inputVoltages = new float[6];
        readonly float[] m_outputVoltages = new float[6];
        readonly int[] m_pulseTicksRemaining = new int[6];
        readonly float[] m_pulseReleaseVoltage = new float[6];

        ComponentBlockEntity m_blockEntity = null!;
        ComponentMoonTerminal m_terminal = null!;
        MoonTerminalElectricElement? m_electricElement;

        public bool IsIoEnabled => Terminal.IsPowered;

        ComponentMoonTerminal Terminal =>
            m_terminal ?? Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
            m_terminal = Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            LoadOutputVoltages(valuesDictionary.GetValue(OutputVoltagesKey, string.Empty));
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(OutputVoltagesKey, SerializeOutputVoltages());
        }

        public void BindElectricElement(MoonTerminalElectricElement element) {
            m_electricElement = element;
        }

        public float GetOutputVoltage(int face) {
            if (face is < 0 or > 5 || !IsIoEnabled) {
                return 0f;
            }
            return m_outputVoltages[face];
        }

        public bool SetInputReading(int face, float voltage) {
            if (face is < 0 or > 5) {
                return false;
            }
            voltage = ClampVoltage(voltage);
            if (Math.Abs(m_inputVoltages[face] - voltage) <= VoltageEpsilon) {
                return false;
            }
            m_inputVoltages[face] = voltage;
            return true;
        }

        public bool ClearInputReadings() {
            bool changed = false;
            for (int face = 0; face < 6; face++) {
                if (m_inputVoltages[face] != 0f) {
                    m_inputVoltages[face] = 0f;
                    changed = true;
                }
            }
            return changed;
        }

        public void OnPowerLost() {
            ClearInputReadings();
            ClearPulses();
            ClearOutputs();
            m_electricElement?.QueueSimulation();
        }

        public void AdvancePulses() {
            if (!IsIoEnabled) {
                return;
            }
            bool changed = false;
            for (int face = 0; face < 6; face++) {
                if (m_pulseTicksRemaining[face] <= 0) {
                    continue;
                }
                m_pulseTicksRemaining[face]--;
                if (m_pulseTicksRemaining[face] != 0) {
                    continue;
                }
                float releaseVoltage = m_pulseReleaseVoltage[face];
                if (Math.Abs(m_outputVoltages[face] - releaseVoltage) <= VoltageEpsilon) {
                    continue;
                }
                m_outputVoltages[face] = releaseVoltage;
                changed = true;
            }
            if (changed) {
                m_electricElement?.QueueSimulation();
            }
        }

        public bool TryReadPort(string portName, out float voltage, out string? error) {
            voltage = 0f;
            error = null;
            if (!TryResolvePort(portName, out int face, out error)) {
                return false;
            }
            if (!IsIoEnabled) {
                return true;
            }
            voltage = m_inputVoltages[face];
            return true;
        }

        public bool TryWritePort(string portName, float voltage, out string? error) {
            error = null;
            if (!TryResolvePort(portName, out int face, out error)) {
                return false;
            }
            if (!IsIoEnabled) {
                error = "terminal is unpowered";
                return false;
            }
            voltage = ClampVoltage(voltage);
            CancelPulse(face);
            if (Math.Abs(m_outputVoltages[face] - voltage) <= VoltageEpsilon) {
                return true;
            }
            m_outputVoltages[face] = voltage;
            m_electricElement?.QueueSimulation();
            return true;
        }

        public bool TryPulsePort(string portName, float voltage, int ticks, out string? error) {
            error = null;
            if (ticks < 1) {
                error = "pulse ticks must be >= 1";
                return false;
            }
            if (!TryResolvePort(portName, out int face, out error)) {
                return false;
            }
            if (!IsIoEnabled) {
                error = "terminal is unpowered";
                return false;
            }
            voltage = ClampVoltage(voltage);
            m_pulseReleaseVoltage[face] = 0f;
            m_pulseTicksRemaining[face] = ticks;
            if (Math.Abs(m_outputVoltages[face] - voltage) <= VoltageEpsilon) {
                m_electricElement?.QueueSimulation();
                return true;
            }
            m_outputVoltages[face] = voltage;
            m_electricElement?.QueueSimulation();
            return true;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddSubTable("electric", new Dictionary<string, DynValue> {
                ["ports"] = context.Callback((executionContext, _) => BuildPortNameTable(executionContext)),
                ["read"] = context.Callback((_, args) => {
                    if (args.Count < 1) {
                        throw new ScriptRuntimeException("electric.read(port) requires a port name");
                    }
                    if (!TryReadPort(args[0].CastToString(), out float voltage, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid port");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["isHigh"] = context.Callback((_, args) => {
                    if (args.Count < 1) {
                        throw new ScriptRuntimeException("electric.isHigh(port) requires a port name");
                    }
                    if (!TryReadPort(args[0].CastToString(), out float voltage, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid port");
                    }
                    return DynValue.NewBoolean(ElectricElement.IsSignalHigh(voltage));
                }),
                ["level"] = context.Callback((_, args) => {
                    if (args.Count < 1) {
                        throw new ScriptRuntimeException("electric.level(port) requires a port name");
                    }
                    if (!TryReadPort(args[0].CastToString(), out float voltage, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid port");
                    }
                    return DynValue.NewNumber((int)MathF.Round(voltage * 15f));
                }),
                ["write"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.write(port, value) requires port and value");
                    }
                    if (!TryWritePort(args[0].CastToString(), (float)args[1].Number, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid port");
                    }
                    return DynValue.Nil;
                }),
                ["pulse"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.pulse(port, ticks[, value]) requires port and ticks");
                    }
                    float voltage = args.Count >= 3 ? (float)args[2].Number : 1f;
                    if (!TryPulsePort(args[0].CastToString(), voltage, (int)args[1].Number, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid port");
                    }
                    return DynValue.Nil;
                }),
                ["isInput"] = context.Callback((_, args) => DynValue.NewBoolean(IsPortName(args, 0))),
                ["isOutput"] = context.Callback((_, args) => DynValue.NewBoolean(IsPortName(args, 0))),
                ["enabled"] = context.Callback((_, _) => DynValue.NewBoolean(IsIoEnabled)),
            });
        }

        bool IsPortName(CallbackArguments args, int index) {
            if (args.Count <= index) {
                return false;
            }
            return TryResolvePort(args[index].CastToString(), out _, out _);
        }

        DynValue BuildPortNameTable(ScriptExecutionContext executionContext) {
            Table table = new(executionContext.OwnerScript);
            for (int i = 0; i < MoonTerminalElectricPorts.PortNames.Count; i++) {
                table.Set(i + 1, DynValue.NewString(MoonTerminalElectricPorts.PortNames[i]));
            }
            return DynValue.NewTable(table);
        }

        int GetBlockData() {
            ComponentBlockEntity? blockEntity = m_blockEntity ?? Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            return blockEntity != null ? Terrain.ExtractData(blockEntity.BlockValue) : 0;
        }

        bool TryResolvePort(string? portName, out int face, out string? error) {
            face = -1;
            error = null;
            if (!MoonTerminalElectricPorts.TryResolveFace(GetBlockData(), portName ?? string.Empty, out face)) {
                error = $"unknown port '{portName}'";
                return false;
            }
            return true;
        }

        bool ClearOutputs() {
            bool changed = ClearPulses();
            for (int face = 0; face < 6; face++) {
                if (m_outputVoltages[face] != 0f) {
                    m_outputVoltages[face] = 0f;
                    changed = true;
                }
            }
            return changed;
        }

        bool ClearPulses() {
            bool changed = false;
            for (int face = 0; face < 6; face++) {
                if (m_pulseTicksRemaining[face] == 0) {
                    continue;
                }
                m_pulseTicksRemaining[face] = 0;
                m_pulseReleaseVoltage[face] = 0f;
                changed = true;
            }
            return changed;
        }

        void CancelPulse(int face) {
            m_pulseTicksRemaining[face] = 0;
            m_pulseReleaseVoltage[face] = 0f;
        }

        void LoadOutputVoltages(string serialized) {
            Array.Clear(m_outputVoltages);
            if (string.IsNullOrWhiteSpace(serialized)) {
                return;
            }
            foreach (string segment in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                string[] parts = segment.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int face)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float voltage)
                    || face is < 0 or > 5) {
                    continue;
                }
                m_outputVoltages[face] = ClampVoltage(voltage);
            }
        }

        string SerializeOutputVoltages() {
            List<string> segments = new();
            for (int face = 0; face < 6; face++) {
                if (m_outputVoltages[face] <= VoltageEpsilon) {
                    continue;
                }
                segments.Add(string.Create(CultureInfo.InvariantCulture, $"{face}:{m_outputVoltages[face]:0.###}"));
            }
            return string.Join(';', segments);
        }

        static float ClampVoltage(float voltage) => Math.Clamp(voltage, 0f, 1f);
    }
}
