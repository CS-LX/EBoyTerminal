using Engine;
using Game;
using GameEntitySystem;

namespace EBoyTerminal.Electric;

/// <summary>
/// 月之终端电路元素。与宿主门电路一致：单安装面 + 旋转，<see cref="ElectricConnectorDirection"/> 相对接线。
/// </summary>
public sealed class MoonTerminalElectricElement : ElectricElement {
    const int DirectionCount = 5;

    readonly Point3 m_point;

    readonly SubsystemBlockEntities m_subsystemBlockEntities;

    ComponentMoonTerminalElectric? m_component;

    int m_direction;

    public MoonTerminalElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int direction)
        : base(subsystemElectricity, cellFace) {
        m_point = cellFace.Point;
        m_direction = direction;
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
        ElectricConnectorDirection? direction = SubsystemElectricity.GetConnectorDirection(4, m_direction, connectorFace);
        if (!direction.HasValue) {
            return 0f;
        }
        return m_component.GetOutputVoltage(direction.Value);
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
        Span<float> inputVoltages = stackalloc float[DirectionCount];
        int rotation = m_direction;
        int mountingFace = CellFaces[0].Face;
        foreach (ElectricConnection connection in Connections) {
            if (!CanReadInputFromNeighbor(connection.ConnectorType, connection.NeighborConnectorType)) {
                continue;
            }
            ElectricConnectorDirection? direction = SubsystemElectricity.GetConnectorDirection(
                mountingFace,
                rotation,
                connection.ConnectorFace
            );
            if (!direction.HasValue) {
                continue;
            }
            int index = (int)direction.Value;
            inputVoltages[index] = Math.Max(
                inputVoltages[index],
                connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
        }
        for (int index = 0; index < DirectionCount; index++) {
            changed |= m_component.SetInputReading((ElectricConnectorDirection)index, inputVoltages[index]);
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

    internal static bool CanReadInputFromNeighbor(ElectricConnectorType connectorType, ElectricConnectorType neighborConnectorType)
        => connectorType != ElectricConnectorType.Output
            && neighborConnectorType != ElectricConnectorType.Input;

    public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) => false;
}
