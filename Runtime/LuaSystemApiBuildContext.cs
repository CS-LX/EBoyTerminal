using GameEntitySystem;

namespace EBoyTerminal.Runtime;

/// <summary>构建 <c>terminal.sys</c> 时的上下文。</summary>
public sealed class LuaSystemApiBuildContext {
    public required LuaScriptApiBuildContext ApiContext { get; init; }

    public required Entity TerminalEntity { get; init; }

    public Project Project => ApiContext.Project;
}
