using Engine;
using Game;

namespace EBoyTerminal;

public class EBoyTerminalLoader : ModLoader
{
    public override void __ModInitialize()
    {
        base.__ModInitialize();
        Log.Information("[EBoyTerminal] 嘉豪的终端 (E-Boy's Terminal) loaded.");
    }
}
