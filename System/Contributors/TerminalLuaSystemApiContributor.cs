using Engine;
using Game;
using GameEntitySystem;
using MoonSharp.Interpreter;
using EBoyTerminal.Runtime;
using EBoyTerminal.Utils;

namespace EBoyTerminal.System.Contributors;

/// <summary>终端自身在世界中的只读信息。</summary>
public sealed class TerminalLuaSystemApiContributor : ILuaSystemApiContributor {
    public void Contribute(LuaSystemApiBuildContext context, IDictionary<string, DynValue> members) {
        LuaScriptApiBuildContext api = context.ApiContext;
        members["readPosition"] = api.Callback((executionContext, _) => {
            ComponentBlockEntity? blockEntity = context.TerminalEntity.FindComponent<ComponentBlockEntity>(throwOnError: false);
            if (blockEntity == null) {
                return LuaUtils.NewVector3(executionContext.OwnerScript, Vector3.Zero);
            }
            Vector3 position = new Vector3(blockEntity.Coordinates) + Vector3.One * 0.5f;
            return LuaUtils.NewVector3(executionContext.OwnerScript, position);
        });
    }
}
