using Engine;
using EBoyTerminal.Peripherals;
using EBoyTerminal.Runtime;

namespace EBoyTerminal;

/// <summary>模组入口侧初始化（由 <see cref="EBoyTerminalLoader"/> 调用）。</summary>
public static class EBoyTerminalMod {
    public const string PackageName = "com.eboy.terminal";

    public static PeripheralRegistry Peripherals { get; } = new();

    public static void Initialize() {
        Peripherals.Clear();
        LuaSystemApiRegistry.DiscoverContributors();
        Log.Information("[EBoyTerminal] runtime scaffold ready (per-entity Lua via ComponentLuaScriptHost).");
    }

    internal static void ResetForTests() {
        LuaSystemApiRegistry.ResetForTests();
    }
}
