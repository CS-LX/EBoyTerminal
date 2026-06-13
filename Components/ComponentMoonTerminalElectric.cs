using System.Globalization;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using TemplatesDatabase;
using EBoyTerminal.Electric;
using EBoyTerminal.Runtime;

namespace EBoyTerminal {
    /// <summary>月之终端逻辑电路 IO（CellFace 0–5）与 <c>terminal.electric.*</c> Lua API。</summary>
    public class ComponentMoonTerminalElectric : Component, ILuaScriptApiProvider {
        public const string OutputVoltagesKey = "ElectricOutputVoltages";

        const float VoltageEpsilon = 0.0001f;

        readonly float[] m_inputVoltages = new float[6];
        readonly float[] m_outputVoltages = new float[6];
        readonly int[] m_pulseTicksRemaining = new int[6];
        readonly float[] m_pulseReleaseVoltage = new float[6];

        ComponentMoonTerminal m_terminal = null!;
        MoonTerminalElectricElement? m_electricElement;

        public bool IsIoEnabled => Terminal.IsPowered;

        ComponentMoonTerminal Terminal =>
            m_terminal ?? Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
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
            NotifyCircuitChanged();
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
                NotifyCircuitChanged();
            }
        }

        public bool TryReadInput(int face, out float voltage) {
            voltage = 0f;
            if (face is < 0 or > 5) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = m_inputVoltages[face];
            }
            return true;
        }

        public bool TryReadOutput(int face, out float voltage) {
            voltage = 0f;
            if (face is < 0 or > 5) {
                return false;
            }
            if (IsIoEnabled) {
                voltage = m_outputVoltages[face];
            }
            return true;
        }

        public bool TryWriteFace(int face, float voltage, out string? error) {
            error = null;
            if (face is < 0 or > 5) {
                error = "face must be 0-5";
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
            NotifyCircuitChanged();
            return true;
        }

        public bool TryPulseFace(int face, float voltage, int ticks, out string? error) {
            error = null;
            if (face is < 0 or > 5) {
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
            voltage = ClampVoltage(voltage);
            m_pulseReleaseVoltage[face] = 0f;
            m_pulseTicksRemaining[face] = ticks;
            if (Math.Abs(m_outputVoltages[face] - voltage) <= VoltageEpsilon) {
                NotifyCircuitChanged();
                return true;
            }
            m_outputVoltages[face] = voltage;
            NotifyCircuitChanged();
            return true;
        }

        public void ContributeLuaApi(LuaScriptApiBuildContext context) {
            context.AddSubTable("electric", new Dictionary<string, DynValue> {
                ["faces"] = context.Callback((executionContext, _) => BuildFaceTable(executionContext)),
                ["read"] = context.Callback((_, args) => {
                    if (!TryParseFaceArg(args, 0, out int face, out string? error)) {
                        throw new ScriptRuntimeException(error ?? "invalid face");
                    }
                    if (!TryReadInput(face, out float voltage)) {
                        throw new ScriptRuntimeException("invalid face");
                    }
                    return DynValue.NewNumber(voltage);
                }),
                ["output"] = context.Callback((_, args) => {
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
                ["level"] = context.Callback((_, args) => {
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
                ["enabled"] = context.Callback((_, _) => DynValue.NewBoolean(IsIoEnabled)),
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
            if (face is < 0 or > 5) {
                error = "face must be 0-5";
                return false;
            }
            return true;
        }

        static DynValue BuildFaceTable(ScriptExecutionContext executionContext) {
            Table table = new(executionContext.OwnerScript);
            for (int face = 0; face < 6; face++) {
                table.Set(face + 1, DynValue.NewNumber(face));
            }
            return DynValue.NewTable(table);
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

        void NotifyCircuitChanged() {
            if (m_electricElement == null) {
                return;
            }
            m_electricElement.QueueSimulation();
            m_electricElement.QueueConnectedNeighbors();
        }
    }
}
