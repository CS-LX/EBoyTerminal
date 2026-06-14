using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 系统级 Lua API 注册表：对扩展开放（Register），对修改关闭（Host 只读聚合）。
/// </summary>
public static class LuaSystemApiRegistry {
    static readonly List<ILuaSystemApiContributor> s_contributors = [];

    public static void Register(ILuaSystemApiContributor contributor) {
        ArgumentNullException.ThrowIfNull(contributor);
        s_contributors.Add(contributor);
    }

    internal static void ResetForTests() => s_contributors.Clear();

    public static IReadOnlyDictionary<string, DynValue> BuildMembers(LuaSystemApiBuildContext context) {
        ArgumentNullException.ThrowIfNull(context);
        Dictionary<string, DynValue> members = new(StringComparer.Ordinal);
        foreach (ILuaSystemApiContributor contributor in s_contributors) {
            contributor.Contribute(context, members);
        }
        return members;
    }
}
