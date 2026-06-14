using Engine;
using Game;
using GameEntitySystem;

namespace EBoyTerminal.Electric;

/// <summary>
/// 月之终端电路元素。Lua/API 与贴图/接线柱共用 SC CellFace；
/// 电路 <see cref="ElectricConnection.CellFace"/>.Face 与接线面相差 <see cref="CellFace.OppositeFace"/>，此处统一转换。
/// </summary>
public sealed class MoonTerminalElectricElement : ElectricElement {
    readonly Point3 m_point;

    readonly SubsystemBlockEntities m_subsystemBlockEntities;

    ComponentMoonTerminalElectric? m_component;

    public MoonTerminalElectricElement(SubsystemElectricity subsystemElectricity, int x, int y, int z)
        : base(subsystemElectricity, BuildCellFaces(x, y, z)) {
        m_point = new Point3(x, y, z);
        m_subsystemBlockEntities = subsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
    }

    public int CircuitStep => SubsystemElectricity.CircuitStep;

    public void QueueSimulation(int stepsFromNow = 1) {
        SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + Math.Max(1, stepsFromNow));
    }

    public void QueueConnectedNeighbors() {
        SubsystemElectricity.QueueElectricElementConnectionsForSimulation(this, SubsystemElectricity.CircuitStep + 1);
    }

    public override void OnAdded() {
        base.OnAdded();
        QueueSimulation();
    }

    public override void OnConnectionsChanged() {
        base.OnConnectionsChanged();
        if (m_component != null) {
            m_component.ClearInputReadings();
        }
        QueueSimulation();
    }

    public override float GetOutputVoltage(int connectorFace) {
        EnsureComponent();
        if (m_component == null) {
            return 0f;
        }
        foreach (ElectricConnection connection in Connections) {
            if (connection.ConnectorFace == connectorFace) {
                return m_component.GetOutputVoltage(ToApiFace(connection.CellFace.Face));
            }
        }
        return 0f;
    }

    public override bool Simulate() {
        EnsureComponent();
        if (m_component == null) {
            return false;
        }
        if (!m_component.IsIoEnabled) {
            return m_component.ClearInputReadings();
        }
        bool changed = false;
        Span<float> inputVoltages = stackalloc float[6];
        foreach (ElectricConnection connection in Connections) {
            if (!CanReadInputFromNeighbor(connection.ConnectorType, connection.NeighborConnectorType)) {
                continue;
            }
            int apiFace = ToApiFace(connection.CellFace.Face);
            inputVoltages[apiFace] = Math.Max(
                inputVoltages[apiFace],
                connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
        }
        for (int apiFace = 0; apiFace < 6; apiFace++) {
            changed |= m_component.SetInputReading(apiFace, inputVoltages[apiFace]);
        }
        changed |= m_component.AdvancePulses(SubsystemElectricity.CircuitStep);
        return changed;
    }

    void EnsureComponent() {
        if (m_component != null) {
            return;
        }
        ComponentBlockEntity? blockEntity = m_subsystemBlockEntities.GetBlockEntity(m_point.X, m_point.Y, m_point.Z);
        m_component = blockEntity?.Entity.FindComponent<ComponentMoonTerminalElectric>(throwOnError: false);
        m_component?.BindElectricElement(this);
    }

    static IEnumerable<CellFace> BuildCellFaces(int x, int y, int z) {
        for (int face = 0; face < 6; face++) {
            yield return new CellFace(x, y, z, face);
        }
    }

    /// <summary>电路 connection.CellFace → Lua/贴图 CellFace（六面均为 OppositeFace）。</summary>
    internal static int ToApiFace(int connectionCellFace) => CellFace.OppositeFace(connectionCellFace);

    internal static bool CanReadInputFromNeighbor(ElectricConnectorType connectorType, ElectricConnectorType neighborConnectorType)
        => connectorType != ElectricConnectorType.Output
            && neighborConnectorType != ElectricConnectorType.Input;
}
