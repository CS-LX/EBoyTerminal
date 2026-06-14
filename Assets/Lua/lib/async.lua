--- 协程调度辅助：周期任务、轮询等待。依赖全局 spawn 与 coroutine.yield。
local util = require("lib.util")

local M = {}

--- 每隔 interval 秒在独立协程中调用 fn（fn 内应自行 yield 时勿嵌套阻塞主协程）。
function M.every(interval, fn)
  spawn(function()
    while true do
      fn()
      coroutine.yield(interval)
    end
  end)
end

--- 每 n 个 tick 调用一次 fn。
function M.everyTick(ticks, fn)
  spawn(function()
    while true do
      fn()
      coroutine.yield("ticks", ticks or 1)
    end
  end)
end

--- 轮询 predicate()，为 true 时返回 true；超时（秒）返回 false。
function M.waitUntil(predicate, timeoutSeconds)
  timeoutSeconds = timeoutSeconds or 30
  local elapsed = 0
  local step = 0.05
  while not predicate() do
    if elapsed >= timeoutSeconds then
      return false
    end
    coroutine.yield(step)
    elapsed = elapsed + step
  end
  return true
end

--- 简单防抖：同一 key 在 cooldown 秒内只执行一次 fn。
local debounceState = {}

function M.debounce(key, cooldownSeconds, fn)
  local now = debounceState[key]
  if now and now > 0 then
    return false
  end
  debounceState[key] = cooldownSeconds
  fn()
  spawn(function()
    while debounceState[key] and debounceState[key] > 0 do
      local dt = 0.05
      coroutine.yield(dt)
      debounceState[key] = debounceState[key] - dt
      if debounceState[key] <= 0 then
        debounceState[key] = nil
      end
    end
  end)
  return true
end

--- 带退避的重试：最多 attempts 次，间隔 interval 秒。
function M.retry(fn, attempts, interval)
  attempts = attempts or 3
  interval = interval or 0.2
  local lastError
  for _ = 1, attempts do
    local ok, result = util.try(fn)
    if ok then
      return true, result
    end
    lastError = result
    coroutine.yield(interval)
  end
  return false, lastError
end

return M
