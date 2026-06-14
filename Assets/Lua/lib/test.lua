--- 轻量自测框架（参考 Samples/smoke_test.lua），用于玩家脚本内联测试。
local M = {}

local pass, fail, skip = 0, 0, 0
local results = {}

function M.reset()
  pass, fail, skip = 0, 0, 0
  results = {}
end

function M.ok(name)
  pass = pass + 1
  results[#results + 1] = { status = "pass", name = name }
  terminal.print("[PASS] " .. name)
end

function M.fail(name, detail)
  fail = fail + 1
  local line = "[FAIL] " .. name .. (detail and (": " .. tostring(detail)) or "")
  results[#results + 1] = { status = "fail", name = name, detail = detail }
  terminal.print(line)
end

function M.skip(name, reason)
  skip = skip + 1
  results[#results + 1] = { status = "skip", name = name, detail = reason }
  terminal.print("[SKIP] " .. name .. (reason and (": " .. tostring(reason)) or ""))
end

function M.assertTrue(condition, name)
  if condition then
    M.ok(name)
  else
    M.fail(name)
  end
end

function M.assertEqual(expected, actual, name)
  if expected == actual then
    M.ok(name)
  else
    M.fail(name, "expected " .. tostring(expected) .. ", got " .. tostring(actual))
  end
end

function M.run(name, fn)
  local ok, err = pcall(fn)
  if ok then
    M.ok(name)
  else
    M.fail(name, err)
  end
end

function M.summary()
  local line = string.format("RESULT: %d pass, %d fail, %d skip", pass, fail, skip)
  terminal.print(line)
  if fail == 0 then
    terminal.print("ALL OK")
  else
    terminal.print("TESTS FAILED")
  end
  return fail == 0, pass, fail, skip
end

function M.results()
  return results
end

return M
