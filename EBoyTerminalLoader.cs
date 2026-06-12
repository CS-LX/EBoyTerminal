using Game;

namespace EBoyTerminal {
    public class EBoyTerminalLoader : ModLoader {
        public override void __ModInitialize() {
            base.__ModInitialize();
            ModsManager.RegisterHook("BlocksInitalized", this);
            EBoyTerminalMod.Initialize();
        }

        public override void BlocksInitalized() {
            if (!BlocksManager.m_categories.Contains("EBoyTerminal")) {
                BlocksManager.m_categories.Add("EBoyTerminal");
            }
        }
    }
}