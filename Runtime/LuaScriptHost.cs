using MoonSharp.Interpreter;

namespace EBoyTerminal.Runtime;

/// <summary>Lua 脚本宿主（MoonSharp）；后续终端方块与 IE2 外设 API 在此注册。</summary>
public sealed class LuaScriptHost
{
    Script? m_script;

    public void Reset() => m_script = new Script(CoreModules.Preset_SoftSandbox);

    public DynValue Execute(string source, string? chunkName = null)
    {
        m_script ??= new Script(CoreModules.Preset_SoftSandbox);
        return m_script.DoString(source, null, chunkName ?? "terminal");
    }

    public void RegisterGlobal(string name, object value)
    {
        m_script ??= new Script(CoreModules.Preset_SoftSandbox);
        m_script.Globals[name] = UserData.Create(value);
    }
}
