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
    /// 月之终端逻辑电路 IO。Lua/API 的 <c>face</c> 与贴图/接线柱 CellFace 一致；
    /// 电路 connection 面在 <see cref="MoonTerminalElectricElement"/> 内经 OppositeFace 映射后再读写。
    /// </summary>
    public class ComponentMoonTerminalElectric : Component, ILuaScriptApiProvider {
        public const string OutputVoltagesKey = "ElectricOutputVoltages";

        const int FaceCount = 6;
        const float VoltageEpsilon = 0.0001f;

        readonly float[] m_inputVoltages = new float[FaceCount];
        readonly float[] m_stableOutputVoltages = new float[FaceCount];
        readonly float[] m_pulseVoltages = new float[FaceCount];
        readonly int[] m_pulseReleaseCircuitSteps = new int[FaceCount];

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

        public float GetOutputVoltage(int face) {
            if (!IsValidFace(face) || !IsIoEnabled) {
                return 0f;
            }
            if (HasActivePulse(face)) {
                 return m_pulseVoltages[face];
            }
            return m_stableOutputVoltages[face];
        }

        public bool SetInputReading(int face, float voltage) {
            if (!IsValidFace(face)) {
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
            for (int face = 0; face < FaceCount; face++) {
                if (m_inputVoltages[face] != 0f) {
                    m_inputVoltages[face] = 0f;
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
            for (int face = 0; face < FaceCount; face++) {
                int releaseStep = m_pulseReleaseCircuitSteps[face];
                if (releaseStep <= 0 || circuitStep < releaseStep) {
                    continue;
                }
                float pulseVoltage = m_pulseVoltages[face];
                m_pulseReleaseCircuitSteps[face] = 0;
                m_pulseVoltages[face] = 0f;
                if (Math.Abs(pulseVoltage - m_stableOutputVoltages[face]) > VoltageEpsilon) {
                    changed = true;
                }
            }
            return changed;
        }

        public bool TryReadInput(int face, out float voltage) {
            voltage = 0f;
            if (!IsValidFace(face)) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = m_inputVoltages[face];
            }
            return true;
        }

        public bool TryReadOutput(int face, out float voltage) {
            voltage = 0f;
            if (!IsValidFace(face)) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = GetOutputVoltage(face);
            }
            return true;
        }

        public bool TryWriteFace(int face, float voltage, out string? error) {
            error = null;
            if (!IsValidFace(face)) {
                error = "face must be 0-5";
                return false;
            }
            if (!IsIoEnabled) {
                error = "terminal is unpowered";
                return false;
            }
            voltage = ClampVoltage(voltage);
            float previousVoltage = GetOutputVoltage(face);
            CancelPulse(face);
            m_stableOutputVoltages[face] = voltage;
            if (Math.Abs(previousVoltage - GetOutputVoltage(face)) > VoltageEpsilon) {
                NotifyCircuitChanged();
            }
            return true;
        }

        public bool TryPulseFace(int face, float voltage, int ticks, out string? error) {
            error = null;
            if (!IsValidFace(face)) {
                error = "face must be 0-5";
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
            voltage = ClampVoltage(voltage);
            float previousVoltage = GetOutputVoltage(face);
            m_pulseVoltages[face] = voltage;
            m_pulseReleaseCircuitSteps[face] = m_electricElement!.CircuitStep + ticks + 1;
            QueuePulseRelease(face);
            if (Math.Abs(previousVoltage - voltage) > VoltageEpsilon) {
                NotifyCircuitChanged();
            }
            return true;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddSubTable("electric", new Dictionary<string, DynValue> {
                ["listFaces"] = context.Callback((executionContext, _) => BuildFaceTable(executionContext)),
                ["readInput"] = context.Callback((_, args) => {
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryReadInput(face, out float voltage)) {
                        throw new ScriptRuntimeException("invalid face");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["readOutput"] = context.Callback((_, args) => {
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryReadOutput(face, out float voltage)) {
                        throw new ScriptRuntimeException("invalid face");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["isHigh"] = context.Callback((_, args) => {
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryReadInput(face, out float voltage)) {
                        throw new ScriptRuntimeException("invalid face");
                    }
                    return DynValue.NewBoolean(ElectricElement.IsSignalHigh(voltage));
                }),
                ["readLevel"] = context.Callback((_, args) => {
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryReadInput(face, out float voltage)) {
                        throw new ScriptRuntimeException("invalid face");
                    }
                    return DynValue.NewNumber((int)MathF.Round(voltage * 15f));
                }),
                ["write"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.write(face, value) requires face and value");
                    }
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryWriteFace(face, (float)args[1].Number, out error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    return DynValue.Nil;
                }),
                ["pulse"] = context.Callback((_, args) => {
                    if (args.Count < 2) {
                        throw new ScriptRuntimeException("electric.pulse(face, ticks[, value]) requires face and ticks");
                    }
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    float voltage = args.Count >= 3 ? (float)args[2].Number : 1f;
                    if (!TryPulseFace(face, voltage, (int)args[1].Number, out error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    return DynValue.Nil;
                }),
                ["isEnabled"] = context.Callback((_, _) => DynValue.NewBoolean(IsIoEnabled)),
            });
        }

        static bool TryParseFaceArg(CallbackArguments args, int index, out int face, out string? error) {
            face = -1;
            error = null;
            if (args.Count <= index) {
                error = "face argument required";
                return false;
            }
            if (args[index].Type != DataType.Number) {
                error = "face must be a number 0-5";
                return false;
            }
            face = (int)args[index].Number;
            if (!IsValidFace(face)) {
                error = "face must be 0-5";
                return false;
            }
            return true;
        }

        static DynValue BuildFaceTable(ScriptExecutionContext executionContext) {
            Table table = new(executionContext.OwnerScript);
            for (int face = 0; face < FaceCount; face++) {
                table.Set(face + 1, DynValue.NewNumber(face));
            }
            return DynValue.NewTable(table);
        }

        bool ClearOutputs() {
            bool changed = ClearPulses();
            for (int face = 0; face < FaceCount; face++) {
                if (m_stableOutputVoltages[face] != 0f) {
                    m_stableOutputVoltages[face] = 0f;
                    changed = true;
                }
            }
            return changed;
        }

        bool ClearPulses() {
            bool changed = false;
            for (int face = 0; face < FaceCount; face++) {
                if (m_pulseReleaseCircuitSteps[face] == 0) {
                    continue;
                }
                if (Math.Abs(m_pulseVoltages[face] - m_stableOutputVoltages[face]) > VoltageEpsilon) {
                    changed = true;
                }
                m_pulseReleaseCircuitSteps[face] = 0;
                m_pulseVoltages[face] = 0f;
            }
            return changed;
        }

        void CancelPulse(int face) {
            m_pulseReleaseCircuitSteps[face] = 0;
            m_pulseVoltages[face] = 0f;
        }

        void LoadOutputVoltages(string serialized) {
            Array.Clear(m_stableOutputVoltages);
            if (string.IsNullOrWhiteSpace(serialized)) {
                return;
            }
            foreach (string segment in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                string[] parts = segment.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length != 2
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int face)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float voltage)
                    || !IsValidFace(face)) {
                    continue;
                }
                m_stableOutputVoltages[face] = ClampVoltage(voltage);
            }
        }

        string SerializeOutputVoltages() {
            List<string> segments = new();
            for (int face = 0; face < FaceCount; face++) {
                if (m_stableOutputVoltages[face] <= VoltageEpsilon) {
                    continue;
                }
                segments.Add(string.Create(CultureInfo.InvariantCulture, $"{face}:{m_stableOutputVoltages[face]:0.###}"));
            }
            return string.Join(';', segments);
        }

        static float ClampVoltage(float voltage) => Math.Clamp(voltage, 0f, 1f);

        static bool IsValidFace(int face) => face is >= 0 and < FaceCount;

        bool HasActivePulse(int face) => m_pulseReleaseCircuitSteps[face] > 0;

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
            for (int face = 0; face < FaceCount; face++) {
                if (m_subsystemElectricity.GetElectricElement(coordinates.X, coordinates.Y, coordinates.Z, face)
                    is MoonTerminalElectricElement element) {
                    BindElectricElement(element);
                    return true;
                }
            }
            return false;
        }

        void QueuePulseRelease(int face) {
            if (!TryBindElectricElement()) {
                return;
            }
            int delay = Math.Max(1, m_pulseReleaseCircuitSteps[face] - m_electricElement.CircuitStep);
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
