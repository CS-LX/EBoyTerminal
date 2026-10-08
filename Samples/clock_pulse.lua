--- 脉冲时钟：从月之终端某一接线侧周期性输出高电平脉冲。
--- 前提：终端已 VoltNet 供电；目标侧（默认 top）已接电路。
--- 可调 CLOCK.connector / periodTicks / pulseCircuitTicks / mode。

local async = require("lib.async")
local log = require("lib.log")
local electric = require("lib.terminal.electric")
local util = require("lib.util")

local CLOCK = {
  connector = electric.top,   -- top / left / bottom / right / in_，或 "top" 等字符串
  periodTicks = 10,           -- 相邻两次脉冲起点间隔（游戏 tick，约 1/60 s）
  pulseCircuitTicks = 2,      -- 单次脉冲高电平持续时长（电路仿真 tick）
  mode = "pulse",             -- "pulse"=内置 pulse API；"toggle"=write 方波
}

terminal.clear()

if not electric.isReady() then
  log.warn("终端未供电，电路 IO 不可用")
  return
end

electric.low(CLOCK.connector)
log.info(string.format(
  "clock start: %s mode=%s period=%d pulse=%d",
  electric.name(CLOCK.connector),
  CLOCK.mode,
  CLOCK.periodTicks,
  CLOCK.pulseCircuitTicks
))

if CLOCK.mode == "toggle" then
  -- 方波：稳定 write 高低交替；脉宽/周期均为游戏 tick，适合驱动对时序要求不严的逻辑。
  spawn(function()
    while true do
      electric.high(CLOCK.connector)
      util.ticks(math.max(1, math.floor(CLOCK.periodTicks / 2)))
      electric.low(CLOCK.connector)
      util.ticks(math.max(1, CLOCK.periodTicks - math.floor(CLOCK.periodTicks / 2)))
    end
  end)
else
  -- 推荐：electric.pulse 脉宽按电路 tick 计时，与门电路/计数器同步；间隔用游戏 tick 粗略控制频率。
  spawn(function()
    while true do
      electric.pulseHigh(CLOCK.connector, CLOCK.pulseCircuitTicks, 1)
      util.ticks(CLOCK.periodTicks)
    end
  end)
end

-- 可选：用 lib.async 封装同等逻辑
-- async.everyTick(CLOCK.periodTicks, function()
--   electric.pulseHigh(CLOCK.connector, CLOCK.pulseCircuitTicks, 1)
-- end)
