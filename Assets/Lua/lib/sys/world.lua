--- sys.world 的 Lua 友好封装：时间格式化、天气摘要等。
local raw = require("sys.world")

local M = {}

function M.getWorldName() return raw.getWorldName() end
function M.getWorldSeed() return raw.getWorldSeed() end
function M.getGameMode() return raw.getGameMode() end
function M.getElapsedSeconds() return raw.getElapsedSeconds() end
function M.getDay() return raw.getDay() end
function M.getTimeOfDay() return raw.getTimeOfDay() end
function M.getSeason() return raw.getSeason() end
function M.getIsPrecipitating() return raw.getIsPrecipitating() end
function M.getPrecipitationIntensity() return raw.getPrecipitationIntensity() end
function M.getTickSeconds() return raw.getTickSeconds() end

--- 将 0–1 的 TimeOfDay 格式化为 24 小时制 HH:MM。
function M.formatClock(timeOfDay)
  local tod = timeOfDay
  if tod == nil then
    tod = M.getTimeOfDay()
  end
  local totalMinutes = math.floor(tod * 24 * 60 + 0.5) % (24 * 60)
  local hour = math.floor(totalMinutes / 60)
  local minute = totalMinutes % 60
  return string.format("%02d:%02d", hour, minute)
end

--- 粗粒度昼夜判断（TimeOfDay 为 0–1 循环；不读取季节修正的 Dawn/Dusk 阈值）。
function M.isDaytime()
  local tod = M.getTimeOfDay()
  return tod >= 0.23 and tod < 0.77
end

function M.isNight()
  return not M.isDaytime()
end

function M.weatherSummary()
  if M.getIsPrecipitating() then
    local intensity = M.getPrecipitationIntensity()
    if intensity >= 0.66 then
      return "heavy rain"
    end
    if intensity >= 0.33 then
      return "rain"
    end
    return "drizzle"
  end
  return "clear"
end

--- 单行世界快照，适合日志或 CRT 首行。
function M.snapshot()
  return string.format(
    "%s | day %.0f %s | %s %s | %.1fs",
    M.getWorldName(),
    M.getDay(),
    M.formatClock(),
    M.getSeason(),
    M.weatherSummary(),
    M.getElapsedSeconds()
  )
end

return M
