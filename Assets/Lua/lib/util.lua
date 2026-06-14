--- 通用小工具：延时、数值与表操作。不依赖 terminal，可在任意脚本复用。
local M = {}

--- 让出协程若干秒（封装 MoonSharp 原生 yield）。
function M.sleep(seconds)
  coroutine.yield(seconds)
end

--- 让出协程若干游戏 tick（每帧一次 Update）。
function M.ticks(count)
  coroutine.yield("ticks", count or 1)
end

function M.clamp(value, minValue, maxValue)
  if value < minValue then return minValue end
  if value > maxValue then return maxValue end
  return value
end

function M.round(value)
  if value >= 0 then
    return math.floor(value + 0.5)
  end
  return math.ceil(value - 0.5)
end

function M.sign(value)
  if value > 0 then return 1 end
  if value < 0 then return -1 end
  return 0
end

function M.isEmpty(value)
  if value == nil then return true end
  if type(value) ~= "table" then return false end
  return next(value) == nil
end

function M.contains(list, item)
  for i = 1, #list do
    if list[i] == item then return true end
  end
  return false
end

function M.keys(map)
  local result = {}
  for key in pairs(map) do
    result[#result + 1] = key
  end
  return result
end

function M.copyShallow(source)
  local copy = {}
  for key, value in pairs(source) do
    copy[key] = value
  end
  return copy
end

local function trimString(text)
  return (text:gsub("^%s+", ""):gsub("%s+$", ""))
end

function M.trim(text)
  if text == nil then return "" end
  return trimString(tostring(text))
end

function M.split(text, delimiter)
  delimiter = delimiter or ","
  local parts = {}
  if text == nil or text == "" then
    return parts
  end
  local pattern = "(.-)" .. delimiter
  local last = 1
  local s, e, capture = text:find(pattern, 1)
  while s do
    parts[#parts + 1] = capture
    last = e + 1
    s, e, capture = text:find(pattern, last)
  end
  parts[#parts + 1] = text:sub(last)
  return parts
end

--- 安全调用 fn；失败时返回 false 与错误信息。
function M.try(fn, ...)
  return pcall(fn, ...)
end

--- 将 0–1 的小数格式化为百分比字符串。
function M.formatPercent(fraction, digits)
  digits = digits or 0
  local scale = 10 ^ digits
  local percent = M.round((fraction or 0) * 100 * scale) / scale
  if digits == 0 then
    return tostring(percent) .. "%"
  end
  return string.format("%." .. digits .. "f%%", percent)
end

return M
