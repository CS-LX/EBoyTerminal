--- sys.version 封装：保留 C# getter，并补充 CC 风格 read* 别名。
local raw = require("sys.version")

local M = {}

function M.getGameVersion() return raw.getGameVersion() end
function M.getApiVersion() return raw.getApiVersion() end
function M.getTerminalModVersion() return raw.getTerminalModVersion() end

M.readGameVersion = M.getGameVersion
M.readApiVersion = M.getApiVersion
M.readTerminalModVersion = M.getTerminalModVersion

function M.label()
  return string.format(
    "SC %s | API %s | EBoy %s",
    M.getGameVersion(),
    M.getApiVersion(),
    M.getTerminalModVersion()
  )
end

return M
