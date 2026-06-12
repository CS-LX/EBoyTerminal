# 嘉豪的终端 — 文档

| 文档 | 说明 |
| --- | --- |
| [策划案.md](策划案.md) | 模组玩法基调、定位与方向（大方向，非实现细案） |
| [CodeBoxWidget实现指南.md](CodeBoxWidget实现指南.md) | 脚本编辑器控件重构：FCTB 架构蒸馏 + SC2 落地路径 |

## 代码结构（脚手架）

| 路径 | 说明 |
| --- | --- |
| `EBoyTerminalLoader.cs` | ModLoader 入口 |
| `EBoyTerminalMod.cs` | 模组运行时初始化 |
| `Runtime/LuaScriptHost.cs` | MoonSharp Lua 宿主 |
| `Peripherals/` | IE2 设备外设接口与注册表 |
| `Blocks/MoonTerminalBlock.cs` | 月之终端占位方块（箱子外观 + 铁块属性） |
| `Widgets/CodeBoxWidget.cs` | 脚本多行编辑框（待重构：行号 + Lua 高亮） |
| `Dialogs/MoonTerminalScriptDialog.cs` | 终端脚本编辑对话框 |
| `EBoyTerminal.csv` | 方块数据表（宿主 `BlocksManager.LoadBlocksData`） |
