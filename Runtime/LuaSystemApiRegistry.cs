using Engine;
using Engine.Serialization;
using MoonSharp.Interpreter;
using System.Reflection;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 系统级 Lua API 注册表：扫描各模组程序集中的 <see cref="ILuaSystemApiContributor"/> 实现并聚合为 <c>terminal.sys</c>。
/// </summary>
public static class LuaSystemApiRegistry {
    static readonly List<ILuaSystemApiContributor> s_contributors = [];
    static readonly List<Assembly> s_scannedAssemblies = [];

    public static void DiscoverContributors() {
        HashSet<Assembly> pending = new();
        foreach (Assembly assembly in TypeCache.LoadedAssemblies) {
            if (!TypeCache.IsKnownSystemAssembly(assembly)) {
                pending.Add(assembly);
            }
        }
        pending.Add(typeof(ILuaSystemApiContributor).Assembly);

        foreach (Assembly assembly in pending) {
            if (s_scannedAssemblies.Contains(assembly)) {
                continue;
            }
            ScanAssembly(assembly);
            s_scannedAssemblies.Add(assembly);
        }
    }

    static void ScanAssembly(Assembly assembly) {
        foreach (TypeInfo definedType in assembly.DefinedTypes) {
            if (definedType.IsAbstract || definedType.IsInterface) {
                continue;
            }
            if (!typeof(ILuaSystemApiContributor).IsAssignableFrom(definedType)) {
                continue;
            }
            try {
                if (Activator.CreateInstance(definedType.AsType()) is ILuaSystemApiContributor contributor) {
                    s_contributors.Add(contributor);
                }
            }
            catch (Exception ex) {
                Log.Error($"[EBoyTerminal] failed to create {definedType.FullName}");
                Log.Error(ex);
            }
        }
    }

    internal static void ResetForTests() {
        s_contributors.Clear();
        s_scannedAssemblies.Clear();
    }

    public static IReadOnlyDictionary<string, DynValue> BuildMembers(LuaSystemApiBuildContext context) {
        ArgumentNullException.ThrowIfNull(context);
        Dictionary<string, DynValue> members = new(StringComparer.Ordinal);
        foreach (ILuaSystemApiContributor contributor in s_contributors) {
            contributor.Contribute(context, members);
        }
        return members;
    }
}
