namespace EBoyTerminal.Runtime;

/// <summary>玩家可见的 Lua 沙箱生命周期状态。</summary>
public enum LuaMachineState {
    Stopped,
    Ready,
    Running,
    Error
}
