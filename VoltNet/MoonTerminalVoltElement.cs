using Engine;
using Game;
using SCIENEW.VoltNet;

namespace EBoyTerminal.VoltNet {
    public class MoonTerminalVoltElement : VoltElement {
        public MoonTerminalVoltElement(SubsystemVoltNet subsystemVoltNet, SubsystemTerrain subsystemTerrain, Point3 position, int blockValue)
            : base(subsystemVoltNet, subsystemTerrain, position, blockValue) { }

        public override float GetPL(float standardPL, CellFace cellFace) => -standardPL;

        public override float GetUL(float standardUL, CellFace cellFace) => -standardUL;
    }
}
