-- EBoyTerminal API 冒烟测试（含 electric）
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

if terminal.electric.enabled() then ok("electric.enabled powered") else bad("electric.enabled powered") end

local ports = terminal.electric.ports()
if type(ports) == "table" and #ports == 5 then ok("electric.ports()") else bad("electric.ports()", "#=" .. tostring(#ports)) end

local wrote, writeErr = pcall(function() terminal.electric.write("back", 1) end)
if wrote then
  ok("electric.write(back,1)")
  terminal.electric.write("back", 0)
else
  bad("electric.write(back,1)", writeErr)
end

if terminal.electric.isInput("back") and terminal.electric.isOutput("back") then
  ok("electric port capabilities")
else
  bad("electric port capabilities")
end

terminal.print(string.format("RESULT: %d pass, %d fail, %d skip", pass, fail, skip))
if fail == 0 then terminal.print("SMOKE OK") else terminal.print("SMOKE FAILED") end
