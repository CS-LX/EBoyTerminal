--- 终端日志：带级别前缀，可选附带世界时间戳。
local world = require("sys.world")

local M = {}

local LEVELS = {
  debug = "DBG",
  info = "INF",
  warn = "WRN",
  error = "ERR",
}

local showTimestamp = true

function M.setTimestamp(enabled)
  showTimestamp = enabled ~= false
end

local function prefix(levelTag)
  if not showTimestamp then
    return "[" .. levelTag .. "] "
  end
  local day = world.getDay()
  local tod = world.getTimeOfDay()
  local hour = math.floor(tod * 24) % 24
  local minute = math.floor((tod * 24 - hour) * 60)
  return string.format("[D%.0f %02d:%02d %s] ", day, hour, minute, levelTag)
end

local function write(levelKey, ...)
  local tag = LEVELS[levelKey] or levelKey
  local parts = {}
  local argc = select("#", ...)
  for i = 1, argc do
    parts[i] = tostring(select(i, ...))
  end
  terminal.print(prefix(tag) .. table.concat(parts, "\t"))
end

function M.debug(...) write("debug", ...) end
function M.info(...) write("info", ...) end
function M.warn(...) write("warn", ...) end
function M.error(...) write("error", ...) end

--- 单行状态栏：适合循环监控。
function M.status(label, value)
  terminal.write(prefix("INF") .. tostring(label) .. ": " .. tostring(value))
end

return M
