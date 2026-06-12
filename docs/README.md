# 嘉豪的终端 — 文档

| 文档 | 说明 |
| --- | --- |
| [策划案.md](策划案.md) | 模组玩法基调、定位与方向（大方向，非实现细案） |

## 代码结构（脚手架）

| 路径 | 说明 |
| --- | --- |
| `EBoyTerminalLoader.cs` | ModLoader 入口 |
| `EBoyTerminalMod.cs` | 模组运行时初始化 |
| `Runtime/LuaScriptHost.cs` | MoonSharp Lua 宿主 |
| `Peripherals/` | IE2 设备外设接口与注册表 |
