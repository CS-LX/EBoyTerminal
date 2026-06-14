using Engine;
using Engine.Serialization;
using MoonSharp.Interpreter;
using System.Reflection;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 系统级 Lua 模块注册表：扫描 <see cref="ILuaSystemApiContributor"/> 并由 <see cref="LuaScriptHost"/> 的 <c>require</c> 解析。
/// </summary>
public static class LuaSystemApiRegistry {
    static readonly Dictionary<string, ILuaSystemApiContributor> s_contributors = new(StringComparer.Ordinal);
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
                if (Activator.CreateInstance(definedType.AsType()) is not ILuaSystemApiContributor contributor) {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(contributor.ModuleName)) {
                    Log.Warning($"[EBoyTerminal] {definedType.FullName} has empty ModuleName, skipped");
                    continue;
                }
                if (s_contributors.ContainsKey(contributor.ModuleName)) {
                    Log.Warning($"[EBoyTerminal] duplicate system module '{contributor.ModuleName}' from {definedType.FullName}, skipped");
                    continue;
                }
                s_contributors[contributor.ModuleName] = contributor;
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

    public static DynValue? TryBuildModule(string moduleName, LuaScriptApiBuildContext context, Script script) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(script);
        if (!s_contributors.TryGetValue(moduleName, out ILuaSystemApiContributor? contributor)) {
            return null;
        }
        Dictionary<string, DynValue> members = new(StringComparer.Ordinal);
        contributor.Contribute(context, members);
        Table table = new(script);
        foreach ((string key, DynValue value) in members) {
            table.Set(key, value);
        }
        return DynValue.NewTable(table);
    }
}
