--- 接线侧编号，对应宿主 Game.ElectricConnectorDirection（Top=0 … In=4）。
local M = {}

M.top = 0
M.left = 1
M.bottom = 2
M.right = 3
M.in_ = 4
M.back = 4

M.t = M.top
M.l = M.left
M.b = M.bottom
M.r = M.right
M.i = M.in_

M.all = { M.top, M.left, M.bottom, M.right, M.in_ }

local NAMES = {
  [M.top] = "top",
  [M.left] = "left",
  [M.bottom] = "bottom",
  [M.right] = "right",
  [M.in_] = "in",
}

local ALIASES = {
  TOP = M.top,
  LEFT = M.left,
  BOTTOM = M.bottom,
  RIGHT = M.right,
  IN = M.in_,
  BACK = M.back,
}

function M.name(connector)
  return NAMES[connector] or ("connector(" .. tostring(connector) .. ")")
end

function M.check(connector)
  if type(connector) ~= "number" or connector < M.top or connector > M.in_ or connector ~= math.floor(connector) then
    error("invalid connector: " .. tostring(connector), 2)
  end
  return connector
end

--- 解析编号或名称："top" / "LEFT" / 0 等。
function M.parse(label)
  if type(label) == "number" then
    return M.check(label)
  end
  local text = tostring(label):lower():gsub("%s+", "")
  if text:match("^%d+$") then
    return M.check(tonumber(text))
  end
  for value, name in pairs(NAMES) do
    if text == name then
      return value
    end
  end
  local connector = ALIASES[text:upper()]
  if connector ~= nil then
    return connector
  end
  error("unknown connector: " .. tostring(label), 2)
end

return M
