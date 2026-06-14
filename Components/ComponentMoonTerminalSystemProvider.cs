using EBoyTerminal.Runtime;
using GameEntitySystem;

namespace EBoyTerminal;

/// <summary>向 Lua 暴露 <c>terminal.sys</c> 只读系统 API。</summary>
public class ComponentMoonTerminalSystemProvider : Component, ILuaScriptApiProvider {
    public void ContributeLuaApi(LuaScriptApiBuildContext context) {
        LuaSystemApiBuildContext sysContext = new() {
            ApiContext = context,
            TerminalEntity = Entity
        };
        context.AddSubTable("sys", LuaSystemApiRegistry.BuildMembers(sysContext));
    }
}
