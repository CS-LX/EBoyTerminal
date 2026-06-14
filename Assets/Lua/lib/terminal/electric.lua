--- 电路 IO；接线侧用 top/left/…、"top" 或 e.T 等短名。
local dir = require("lib.terminal.dir")
local util = require("lib.util")

local M = {}

M.top = dir.top
M.left = dir.left
M.bottom = dir.bottom
M.right = dir.right
M.in_ = dir.in_
M.back = dir.back
M.t = dir.t
M.l = dir.l
M.b = dir.b
M.r = dir.r
M.i = dir.i
M.all = dir.all
M.parse = dir.parse
M.name = dir.name
M.check = dir.check

local function api()
  return terminal.electric
end

local function resolveConnector(label)
  if type(label) == "string" then
    return dir.parse(label)
  end
  return dir.check(label)
end

function M.isReady()
  return api().isEnabled()
end

function M.requirePower()
  if not M.isReady() then
    error("terminal is unpowered", 2)
  end
end

function M.high(connector, level)
  M.requirePower()
  api().write(resolveConnector(connector), level or 1)
end

function M.low(connector)
  M.requirePower()
  api().write(resolveConnector(connector), 0)
end

function M.pulseHigh(connector, tickCount, level)
  M.requirePower()
  api().pulse(resolveConnector(connector), tickCount or 1, level or 1)
end

function M.readInput(connector)
  return api().readInput(resolveConnector(connector))
end

function M.readOutput(connector)
  return api().readOutput(resolveConnector(connector))
end

function M.isHigh(connector)
  return api().isHigh(resolveConnector(connector))
end

function M.readLevel(connector)
  return api().readLevel(resolveConnector(connector))
end

--- CC 风格别名：read 指邻侧输入。
M.read = M.readInput

--- 读取五路输入，返回 { [connector] = voltage }。
function M.readAllInputs()
  local values = {}
  for i = 1, #dir.all do
    local connector = dir.all[i]
    values[connector] = M.readInput(connector)
  end
  return values
end

--- 读取五路本机输出。
function M.readAllOutputs()
  local values = {}
  for i = 1, #dir.all do
    local connector = dir.all[i]
    values[connector] = M.readOutput(connector)
  end
  return values
end

--- 批量写输出；map 键为编号或名称（如 top / "right"）。
function M.writeAll(map)
  M.requirePower()
  for connector, voltage in pairs(map) do
    api().write(resolveConnector(connector), voltage)
  end
end

function M.clearOutputs()
  local cleared = {}
  for i = 1, #dir.all do
    cleared[dir.all[i]] = 0
  end
  M.writeAll(cleared)
end

function M.waitHigh(connector, timeoutSeconds)
  connector = resolveConnector(connector)
  timeoutSeconds = timeoutSeconds or 30
  local elapsed = 0
  local step = 0.05
  while not api().isHigh(connector) do
    if elapsed >= timeoutSeconds then
      return false
    end
    coroutine.yield(step)
    elapsed = elapsed + step
  end
  return true
end

function M.waitLow(connector, timeoutSeconds)
  connector = resolveConnector(connector)
  timeoutSeconds = timeoutSeconds or 30
  local elapsed = 0
  local step = 0.05
  while api().isHigh(connector) do
    if elapsed >= timeoutSeconds then
      return false
    end
    coroutine.yield(step)
    elapsed = elapsed + step
  end
  return true
end

function M.formatSnapshot()
  local chunks = {}
  for i = 1, #dir.all do
    local connector = dir.all[i]
    chunks[#chunks + 1] = string.format(
      "%s in=%d out=%s",
      dir.name(connector),
      M.readLevel(connector),
      util.formatPercent(M.readOutput(connector), 0)
    )
  end
  return table.concat(chunks, " | ")
end

return M
