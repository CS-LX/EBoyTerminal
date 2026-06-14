-- EBoyTerminal API 冒烟测试（含 electric）
-- 接线侧：e.top / e.t / "top" / 0（对应宿主 ElectricConnectorDirection）
-- 连 CRT、供电后粘贴运行；末尾 SMOKE OK 即通过。

local e = require("lib.terminal.electric")

local pass, fail, skip = 0, 0, 0

local function ok(name)
  pass = pass + 1
  terminal.print("[PASS] " .. name)
end

local function bad(name, detail)
  fail = fail + 1
  terminal.print("[FAIL] " .. name .. (detail and (": " .. detail) or ""))
end

local function skipCase(name, reason)
  skip = skip + 1
  terminal.print("[SKIP] " .. name .. (reason and (": " .. reason) or ""))
end

terminal.clear()
terminal.print("=== EBoyTerminal Smoke Test ===")

if terminal.electric.isEnabled() then ok("electric.isEnabled powered") else bad("electric.isEnabled powered") end

local sysVersion = require "sys.version"
if sysVersion.readGameVersion() and sysVersion.readApiVersion() then
  ok("sys.version.readGameVersion/readApiVersion")
else
  bad("sys.version.readGameVersion/readApiVersion")
end

local connectors = terminal.electric.listDirections()
if type(connectors) == "table" and #connectors == 5 and connectors[1] == e.top and connectors[5] == e.in_ then
  ok("electric.listDirections()")
else
  bad("electric.listDirections()", "count=" .. tostring(#connectors))
end

local wrote, writeErr = pcall(function() terminal.electric.write(e.top, 1) end)
if wrote then
  ok("electric.write(top,1)")
  terminal.electric.write(e.top, 0)
else
  bad("electric.write(top,1)", writeErr)
end

local pulsed, pulseErr = pcall(function() terminal.electric.pulse("top", 2) end)
if pulsed then ok("electric.pulse('top',2)") else bad("electric.pulse('top',2)", pulseErr) end

if e.parse("right") == e.r and e.name(e.in_) == "in" then
  ok("electric.parse/name")
else
  bad("electric.parse/name")
end

terminal.print(string.format("RESULT: %d pass, %d fail, %d skip", pass, fail, skip))
if fail == 0 then terminal.print("SMOKE OK") else terminal.print("SMOKE FAILED") end
