local M = {}

M.name = "EBoyTerminal"
M.api = 1

function M.label()
  return M.name .. " v" .. tostring(M.api)
end

return M
