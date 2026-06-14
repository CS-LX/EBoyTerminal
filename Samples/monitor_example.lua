--- 示例：世界监控 + 电路快照（粘贴到月之终端运行，需供电）。
local log = require("lib.log")
local world = require("lib.sys.world")
local electric = require("lib.terminal.electric")
local screen = require("lib.terminal.screen")
local util = require("lib.util")

terminal.clear()
log.info(world.snapshot())
if screen.isLinked() then
  log.info(string.format("CRT %dx%d", screen.countColumns(), screen.countRows()))
else
  log.info("CRT not linked")
end

while true do
  if electric.isReady() then
    log.status("IO", electric.formatSnapshot())
  else
    log.warn("unpowered")
  end
  util.sleep(2)
end
