using GameEntitySystem;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 由实体上的 <see cref="Component"/> 实现，向 <see cref="ComponentLuaScriptHost"/> 提供 Lua API 片段。
/// </summary>
public interface ILuaScriptApiProvider {
    void ContributeLuaApi(LuaScriptApiBuildContext context);
}
