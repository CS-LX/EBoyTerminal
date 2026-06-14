--- 宿主 Game.ElectricConnectorDirection（Top/Left/Bottom/Right/In）。
local M = {}

M.Top = 0
M.Left = 1
M.Bottom = 2
M.Right = 3
M.In = 4

M.ALL = { M.Top, M.Left, M.Bottom, M.Right, M.In }

local NAMES = {
  [M.Top] = "top",
  [M.Left] = "left",
  [M.Bottom] = "bottom",
  [M.Right] = "right",
  [M.In] = "in",
}

local ALIASES = {
  TOP = M.Top,
  LEFT = M.Left,
  BOTTOM = M.Bottom,
  RIGHT = M.Right,
  IN = M.In,
  BACK = M.In,
}

function M.name(connector)
  return NAMES[connector] or ("ElectricConnectorDirection(" .. tostring(connector) .. ")")
end

function M.isValid(connector)
  return type(connector) == "number" and connector >= M.Top and connector <= M.In and connector == math.floor(connector)
end

function M.assertValid(connector)
  if not M.isValid(connector) then
    error("invalid ElectricConnectorDirection: " .. tostring(connector), 2)
  end
  return connector
end

--- 解析编号或名称："top" / "LEFT" / 0 等。
function M.parse(label)
  if type(label) == "number" then
    return M.assertValid(label)
  end
  local text = tostring(label):lower():gsub("%s+", "")
  if text:match("^%d+$") then
    return M.assertValid(tonumber(text))
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
  error("unknown ElectricConnectorDirection: " .. tostring(label), 2)
end

return M
