using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 向 <c>require</c> 可加载的系统模块贡献只读 API。在任意已加载模组程序集中实现本接口（非 abstract 类），
/// 由 <see cref="LuaSystemApiRegistry.DiscoverContributors"/> 自动发现并实例化。
/// </summary>
public interface ILuaSystemApiContributor {
    /// <summary>模块名，如 <c>sys.version</c>；与 <c>require(moduleName)</c> 一致。</summary>
    string ModuleName { get; }

    void Contribute(LuaScriptApiBuildContext context, IDictionary<string, DynValue> members);
}
