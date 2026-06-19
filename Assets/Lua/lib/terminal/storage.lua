--- terminal.storage 的 Lua 友好封装。
local M = {}

local function api()
  return terminal.storage
end

function M.get(key)
  return api().get(key)
end

function M.set(key, value)
  api().set(key, value)
end

function M.remove(key)
  api().remove(key)
end

function M.clear()
  api().clear()
end

function M.has(key)
  return api().has(key)
end

function M.keys()
  return api().keys()
end

return M
