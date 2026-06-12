# 嘉豪的终端 — 文档

| 文档 | 说明 |
| --- | --- |
| [策划案.md](策划案.md) | 模组玩法基调、定位与方向（大方向，非实现细案） |
| [CodeBoxWidget实现指南.md](CodeBoxWidget实现指南.md) | 脚本编辑器控件重构：FCTB 架构蒸馏 + SC2 落地路径 |

## CI（GitHub Actions）

本仓库为 IE2 附属模组，源码 `using SCIENEW` / `ProjectReference` 依赖 monorepo 内 `SCIENEW` 工程。Workflow 会：

1. 检出 [CS-LX/Industrial-Era-2](https://github.com/CS-LX/Industrial-Era-2)（`NEXT` 分支，`submodules: recursive`）
2. 用当前 commit 覆盖 `Addons/EBoyTerminal`
3. `dotnet restore`（含 SCIENEW 与其 `MiniBinaryXmlTool` 预构建工具）后 `dotnet build -p:ObfuscateGen=false`（跳过 IE2 的 GEN 混淆链）与打包 `.scmod`

本地 monorepo 开发路径须保持 `industrial-era-2/Addons/EBoyTerminal` 与 `industrial-era-2/SCIENEW` 同级。

### Private 主仓：配置 `IE2_REPO_TOKEN`

[Industrial-Era-2](https://github.com/CS-LX/Industrial-Era-2) 为 **private** 时，默认 `GITHUB_TOKEN` 无法跨仓检出。须在本仓库 **Settings → Secrets and variables → Actions** 添加：

| Secret | 说明 |
| --- | --- |
| `IE2_REPO_TOKEN` | 对 `CS-LX/Industrial-Era-2` 有 **读** 权限的 PAT（classic 勾 `repo`，或 fine-grained 只读该仓库） |

同一 GitHub 组织下也可在 **Organization secrets** 配置后授权给 `EBoyTerminal` 仓库。

**限制**：来自 fork 的 PR 默认拿不到该 secret，CI 会跳过/失败；主仓分支 push 与 `workflow_dispatch` 正常。

若不想维护 PAT，可关闭 GitHub Actions，仅在 Gitee monorepo（`industrial-era-2`）内构建。

## 代码结构（脚手架）

| 路径 | 说明 |
| --- | --- |
| `EBoyTerminalLoader.cs` | ModLoader 入口 |
| `EBoyTerminalMod.cs` | 模组运行时初始化 |
| `Runtime/LuaScriptHost.cs` | MoonSharp Lua 解释器封装（每实体一份） |
| `Runtime/LuaMachine.cs` | 协作式 Lua 虚拟机：`Load` / `Start` / `Stop` / `Tick`，含 `spawn` / `print`；延迟使用 MoonSharp 原生 `coroutine.yield(seconds)` 或 `coroutine.yield("ticks", n)` |
| `Components/ComponentLuaScriptHost.cs` | 实体级 Lua 运行时 Component |
| `Components/ComponentMoonTerminal.cs` | 月之终端：脚本持久化 + 组合 `ComponentLuaScriptHost` |
| `Components/ComponentMoonTerminalScreenProvider.cs` | 月之终端屏幕输出（`IScreenProviderComponent`，左上角绘字） |
| `Peripherals/` | IE2 设备外设接口与注册表 |
| `Blocks/MoonTerminalBlock.cs` | 月之终端占位方块（箱子外观 + 铁块属性） |
| `Widgets/CodeBoxWidget.cs` | 通用代码编辑框（行号、滚动、可选语法高亮） |
| `Widgets/LuaCodeBoxWidget.cs` | Lua 语法高亮版 `CodeBoxWidget` |
| `Dialogs/MoonTerminalScriptDialog.cs` | 终端脚本编辑对话框 |
| `EBoyTerminal.csv` | 方块数据表（宿主 `BlocksManager.LoadBlocksData`） |
