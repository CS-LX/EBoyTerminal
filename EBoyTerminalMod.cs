using Engine;
using EBoyTerminal.Peripherals;
using EBoyTerminal.Runtime;

namespace EBoyTerminal;

/// <summary>模组入口侧初始化（由 <see cref="EBoyTerminalLoader"/> 调用）。</summary>
public static class EBoyTerminalMod {
    public const string PackageName = "com.eboy.terminal";

    public static PeripheralRegistry Peripherals { get; } = new();

    public static LuaScriptHost ScriptHost { get; } = new();

    public static void Initialize() {
        Peripherals.Clear();
        ScriptHost.Reset();
        Log.Information("[EBoyTerminal] runtime scaffold ready (Lua via MoonSharp).");
    }
}