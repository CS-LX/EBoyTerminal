--- 终端六向 CellFace 常量与名称（与 SC2 CellFace.m_faceToVector3 一致）。
--- face 0–5：0 +Z, 1 +X, 2 -Z, 3 -X, 4 +Y 顶, 5 -Y 底。
local M = {}

M.POS_Z = 0
M.POS_X = 1
M.NEG_Z = 2
M.NEG_X = 3
M.POS_Y = 4
M.NEG_Y = 5

M.ALL = { 0, 1, 2, 3, 4, 5 }

local NAMES = {
  [0] = "+Z",
  [1] = "+X",
  [2] = "-Z",
  [3] = "-X",
  [4] = "+Y",
  [5] = "-Y",
}

local OPPOSITE = {
  [0] = 2, [2] = 0,
  [1] = 3, [3] = 1,
  [4] = 5, [5] = 4,
}

function M.name(face)
  return NAMES[face] or ("face" .. tostring(face))
end

function M.opposite(face)
  return OPPOSITE[face]
end

function M.isValid(face)
  return type(face) == "number" and face >= 0 and face <= 5 and face == math.floor(face)
end

function M.assertValid(face)
  if not M.isValid(face) then
    error("face must be integer 0-5, got " .. tostring(face), 2)
  end
  return face
end

--- 按名称解析："+Z" / "pos_z" / "4" 等。
function M.parse(label)
  if type(label) == "number" then
    return M.assertValid(label)
  end
  local text = tostring(label):upper():gsub("%s+", "")
  if text:match("^%d+$") then
    return M.assertValid(tonumber(text))
  end
  for face, name in pairs(NAMES) do
    if text == name or text == name:gsub("+", "POS_"):gsub("-", "NEG_") then
      return face
    end
  end
  local aliases = {
    POS_Z = 0, POSX = 1, POS_X = 1,
    NEG_Z = 2, NEGZ = 2,
    NEG_X = 3, NEGX = 3,
    POS_Y = 4, POSY = 4, TOP = 4,
    NEG_Y = 5, NEGY = 5, BOTTOM = 5,
  }
  local face = aliases[text]
  if face ~= nil then
    return face
  end
  error("unknown face label: " .. tostring(label), 2)
end

return M
