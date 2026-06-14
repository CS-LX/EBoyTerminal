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

    readonly Dictionary<int, int> m_connectorFaceToCellFace = new();

    ComponentMoonTerminalElectric? m_component;

    public MoonTerminalElectricElement(SubsystemElectricity subsystemElectricity, int x, int y, int z)
        : base(subsystemElectricity, BuildCellFaces(x, y, z)) {
        m_point = new Point3(x, y, z);
        m_subsystemBlockEntities = subsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
    }

    public void QueueSimulation() {
        SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 1);
    }

    public void QueueConnectedNeighbors() {
        SubsystemElectricity.QueueElectricElementConnectionsForSimulation(this, SubsystemElectricity.CircuitStep + 1);
    }

    public override void OnAdded() {
        base.OnAdded();
        RebuildConnectorMap();
    }

    public override void OnConnectionsChanged() {
        base.OnConnectionsChanged();
        RebuildConnectorMap();
    }

    public override float GetOutputVoltage(int connectorFace) {
        EnsureComponent();
        if (m_component == null) {
            return 0f;
        }
        if (m_connectorFaceToCellFace.TryGetValue(connectorFace, out int connectionFace)) {
            return m_component.GetOutputVoltage(ToApiFace(connectionFace));
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
        for (int apiFace = 0; apiFace < 6; apiFace++) {
            int connectionFace = ToConnectionFace(apiFace);
            float voltage = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.CellFace.Face != connectionFace) {
                    continue;
                }
                if (connection.ConnectorType == ElectricConnectorType.Output
                    || connection.NeighborConnectorType == 0) {
                    continue;
                }
                voltage = Math.Max(
                    voltage,
                    connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
            }
            changed |= m_component.SetInputReading(apiFace, voltage);
        }
        m_component.AdvancePulses();
        return changed;
    }

    void RebuildConnectorMap() {
        m_connectorFaceToCellFace.Clear();
        foreach (ElectricConnection connection in Connections) {
            m_connectorFaceToCellFace[connection.ConnectorFace] = connection.CellFace.Face;
        }
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

    static int ToConnectionFace(int apiFace) => CellFace.OppositeFace(apiFace);
}
