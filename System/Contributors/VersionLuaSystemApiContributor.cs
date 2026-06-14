using Game;
using MoonSharp.Interpreter;
using EBoyTerminal.Runtime;

namespace EBoyTerminal.System.Contributors;

/// <summary>版本信息（对标 CC <c>os.version</c> / OC 组件版本查询）。</summary>
public sealed class VersionLuaSystemApiContributor : ILuaSystemApiContributor {
    public string ModuleName => "sys.version";

    public void Contribute(LuaScriptApiBuildContext context, IDictionary<string, DynValue> members) {
        AddReader(members, "getGameVersion", context, static (_, _) => DynValue.NewString(ModsManager.ShortGameVersion));
        AddReader(members, "getApiVersion", context, static (_, _) => DynValue.NewString(ModsManager.APIVersionString));
        AddReader(members, "getTerminalModVersion", context, static (_, _) => DynValue.NewString(ResolveTerminalModVersion()));
    }

    static void AddReader(
        IDictionary<string, DynValue> members,
        string name,
        LuaScriptApiBuildContext api,
        Func<ScriptExecutionContext, CallbackArguments, DynValue> handler) {
        members[name] = api.Callback(handler);
    }

    static string ResolveTerminalModVersion() {
        foreach (ModEntity modEntity in ModsManager.ModList) {
            if (modEntity.modInfo.PackageName == EBoyTerminalMod.PackageName) {
                return modEntity.modInfo.Version;
            }
        }
        return "unknown";
    }
}
