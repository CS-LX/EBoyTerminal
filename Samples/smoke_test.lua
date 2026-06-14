-- EBoyTerminal API 冒烟测试（含 electric）
-- face 0-5 = 终端自身 CellFace（非邻块面）：0 +Z, 1 +X, 2 -Z, 3 -X, 4 +Y顶, 5 -Y底
-- 连 CRT、供电后粘贴运行；末尾 SMOKE OK 即通过。

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

if terminal.sys.readGameVersion() and terminal.sys.readApiVersion() then
  ok("sys.readGameVersion/readApiVersion")
else
  bad("sys.readGameVersion/readApiVersion")
end

local faces = terminal.electric.listFaces()
if type(faces) == "table" and #faces == 6 and faces[1] == 0 and faces[6] == 5 then
  ok("electric.listFaces()")
else
  bad("electric.listFaces()", "count=" .. tostring(#faces))
end

local wrote, writeErr = pcall(function() terminal.electric.write(0, 1) end)
if wrote then
  ok("electric.write(0,1)")
  terminal.electric.write(0, 0)
else
  bad("electric.write(0,1)", writeErr)
end

local pulsed, pulseErr = pcall(function() terminal.electric.pulse(0, 2) end)
if pulsed then ok("electric.pulse(0,2)") else bad("electric.pulse(0,2)", pulseErr) end

terminal.print(string.format("RESULT: %d pass, %d fail, %d skip", pass, fail, skip))
if fail == 0 then terminal.print("SMOKE OK") else terminal.print("SMOKE FAILED") end
