--- CRT / 屏幕链接状态辅助。
local M = {}

local function api()
  return terminal.screen
end

function M.isLinked()
  return api().isLinked()
end

function M.countRows()
  return api().countRows()
end

function M.countColumns()
  return api().countColumns()
end

function M.readPosition()
  return api().readPosition()
end

--- 未链接 CRT 时返回 0×0。
function M.size()
  return M.countColumns(), M.countRows()
end

--- 按列宽截断单行（近似 TerminalScreenLayout 行为，便于自绘状态行）。
function M.truncateLine(text, maxColumns)
  if maxColumns == nil or maxColumns <= 0 then
    return text or ""
  end
  text = text or ""
  if #text <= maxColumns then
    return text
  end
  if maxColumns <= 3 then
    return text:sub(1, maxColumns)
  end
  return text:sub(1, maxColumns - 3) .. "..."
end

local function eachLine(text)
  text = text or ""
  local pos = 1
  local len = #text
  return function()
    if pos > len then
      return nil
    end
    local nl = text:find("\n", pos, true)
    local line
    if nl then
      line = text:sub(pos, nl - 1)
      pos = nl + 1
    else
      line = text:sub(pos)
      pos = len + 1
    end
    return line
  end
end

--- 将多行文本按列宽截断后 terminal.print。
function M.printWrapped(text)
  local cols = M.countColumns()
  if not M.isLinked() or cols <= 0 then
    terminal.print(text or "")
    return
  end
  for line in eachLine(text) do
    terminal.print(M.truncateLine(line, cols))
  end
end

return M
