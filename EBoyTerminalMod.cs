using Engine;
using EBoyTerminal.Peripherals;
using EBoyTerminal.Runtime;
using EBoyTerminal.System.Contributors;

namespace EBoyTerminal;

/// <summary>模组入口侧初始化（由 <see cref="EBoyTerminalLoader"/> 调用）。</summary>
public static class EBoyTerminalMod {
    public const string PackageName = "com.eboy.terminal";

    public static PeripheralRegistry Peripherals { get; } = new();

    static bool s_systemApisRegistered;

    public static void Initialize() {
        Peripherals.Clear();
        RegisterBuiltinSystemApis();
        Log.Information("[EBoyTerminal] runtime scaffold ready (per-entity Lua via ComponentLuaScriptHost).");
    }

    static void RegisterBuiltinSystemApis() {
        if (s_systemApisRegistered) {
            return;
        }
        LuaSystemApiRegistry.Register(new VersionLuaSystemApiContributor());
        LuaSystemApiRegistry.Register(new WorldLuaSystemApiContributor());
        LuaSystemApiRegistry.Register(new TerminalLuaSystemApiContributor());
        s_systemApisRegistered = true;
    }

    internal static void ResetForTests() {
        LuaSystemApiRegistry.ResetForTests();
        s_systemApisRegistered = false;
    }
}
