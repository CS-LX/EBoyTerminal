using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 向 <c>terminal.sys</c> 贡献只读系统 API。在任意已加载模组程序集中实现本接口（非 abstract 类），
/// 由 <see cref="LuaSystemApiRegistry.DiscoverContributors"/> 自动发现并实例化，无需修改 Host。
/// </summary>
public interface ILuaSystemApiContributor {
    void Contribute(LuaSystemApiBuildContext context, IDictionary<string, DynValue> members);
}
