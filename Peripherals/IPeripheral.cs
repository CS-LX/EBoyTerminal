namespace EBoyTerminal.Peripherals;

/// <summary>可挂接到终端的 IE2 设备外设（对标 ComputerCraft Peripheral）。</summary>
public interface IPeripheral {
    string Type { get; }

    /// <summary>供 Lua 侧调用的方法表名，例如 <c>furnace</c>。</summary>
    string LuaName { get; }
}