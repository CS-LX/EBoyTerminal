--- EBoyTerminal 内置 Lua 标准库元信息。
local M = {}

M.name = "EBoyTerminal"
M.api = 1

--- 已打包的标准库模块（require 路径）。
M.modules = {
  "lib.util",
  "lib.async",
  "lib.log",
  "lib.test",
  "lib.sys.world",
  "lib.sys.version",
  "lib.terminal.dir",
  "lib.terminal.electric",
  "lib.terminal.screen",
}

function M.label()
  return M.name .. " stdlib v" .. tostring(M.api)
end

return M
