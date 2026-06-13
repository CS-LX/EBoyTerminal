using Engine;
using Game;
using GameEntitySystem;

namespace EBoyTerminal.Electric;

public sealed class MoonTerminalElectricElement : ElectricElement {
    readonly Point3 m_point;

    readonly SubsystemBlockEntities m_subsystemBlockEntities;

    ComponentMoonTerminalElectric? m_component;

    public MoonTerminalElectricElement(SubsystemElectricity subsystemElectricity, int x, int y, int z, int blockData)
        : base(subsystemElectricity, BuildCellFaces(x, y, z, blockData)) {
        m_point = new Point3(x, y, z);
        m_subsystemBlockEntities = subsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
    }

    public void QueueSimulation() {
        SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 1);
    }

    public override float GetOutputVoltage(int face) {
        EnsureComponent();
        return m_component?.GetOutputVoltage(face) ?? 0f;
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
        for (int face = 0; face < 6; face++) {
            float voltage = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.CellFace.Face != face) {
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
            changed |= m_component.SetInputReading(face, voltage);
        }
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

    static IEnumerable<CellFace> BuildCellFaces(int x, int y, int z, int blockData) {
        int facing = MoonTerminalElectricPorts.GetFacing(blockData);
        for (int face = 0; face < 6; face++) {
            if (face != facing) {
                yield return new CellFace(x, y, z, face);
            }
        }
    }
}
