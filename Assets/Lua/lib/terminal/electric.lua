--- 电路 IO 高层封装，基于 terminal.electric（face 0–5）。
local face = require("lib.terminal.face")
local util = require("lib.util")

local M = {}

local function api()
  return terminal.electric
end

function M.isReady()
  return api().isEnabled()
end

function M.requirePower()
  if not M.isReady() then
    error("terminal is unpowered", 2)
  end
end

function M.high(targetFace, level)
  M.requirePower()
  api().write(face.assertValid(targetFace), level or 1)
end

function M.low(targetFace)
  M.requirePower()
  api().write(face.assertValid(targetFace), 0)
end

function M.pulseHigh(targetFace, tickCount, level)
  M.requirePower()
  api().pulse(face.assertValid(targetFace), tickCount or 1, level or 1)
end

function M.readInput(targetFace)
  return api().readInput(face.assertValid(targetFace))
end

function M.readOutput(targetFace)
  return api().readOutput(face.assertValid(targetFace))
end

function M.isHigh(targetFace)
  return api().isHigh(face.assertValid(targetFace))
end

function M.readLevel(targetFace)
  return api().readLevel(face.assertValid(targetFace))
end

--- CC 风格别名：read 指邻侧输入。
M.read = M.readInput

--- 读取六面输入，返回 { [face] = voltage }。
function M.readAllInputs()
  local values = {}
  for i = 1, #face.ALL do
    local f = face.ALL[i]
    values[f] = M.readInput(f)
  end
  return values
end

--- 读取六面本机输出。
function M.readAllOutputs()
  local values = {}
  for i = 1, #face.ALL do
    local f = face.ALL[i]
    values[f] = M.readOutput(f)
  end
  return values
end

--- 批量写输出；map 键为 face 编号。
function M.writeAll(map)
  M.requirePower()
  for f, voltage in pairs(map) do
    api().write(face.assertValid(f), voltage)
  end
end

function M.clearOutputs()
  M.writeAll({ [0] = 0, [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 })
end

--- 等待某面输入变高；timeoutSeconds 默认 30，超时返回 false。
function M.waitHigh(targetFace, timeoutSeconds)
  targetFace = face.assertValid(targetFace)
  timeoutSeconds = timeoutSeconds or 30
  local elapsed = 0
  local step = 0.05
  while not api().isHigh(targetFace) do
    if elapsed >= timeoutSeconds then
      return false
    end
    coroutine.yield(step)
    elapsed = elapsed + step
  end
  return true
end

--- 等待某面输入变低。
function M.waitLow(targetFace, timeoutSeconds)
  targetFace = face.assertValid(targetFace)
  timeoutSeconds = timeoutSeconds or 30
  local elapsed = 0
  local step = 0.05
  while api().isHigh(targetFace) do
    if elapsed >= timeoutSeconds then
      return false
    end
    coroutine.yield(step)
    elapsed = elapsed + step
  end
  return true
end

--- 格式化六面状态为一行，便于 terminal.print 调试。
function M.formatSnapshot()
  local chunks = {}
  for i = 1, #face.ALL do
    local f = face.ALL[i]
    local level = M.readLevel(f)
    local out = M.readOutput(f)
    chunks[#chunks + 1] = string.format(
      "%s in=%d out=%s",
      face.name(f),
      level,
      util.formatPercent(out, 0)
    )
  end
  return table.concat(chunks, " | ")
end

return M
