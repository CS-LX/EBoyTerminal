using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>
/// 向 <c>terminal.sys</c> 贡献只读系统 API。新增能力时实现本接口并在模组初始化时注册，无需修改 Host。
/// </summary>
public interface ILuaSystemApiContributor {
    void Contribute(LuaSystemApiBuildContext context, IDictionary<string, DynValue> members);
}
