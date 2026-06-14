using System.Globalization;
using Engine;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using TemplatesDatabase;
using EBoyTerminal.Electric;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    /// <summary>
    /// 月之终端逻辑电路 IO。Lua/API 使用宿主 <see cref="ElectricConnectorDirection"/> 相对接线（Top/Left/Bottom/Right/In）。
    /// </summary>
    public class ComponentMoonTerminalElectric : Component, ILuaScriptApiProvider {
        public const string OutputVoltagesKey = "ElectricOutputVoltages";

        const int DirectionCount = 5;
        const float VoltageEpsilon = 0.0001f;

        readonly float[] m_inputVoltages = new float[DirectionCount];
        readonly float[] m_stableOutputVoltages = new float[DirectionCount];
        readonly float[] m_pulseVoltages = new float[DirectionCount];
        readonly int[] m_pulseReleaseCircuitSteps = new int[DirectionCount];

        ComponentMoonTerminal m_terminal = null!;
        ComponentBlockEntity? m_blockEntity;
        SubsystemElectricity? m_subsystemElectricity;
        MoonTerminalElectricElement? m_electricElement;

        public bool IsIoEnabled => Terminal.IsPowered;

        ComponentMoonTerminal Terminal =>
            m_terminal ?? Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_terminal = Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            m_blockEntity = Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            m_subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(throwOnError: false);
            LoadOutputVoltages(valuesDictionary.GetValue(OutputVoltagesKey, string.Empty));
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue(OutputVoltagesKey, SerializeOutputVoltages());
        }

        public void BindElectricElement(MoonTerminalElectricElement element) {
            m_electricElement = element;
        }

        public float GetOutputVoltage(ElectricConnectorDirection direction) {
            if (!IsValidDirection(direction) || !IsIoEnabled) {
                return 0f;
            }
            int index = (int)direction;
            if (HasActivePulse(index)) {
                return m_pulseVoltages[index];
            }
            return m_stableOutputVoltages[index];
        }

        public bool SetInputReading(ElectricConnectorDirection direction, float voltage) {
            if (!IsValidDirection(direction)) {
                return false;
            }
            int index = (int)direction;
            voltage = ClampVoltage(voltage);
            if (Math.Abs(m_inputVoltages[index] - voltage) <= VoltageEpsilon) {
                return false;
            }
            m_inputVoltages[index] = voltage;
            return true;
        }

        public bool ClearInputReadings() {
            bool changed = false;
            for (int index = 0; index < DirectionCount; index++) {
                if (m_inputVoltages[index] != 0f) {
                    m_inputVoltages[index] = 0f;
                    changed = true;
                }
            }
            return changed;
        }

        public void OnPowerLost() {
            ClearInputReadings();
            ClearOutputs();
            NotifyCircuitChanged();
        }

        public bool AdvancePulses(int circuitStep) {
            if (!IsIoEnabled) {
                return false;
            }
            bool changed = false;
            for (int index = 0; index < DirectionCount; index++) {
                int releaseStep = m_pulseReleaseCircuitSteps[index];
                if (releaseStep <= 0 || circuitStep < releaseStep) {
                    continue;
                }
                float pulseVoltage = m_pulseVoltages[index];
                m_pulseReleaseCircuitSteps[index] = 0;
                m_pulseVoltages[index] = 0f;
                if (Math.Abs(pulseVoltage - m_stableOutputVoltages[index]) > VoltageEpsilon) {
                    changed = true;
                }
            }
            return changed;
        }

        public bool TryReadInput(ElectricConnectorDirection direction, out float voltage) {
            voltage = 0f;
            if (!IsValidDirection(direction)) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = m_inputVoltages[(int)direction];
            }
            return true;
        }

        public bool TryReadOutput(ElectricConnectorDirection direction, out float voltage) {
            voltage = 0f;
            if (!IsValidDirection(direction)) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = GetOutputVoltage(direction);
            }
            return true;
        }

        public bool TryWriteDirection(ElectricConnectorDirection direction, float voltage, out string? error) {
            error = null;
            if (!IsValidDirection(direction)) {
                error = "direction must be 0-4 (ElectricConnectorDirection)";
                return false;
            }
            if (!IsIoEnabled) {
                error = "terminal is unpowered";
                return false;
            }
            int index = (int)direction;
            voltage = ClampVoltage(voltage);
            float previousVoltage = GetOutputVoltage(direction);
            CancelPulse(index);
            m_stableOutputVoltages[index] = voltage;
            if (Math.Abs(previousVoltage - GetOutputVoltage(direction)) > VoltageEpsilon) {
                NotifyCircuitChanged();
            }
            return true;
        }

        public bool TryPulseDirection(ElectricConnectorDirection direction, float voltage, int ticks, out string? error) {
            error = null;
            if (!IsValidDirection(direction)) {
                error = "direction must be 0-4 (ElectricConnectorDirection)";
                return false;
            }
            if (ticks < 1) {
                error = "pulse ticks must be >= 1";
                return false;
            }
            if (!IsIoEnabled) {
                error = "terminal is unpowered";
                return false;
            }
            if (!TryBindElectricElement()) {
                error = "electric element is not ready";
                return false;
            }
            int index = (int)direction;
            voltage = ClampVoltage(voltage);
            float previousVoltage = GetOutputVoltage(direction);
            m_pulseVoltages[index] = voltage;
            m_pulseReleaseCircuitSteps[index] = m_electricElement!.CircuitStep + ticks + 1;
            QueuePulseRelease(index);
            if (Math.Abs(previousVoltage - voltage) > VoltageEpsilon) {
                NotifyCircuitChanged();
            }
            return true;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddSubTable("electric", new Dictionary<string, DynValue> {
                ["listDirections"] = context.Callback((executionContext, _) => BuildDirectionTable(executionContext)),
                ["readInput"] = context.Callback((_, args) => {
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    if (!TryReadInput(direction, out float voltage)) {
                        throw new ScriptRuntimeException("invalid direction");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["readOutput"] = context.Callback((_, args) => {
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    if (!TryReadOutput(direction, out float voltage)) {
                        throw new ScriptRuntimeException("invalid direction");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["isHigh"] = context.Callback((_, args) => {
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    if (!TryReadInput(direction, out float voltage)) {
                        throw new ScriptRuntimeException("invalid direction");
                    }
                    return DynValue.NewBoolean(ElectricElement.IsSignalHigh(voltage));
                }),
                ["readLevel"] = context.Callback((_, args) => {
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    if (!TryReadInput(direction, out float voltage)) {
                        throw new ScriptRuntimeException("invalid direction");
                    }
                    return DynValue.NewNumber((int)MathF.Round(voltage * 15f));
                }),
                ["write"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.write(direction, value) requires direction and value");
                    }
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    if (!TryWriteDirection(direction, (float)args[1].Number, out error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    return DynValue.Nil;
                }),
                ["pulse"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.pulse(direction, ticks[, value]) requires direction and ticks");
                    }
                    if (!TryParseDirectionArg(args, 0, out ElectricConnectorDirection direction, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    float voltage = args.Count >= 3 ? (float)args[2].Number : 1f;
                    if (!TryPulseDirection(direction, voltage, (int)args[1].Number, out error)) {
                        throw new ScriptRuntimeException(error ?? "invalid direction");
                    }
                    return DynValue.Nil;
                }),
                ["isEnabled"] = context.Callback((_, _) => DynValue.NewBoolean(IsIoEnabled)),
            });
        }

        static bool TryParseDirectionArg(CallbackArguments args, int index, out ElectricConnectorDirection direction, out string? error) {
            direction = default;
            error = null;
            if (args.Count <= index) {
                error = "direction argument required";
                return false;
            }
            DynValue arg = args[index];
            if (arg.Type == DataType.Number) {
                int value = (int)arg.Number;
                if (!IsValidDirectionIndex(value)) {
                    error = "direction must be 0-4 (ElectricConnectorDirection)";
                    return false;
                }
                direction = (ElectricConnectorDirection)value;
                return true;
            }
            if (arg.Type == DataType.String) {
                if (!TryParseDirectionName(arg.String, out direction, out error)) {
                    return false;
                }
                return true;
            }
            error = "direction must be a number 0-4 or name (top/left/bottom/right/in)";
            return false;
        }

        static bool TryParseDirectionName(string text, out ElectricConnectorDirection direction, out string? error) {
            direction = default;
            error = null;
            switch (text.Trim().ToLowerInvariant()) {
                case "top": direction = ElectricConnectorDirection.Top; return true;
                case "left": direction = ElectricConnectorDirection.Left; return true;
                case "bottom": direction = ElectricConnectorDirection.Bottom; return true;
                case "right": direction = ElectricConnectorDirection.Right; return true;
                case "in":
                case "back": direction = ElectricConnectorDirection.In; return true;
                default:
                    error = "unknown connector direction: " + text;
                    return false;
            }
        }

        static DynValue BuildDirectionTable(ScriptExecutionContext executionContext) {
            Table table = new(executionContext.OwnerScript);
            for (int index = 0; index < DirectionCount; index++) {
                table.Set(index + 1, DynValue.NewNumber(index));
            }
            return DynValue.NewTable(table);
        }

        bool ClearOutputs() {
            bool changed = ClearPulses();
            for (int index = 0; index < DirectionCount; index++) {
                if (m_stableOutputVoltages[index] != 0f) {
                    m_stableOutputVoltages[index] = 0f;
                    changed = true;
                }
            }
            return changed;
        }

        bool ClearPulses() {
            bool changed = false;
            for (int index = 0; index < DirectionCount; index++) {
                if (m_pulseReleaseCircuitSteps[index] == 0) {
                    continue;
                }
                if (Math.Abs(m_pulseVoltages[index] - m_stableOutputVoltages[index]) > VoltageEpsilon) {
                    changed = true;
                }
                m_pulseReleaseCircuitSteps[index] = 0;
                m_pulseVoltages[index] = 0f;
            }
            return changed;
        }

        void CancelPulse(int index) {
            m_pulseReleaseCircuitSteps[index] = 0;
            m_pulseVoltages[index] = 0f;
        }

        void LoadOutputVoltages(string serialized) {
            Array.Clear(m_stableOutputVoltages);
            if (string.IsNullOrWhiteSpace(serialized)) {
                return;
            }
            foreach (string segment in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                string[] parts = segment.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int directionIndex)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float voltage)
                    || !IsValidDirectionIndex(directionIndex)) {
                    continue;
                }
                m_stableOutputVoltages[directionIndex] = ClampVoltage(voltage);
            }
        }

        string SerializeOutputVoltages() {
            List<string> segments = new();
            for (int index = 0; index < DirectionCount; index++) {
                if (m_stableOutputVoltages[index] <= VoltageEpsilon) {
                    continue;
                }
                segments.Add(string.Create(CultureInfo.InvariantCulture, $"{index}:{m_stableOutputVoltages[index]:0.###}"));
            }
            return string.Join(';', segments);
        }

        static float ClampVoltage(float voltage) => Math.Clamp(voltage, 0f, 1f);

        static bool IsValidDirection(ElectricConnectorDirection direction) => IsValidDirectionIndex((int)direction);

        static bool IsValidDirectionIndex(int directionIndex) => directionIndex is >= 0 and < DirectionCount;

        bool HasActivePulse(int index) => m_pulseReleaseCircuitSteps[index] > 0;

        bool TryBindElectricElement() {
            if (m_electricElement != null) {
                return true;
            }
            m_blockEntity ??= Entity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            m_subsystemElectricity ??= Project.FindSubsystem<SubsystemElectricity>(throwOnError: false);
            if (m_blockEntity == null || m_subsystemElectricity == null) {
                return false;
            }
            Point3 coordinates = m_blockEntity.Coordinates;
            for (int mountingFace = 0; mountingFace < 6; mountingFace++) {
                if (m_subsystemElectricity.GetElectricElement(coordinates.X, coordinates.Y, coordinates.Z, mountingFace)
                    is MoonTerminalElectricElement element) {
                    BindElectricElement(element);
                    return true;
                }
            }
            return false;
        }

        void QueuePulseRelease(int index) {
            if (!TryBindElectricElement()) {
                return;
            }
            int delay = Math.Max(1, m_pulseReleaseCircuitSteps[index] - m_electricElement.CircuitStep);
            m_electricElement.QueueSimulation(delay);
        }

        void NotifyCircuitChanged() {
            if (!TryBindElectricElement()) {
                return;
            }
            m_electricElement.QueueSimulation();
            m_electricElement.QueueConnectedNeighbors();
        }
    }
}
