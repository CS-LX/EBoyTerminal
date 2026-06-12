using System.Collections;

namespace EBoyTerminal.Peripherals;

/// <summary>已连接外设的注册表；终端方块接入 IE2 设备时在此登记。</summary>
public sealed class PeripheralRegistry : IEnumerable<IPeripheral>
{
    readonly Dictionary<string, IPeripheral> m_byLuaName = new(StringComparer.Ordinal);

    public int Count => m_byLuaName.Count;

    public void Register(IPeripheral peripheral)
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        if (string.IsNullOrWhiteSpace(peripheral.LuaName)) {
            throw new ArgumentException("Peripheral LuaName is required.", nameof(peripheral));
        }
        m_byLuaName[peripheral.LuaName] = peripheral;
    }

    public bool TryGet(string luaName, out IPeripheral? peripheral) =>
        m_byLuaName.TryGetValue(luaName, out peripheral);

    public void Clear() => m_byLuaName.Clear();

    public IEnumerator<IPeripheral> GetEnumerator() => m_byLuaName.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
