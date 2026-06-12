using Engine;
using Game;
using GameEntitySystem;
using SCIENEW.Utils;
using TemplatesDatabase;

namespace EBoyTerminal {
    public class SubsystemMoonTerminalBlockBehavior : SubsystemBlockBehavior {
        public const string EntityTemplateName = "MoonTerminal";

        SubsystemTerrain m_subsystemTerrain;

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<MoonTerminalBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
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
            if (!BlockEntityUtils.GetBlockEntity(m_subsystemTerrain, raycastResult.CellFace.Point, out ComponentBlockEntity blockEntity)) {
                return false;
            }
            ComponentMoonTerminal terminal = blockEntity.Entity.FindComponent<ComponentMoonTerminal>(throwOnError: true);
            DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, new MoonTerminalScriptDialog(terminal));
            AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            return true;
        }
    }
}
