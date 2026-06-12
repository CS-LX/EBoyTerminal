using Engine;
using Game;

namespace EBoyTerminal;

public class EBoyTerminalLoader : ModLoader {
    public override void __ModInitialize() {
        base.__ModInitialize();
        EBoyTerminalMod.Initialize();
    }
}