using Engine;
using Game;
using GameEntitySystem;
using SCIENEW.Utils;
using SCIENEW.VoltNet;
using TemplatesDatabase;

namespace EBoyTerminal {
    public class SubsystemMoonTerminalBlockBehavior : SubsystemBlockBehavior {
        public const string EntityTemplateName = "MoonTerminal";

        SubsystemTerrain m_subsystemTerrain;
        SubsystemVoltNet m_subsystemVoltNet;

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<MoonTerminalBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
            m_subsystemVoltNet = Project.FindSubsystem<SubsystemVoltNet>(throwOnError: true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            BlockEntityUtils.CreateBlockEntity(m_subsystemTerrain, EntityTemplateName, new Point3(x, y, z));
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            BlockEntityUtils.RemoveBlockEntity(m_subsystemTerrain, new Point3(x, y, z));
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            if (!BlockEntityUtils.GetBlockEntity(m_subsystemTerrain, new Point3(x, y, z), out _)) {
                OnBlockAdded(value, 0, x, y, z);
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            if (componentMiner.ComponentPlayer == null) {
                return false;
            }
            Point3 point = raycastResult.CellFace.Point;
            if (!BlockEntityUtils.GetBlockEntity(m_subsystemTerrain, point, out ComponentBlockEntity blockEntity)) {
                return false;
            }
            ComponentMoonTerminal terminal = blockEntity.Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            if (m_subsystemVoltNet.GetVoltElement(point)?.CurrentWorkState != WorkState.Active) {
                componentMiner.ComponentPlayer.ComponentGui.DisplaySmallMessage(
                    LanguageUtils.GetText(terminal, "NoPowerInteract"),
                    Color.White,
                    blinking: true,
                    playNotificationSound: true);
                return true;
            }
            DialogsManager.ShowDialog(
                componentMiner.ComponentPlayer.GuiWidget,
                new MoonTerminalScriptDialog(terminal, componentMiner.ComponentPlayer));
            AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            return true;
        }
    }
}
