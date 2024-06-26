﻿--[[
Selene, A Lua library for more convenient functional programming
Author: Vexatos
]]

--------
-- Utils
--------
local checkArg = function(n, have, ...)
  have = type(have)
  local function check(want, ...)
    if not want then
      return false
    else
      return have == want or check(...)
    end
  end

  if not check(...) then
    local msg = string.format("bad argument #%d (%s expected, got %s)",
      n, table.concat({ ... }, " or "), have)
    error(msg, 3)
  end
end

local function shallowcopy(orig)
  local copy
  if type(orig) == 'table' then
    copy = {}
    for k, v in pairs(orig) do
      copy[k] = v
    end
  else
    copy = orig
  end
  return copy
end

local function clamp(num, mn, mx)
  checkArg(1, num, "number")
  checkArg(2, mn, "number", "nil")
  checkArg(3, mx, "number", "nil")
  if not mn and mx then
    return math.min(num, mx)
  elseif mn and not mx then
    return math.max(num, mn)
  else
    return math.max(math.min(num, mx), mn)
  end
end

-- Returns the number of parameters on the function
local function parCount(obj, def)
  checkArg(1, obj, "table", "function")
  checkArg(2, def, "number", "nil")
  if type(obj) == "function" then
    return def
  end
  local m = getmetatable(obj)
  return (m and m.parCount) or def
end

-- Returns the table type or the type
local function tblType(obj)
  if type(obj) == "table" then
    local m = getmetatable(obj)
    return (m and m.ltype) or "table"
  end
  return type(obj)
end

local function isList_t(t)
  local c = 0
  for i in pairs(t) do
    if type(i) ~= "number" then
      return false, -1
    elseif i < 1 then
      return false, i
    end
    c = c + 1
  end
  return c == #t
end

local function isList(t)
  checkArg(1, t, "table")
  local tp = tblType(t)
  if tp == "list" or tp == "stringlist" or tp == "array" then
    return true
  elseif tp == "map" then
    return false, -1
  elseif tp == "table" then
    return isList_t(t)
  end
  return false, -2
end

local function getLen(t)
  local tp = tblType(t)
  if tp == "list" or tp == "stringlist" or tp == "array" then
    return t:len(), "list"
  elseif tp == "map" then
    return false, -1
  elseif tp == "table" and isList_t(t) then
    return #t, "table"
  end
  return false, -2
end

local function checkList(n, have)
  if not isList(have) then
    local msg = string.format("[Selene] bad argument #%d (list expected, got %s)", n, have)
    error(msg, 2)
  end
end

local function insert(tbl, fuzzyList, key, value)
  fuzzyList = fuzzyList or false
  if value then
    if fuzzyList then
      table.insert(tbl, value)
    else
      tbl[key] = value
    end
  else
    table.insert(tbl, key)
  end
end

local function mpairs(obj)
  if type(obj) == "table" and isList(obj) then
    return ipairs(obj)
  else
    return pairs(obj)
  end
end

local validMaps = { map = true, list = true, stringlist = true }

local function lpairs(obj)
  if validMaps[tblType(obj)] then
    return mpairs(obj)
  else
    return pairs(obj)
  end
end

local allMaps = { "map", "list", "stringlist" }
local allIterables = { "map", "list", "stringlist", "iterable", "array" }
local allIndexables = {"list", "stringlist", "iterable", "array" }

-- Returns true if the value is a valid type
local function isType(n, have, ...)
  have = tblType(have)
  local things = { ... }
  if #things == 0 then
    things = allIterables
  elseif #things == 1 and type(things[1]) == "table" then
    things = things[1]
  end
  for _, want in ipairs(things) do
    if have == want then
      return true
    end
  end
  return false
end

-- Errors if the value is not a valid type (list or map)
local function checkType(n, have, ...)
  if not isType(n, have, ...) then
    local msg = string.format("[Selene] bad argument #%d (%s expected, got %s)",
            n, table.concat(things, " or "), have)
    error(msg, 3)
  end
end

-- Errors if the value is not a function or does not have the required parameter count
local function checkFunc(n, have, ...)
  checkType(n, have, "function", "table")
  if type(have) == "function" then return end
  local haveParCount = parCount(have, nil)
  have = type(have)
  if not haveParCount then
    return
  end

  if #{ ... } == 0 then return end

  local level = 3

  local function check(want, ...)
    if not want then
      return false
    else
      checkArg(level, want, "number")
      level = level + 1
      return haveParCount == want or check(...)
    end
  end

  if not check(...) then
    local msg = string.format("[Selene] bad argument #%d (%s parameter(s) expected, got %s)",
      n, table.concat({ ... }, " or "), haveParCount)
    error(msg, 3)
  end
end

local function switch(o, ...)
  for i, f in ipairs({ ... }) do
    checkFunc(i + 1, f, 1, 0)
    if type(f) == "table" then
      if f.applies and f.applies(o) then
        return f._fnc(o)
      end
    else
      return f(o)
    end
  end
end

--------
-- Bulk data operations, using the new $ object
--------

local mt = {
  __call = function(tbl)
    return tbl._tbl
  end,
  __len = function(tbl)
    return #tbl._tbl
  end,
  __pairs = function(tbl)
    return pairs(tbl._tbl)
  end,
  __ipairs = function(tbl)
    return ipairs(tbl._tbl)
  end,
  __tostring = function(tbl)
    return tostring(tbl._tbl)
  end,
  __index = function(tbl, key)
    return tbl._tbl[key]
  end,
  __newindex = function(tbl, key, val)
    error("[Selene] attempt to insert value into " .. tblType(tbl), 2)
  end,
  ltype = "map"
}

local lmt = shallowcopy(mt)
lmt.ltype = "list"
lmt.islist = true
lmt.__ipairs = function(tbl)
  return function(tbl, i)
    i = i + 1
    if i <= #tbl then
      return i, tbl[i]
    end
  end, tbl, 0
end

local mdmt = shallowcopy(lmt)
mdmt.ltype = "array"
mdmt.islist = true

local function truef() return true end

local fmt = {
  __call = function(fnc, ...)
    if fnc.applies and not fnc.applies(...) then return end
    return fnc._fnc(...)
  end,
  __len = function(fnc)
    return parCount(fnc)
  end,
  __pairs = function(fnc)
    return pairs(fnc._fnc)
  end,
  __ipairs = function(fnc)
    return ipairs(fnc._fnc)
  end,
  __tostring = function(fnc)
    return tostring(fnc._fnc)
  end,
  __index = function(fnc, key)
    if key == "applies" then
      local fm = getmetatable(fnc)
      return (not fm.applies and truef) or fm.applies
    end
	--print(fnc._fnc, key, fnc)
	--local inf = debug.getinfo(fnc._fnc, "Sl")
	--print(inf.source, inf.linedefined)

    return fnc._fnc[key]
  end,
  __newindex = mt.__newindex,
  ltype = "function"
}

local cfmt = shallowcopy(fmt)
cfmt.__call = function(fnc, ...)
  local fm = getmetatable(fnc)
  for i, f in ipairs(fm._cfnc) do
    if f[1](...) then
      return f[2](...)
    end
  end
end

local _Table = {}
local _String = {}
local _Iterable = {}
local _Array = {}
local _Func = {}

local smt = shallowcopy(mt)
smt.ltype = "stringlist"
smt.islist = true
smt.__call = function(str)
  return table.concat(str._tbl)
end
smt.__tostring = smt.__call

local function inext(spl, i)
  local v = spl(i)
  if v == nil then
    return v
  else
    return i+1, v
  end
end

local imt = {
  __call = function(itr)
    local v = itr._spl(itr._i)
    itr._i = itr._i + 1
    return v
  end,
  __len = function(itr)
    error("[Selene] attempt to get length of " .. tblType(itr), 2)
  end,
  __pairs = function(itr)
    return inext, itr._spl, 1
  end,
  __ipairs = function(itr)
    return inext, itr._spl, 1
  end,
  __tostring = function(itr)
    return tostring(itr._spl)
  end,
  __index = function(itr, key)
    error("[Selene] attempt to index " .. tblType(itr), 2)
  end,
  __newindex = function(itr, key, val)
    error("[Selene] attempt to insert value into " .. tblType(itr), 2)
  end,
  ltype = "iterable"
}

--------
-- Initialization functions
--------
local function new(...)
  local t = ...
  if #{ ... } > 1 or type(t) ~= "table" then t = { ... } end
  t = t or {}
  local newObj = {}
  for i, j in pairs(_Table) do
    newObj[i] = j
  end
  newObj._tbl = t
  setmetatable(newObj, mt)
  return newObj
end

local function newOptional(...)
  return new({ ... })
end

local function _newStringList(s)
  s = s or {}
  local newObj = {}
  for i, j in pairs(_String) do
    newObj[i] = j
  end
  newObj._tbl = s
  setmetatable(newObj, smt)
  return newObj
end

local function newStringList(s)
  checkArg(1, s, "table", "nil")
  s = s or {}
  local newObj = {}
  for i, j in pairs(_String) do
    newObj[i] = j
  end
  newObj._tbl = {}
  for i = 1, #s do
    if not s[i] or type(s[i]) ~= "string" or #s[i] > 1 then
      error("[Selene] could not create list: bad table key: " .. i .. " is not a character", 2)
    end
    newObj._tbl[i] = s[i]
  end
  setmetatable(newObj, smt)
  return newObj
end

local function newString(s)
  checkArg(1, s, "string", "nil")
  s = s or ""
  local newObj = {}
  for i, j in pairs(_String) do
    newObj[i] = j
  end
  newObj._tbl = {}
  for i = 1, #s do
    newObj._tbl[i] = s:sub(i, i)
  end
  setmetatable(newObj, smt)
  return newObj
end

local function newList(...)
  local newObj = new(...)
  local s, i = isList(newObj._tbl)
  if not s then
    error("[Selene] could not create list: " .. (i and (i >= 0 and "bad table key: " .. i or (i == -1 and "parameter is not a list-like table" or "parameter is not a table")) or "table length does not match number of entries"), 2)
  end
  setmetatable(newObj, lmt)
  return newObj
end

local function _newListOrMap(t, islist)
  t = t or {}
  local newObj = {}
  for i, j in pairs(_Table) do
    newObj[i] = j
  end
  newObj._tbl = t
  setmetatable(newObj, islist and lmt or mt)
  return newObj
end

local function _newList(t)
  return _newListOrMap(t, true)
end

local function newListOrMap(...)
  local newObj = new(...)
  if isList(newObj._tbl) then
    setmetatable(newObj, lmt)
  end
  return newObj
end

local function newFunc(f, parCnt, applies)
  checkArg(1, f, "function")
  checkArg(2, parCnt, "number")
  if parCnt < 0 then
    error("[Selene] could not create function: bad parameter amount: " .. parCnt .. " is below 0", 2)
  end
  local newF = {}
  for i, j in pairs(_Func) do
    newF[i] = j
  end
  local fm = shallowcopy(fmt)
  newF._fnc = f
  newF.applies = applies
  fm.parCount = parCnt
  setmetatable(newF, fm)
  return newF
end

local function newIterable(spl)
  checkType(1, spl, "function")
  local newI = {}
  newI._spl = spl
  newI._i = 1
  for i, j in pairs(_Iterable) do
    newI[i] = j
  end
  setmetatable(newI, imt)
  return newI
end

local function newGeneric(...)
  local t = ...
  if #{ ... } > 1 then t = { ... } end
  if type(t) == "string" then
    return newString(t)
  elseif tblType(t) == "function" then
    return newIterable(t)
  else
    return newListOrMap(t)
  end
end

--------
-- Final metatable initialization
--------

mt.__concat = function(first, second)
  local fType = tblType(first)
  if fType == "map" and type(second) == "table" then
    local merged = shallowcopy(first._tbl)
    for k, v in lpairs(second) do
      merged[k] = v
    end
    return newListOrMap(merged)
  else
    local sType = tblType(second)
    error(string.format("[Selene] attempt to concatenate %s and %s (cannot insert %s into %s)",
      fType, sType, sType, fType), 2)
  end
end

local function concatOnCondition(first, second, cond)
  local fType = tblType(first)
  if (fType == "list" or fType == "stringlist") and cond(second) then
    local merged = shallowcopy(first._tbl)
    for _, v in ipairs(second) do
      table.insert(merged, v)
    end
    return _newList(merged)
  else
    local sType = tblType(second)
    error(string.format("[Selene] attempt to concatenate %s and %s (cannot insert %s into %s)",
      fType, sType, sType, fType), 2)
  end
end

lmt.__concat = function(first, second)
  return concatOnCondition(first, second, isList)
end

local function isStringList(val)
  return tblType(val) == "stringlist"
end

smt.__concat = function(first, second)
  return concatOnCondition(first, second, isStringList)
end

lmt.__add = function(first, second)
  local fType = tblType(first)
  if fType == "list" then
    local merged = shallowcopy(first._tbl)
    table.insert(merged, second)
    return _newList(merged)
  else
    local sType = tblType(second)
    error(string.format("[Selene] attempt to add %s and %s (cannot insert %s into %s)",
      fType, sType, sType, fType), 2)
  end
end

lmt.__index = function(tbl, key)
  if type(key) == "table" then
    local r = {}
    for i = key[1], key[2] do
      table.insert(r, tbl._tbl[i])
    end
    return _newList(r)
  else
    return tbl._tbl[key]
  end
end

fmt.__add = function(first, second)
  if type(first) == "table" and type(second) == "table" then
    local fm = getmetatable(first)
    local sm = getmetatable(second)
    if(fm and fm.ltype == "function" and sm and sm.ltype == "function") then
      local funcs = {}
      local pc
      local function insert(p, t)
        if not pc or pc == p then
          table.insert(funcs, t)
          pc = pc or p
        else
          error(string.format("[Selene] attempt to create composite function out of functions with different parameter counts %s and %s",
            pc, p), 3)
        end
      end
      if fm._cfnc then
        for i, f in ipairs(fm._cfnc) do
          insert(fm.parCount, f)
        end
      else
        insert(fm.parCount, {first.applies, first._fnc})
      end
      if sm._cfnc then
        for i, f in ipairs(sm._cfnc) do
          insert(sm.parCount, f)
        end
      else
        insert(sm.parCount, {second.applies, second._fnc})
      end
      local newC = {}
      local cm = shallowcopy(cfmt)
      cm._cfnc = funcs
      newC.applies = function(...)
        for i, f in ipairs(cm._cfnc) do
          if f[1](...) then
            return true
          end
        end
        return false
      end
      newC._fnc = function(...)
        return newC(...)
      end
      cm.parCount = pc
      setmetatable(newC, cm)
      return newC
    end
  end
  error(string.format("[Selene] attempt to create composite function out of %s and %s",
    tblType(first), tblType(second)), 2)
end

--------
-- Bulk data operations on tables
--------

-- local utility functions for more efficient code

local function wrap_dropfromleft(self, amt)
  return amt + 1, #self
end

local function wrap_dropfromright(self, amt)
  return 1, #self - amt
end

local function wrap_takefromleft(self, amt)
  return 1, amt
end

local function wrap_takefromright(self, amt)
  return #self - amt + 1, #self
end

local function wrap_returnsecond(i, j)
  return j
end

local function wrap_returnboth(i, j)
  return i, j
end

local function wrap_returnself(self)
  return self
end

local function wrap_returnempty(self)
  return _newList({})
end

local function checkParCnt(parCnt)
  if parCnt == 1 then
    return wrap_returnsecond
  else
    return wrap_returnboth
  end
end

-- Concatenates the entries of the table just like table.concat
local function tbl_concat(self, sep, i, j)
  return table.concat(self._tbl, sep, i, j)
end

local function tbl_foreach(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    f(parCnt(i, j))
  end
end

-- Iterates through each entry and calls the function, returns a list if possible, a map otherwise
local function tbl_map(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local mapped = {}
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    insert(mapped, false, f(parCnt(i, j)))
  end
  return newListOrMap(mapped)
end

-- Only returns the characters that match the filter, returns a list if possible, a map otherwise
local function tbl_filter(self, f)
  checkType(1, self, "map", "list", "array")
  checkFunc(2, f)
  local list = false
  if isList(self) then
    list = true
  end
  local filtered = {}
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      insert(filtered, list, i, j)
    end
  end
  return _newListOrMap(filtered, getmetatable(self).islist)
end

local function wrap_tableDropReturn(self, amt, wrap)
  local dropped = {}
  local start, stop = wrap(self, amt)
  for i = start, stop do
    insert(dropped, false, self._tbl[i])
  end
  return _newList(dropped)
end

local function wrap_stringDropReturn(self, amt, wrap)
  return self:sub(wrap(self, amt))
end

local function wrap_handleDropReturn(self, amt, wrap, whenzero, whenall, normal)
  if amt == 0 then
    return whenzero(self)
  elseif amt == #self then
    return whenall(self)
  else
    return normal(self, amt, wrap)
  end
end

local function wrap_dropOrTake(self, amt, wrap, whenzero, whenall)
  checkType(1, self, "list", "stringlist", "array")
  checkArg(2, amt, "number")
  amt = clamp(amt, 0, #self)
  return wrap_handleDropReturn(self, amt, wrap, whenzero, whenall, wrap_tableDropReturn)
end

-- Removes the first amt entries of the list, returns a list
local function tbl_drop(self, amt)
  return wrap_dropOrTake(self, amt, wrap_dropfromleft, wrap_returnself, wrap_returnempty)
end

-- Removes the last amt entries of the list, returns a list
local function tbl_dropright(self, amt)
  return wrap_dropOrTake(self, amt, wrap_dropfromright, wrap_returnself, wrap_returnempty)
end

-- Takes the first amt entries of the list, returns a list
local function tbl_take(self, amt)
  return wrap_dropOrTake(self, amt, wrap_takefromleft, wrap_returnempty, wrap_returnself)
end

-- Takes the last amt entries of the list, returns a list
local function tbl_takeright(self, amt)
  return wrap_dropOrTake(self, amt, wrap_takefromright, wrap_returnempty, wrap_returnself)
end

-- Removes entries while the function returns true, returns a list
local function wrap_dropOrTakeWhile(self, f, wrap, whenzero, whenall)
  checkType(1, self, "list", "stringlist", "array")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  local curr = 0
  for i, j in mpairs(self) do
    if not f(parCnt(i, j)) then
      break
    end
    curr = i
  end
  return wrap_handleDropReturn(self, curr, wrap, whenzero, whenall, wrap_tableDropReturn)
end

local function tbl_dropwhile(self, f)
  return wrap_dropOrTakeWhile(self, f, wrap_dropfromleft, wrap_returnself, wrap_returnempty)
end

local function tbl_takewhile(self, f)
  return wrap_dropOrTakeWhile(self, f, wrap_takefromleft, wrap_returnempty, wrap_returnself)
end

local function wrap_rawslice(self, start, stop, step, sub, returned)
  checkArg(2, start, "number", "nil")
  checkArg(3, stop, "number", "nil")
  checkArg(4, step, "number", "nil")
  step = step or 1
  if step == 0 then
    error("[Selene] bad argument #4 (got step size of 0)", 3)
  end
  start = math.max(1, (not start or start == 0) and (step < 0 and #self or 1) or (start < 0 and start + #self + 1) or start)
  stop = math.min(#self, (not stop or stop == 0) and (step < 0 and 1 or #self) or (stop < 0 and stop + #self + 1) or stop)
  local sliced = {}
  for i = start, stop, step do
    insert(sliced, false, sub(self, i))
  end
  return returned(sliced)
end

local function wrap_returnselfentry(self, i)
  return self._tbl[i]
end

local function tbl_slice(self, start, stop, step)
  checkType(1, self, "list", "stringlist", "array")
  return wrap_rawslice(self, start, stop, step, wrap_returnselfentry, _newList)
end

local function tbl_splice(self, index, ...)
  checkType(1, self, "list", "stringlist", "array")
  checkArg(2, index, "number")
  local repl = {...}
  local spliced = shallowcopy(self._tbl)
  if #repl == 0 then
    table.remove(spliced, index)
  else
    spliced[index] = repl[1]
    for i = 2, #repl do
      table.insert(spliced, index + i - 1, repl[i])
    end
  end
  return _newList(spliced)
end

--inverts the list
local function wrap_reverse(self, newl)
  checkType(1, self, "list", "stringlist", "array")
  local reversed = {}
  for i, j in mpairs(self) do
    table.insert(reversed, 1, j)
  end
  return newl(reversed)
end

local function tbl_reverse(self)
  return wrap_reverse(self, _newList)
end

local function rawflip(self)
  local flipped = {}
  for i, j in mpairs(self) do
    flipped[j] = i
  end
  return flipped
end

local function tbl_flip(self)
  checkType(1, self)
  return newListOrMap(rawflip(self))
end

-- Returns the accumulator
local function tbl_foldleft(self, start, f)
  checkType(1, self)
  checkFunc(3, f)
  local m = start
  for i, j in mpairs(self) do
    m = f(m, j)
  end
  return m
end

-- Returns the accumulator
local function tbl_foldright(self, start, f)
  return tbl_foldleft(tbl_reverse(self), start, f)
end

local function tbl_reduceleft(self, f)
  checkType(1, self, "list", "stringlist", "iterable", "array")
  checkFunc(2, f)
  local d = false
  local m
  for i, j in mpairs(self) do
    if d then
      m = f(m, j)
    else
      d = true
      m = j
    end
  end
  return m
end

local function tbl_reduceright(self, f)
  return tbl_reduceleft(tbl_reverse(self), f)
end

-- Returns the first element of the table that matches the function.
local function tbl_find(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      return j
    end
  end
end

local function tbl_index(self, f)
  checkType(1, self, allIndexables)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      return i
    end
  end
end

local function rawflatten(self)
  checkArg(1, self, "table")
  local flattened = {}
  for i, j in ipairs(self) do
    if type(j) == "table" and isList(j) then
      for k, v in ipairs(j) do
        if v ~= nil then
          table.insert(flattened, v)
        end
      end
    elseif j ~= nil then
      table.insert(flattened, j)
    end
  end
  return flattened
end

local function tbl_flatten(self)
  checkType(1, self, "list", "iterable")
  return _newList(rawflatten(self._tbl))
end

local function tbl_flatmap(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local mapped = {}
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    local mk, mv = f(parCnt(i, j))
    if mv == nil and type(mk) == "table" and isList(mk) then
      for k, v in ipairs(mk) do
        if v ~= nil then
          insert(mapped, false, v)
        end
      end
    elseif mk ~= nil then
      insert(mapped, false, mk, mv)
    end
  end
  return newListOrMap(mapped)
end

local function tbl_zip(self, other)
  checkType(1, self, "list", "stringlist")
  checkType(2, other, "list", "stringlist", "function", "table")
  local zipped = {}
  local tp = tblType(other)
  if tp == "function" then
    local parCnt = checkParCnt(parCount(other, 2))
    for i, j in mpairs(self) do
      table.insert(zipped, { j, f(parCnt(i, j)) })
    end
  elseif tp == "table" then checkList(2, other)
    assert(#self == #other, "length mismatch in zip: Argument #1 has " .. tostring(#self) .. ", argument #2 has " .. tostring(#other))
    for i in mpairs(self) do
      table.insert(zipped, { self._tbl[i], other[i] })
    end
  else
    assert(#self == #other, "length mismatch in zip: Argument #1 has " .. tostring(#self) .. ", argument #2 has " .. tostring(#other))
    for i in mpairs(self) do
      table.insert(zipped, { self._tbl[i], other._tbl[i] })
    end
  end
  return _newList(zipped)
end

local function tbl_contains(self, val)
  checkType(1, self)
  for i, j in mpairs(self) do
    if j == val then
      return true
    end
  end
  return false
end

local function tbl_containskey(self, key)
  checkType(1, self, allMaps)
  return self._tbl[key] ~= nil
end

local function tbl_count(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  local cnt = 0
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      cnt = cnt + 1
    end
  end
  return cnt
end

local function tbl_exists(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      return true
    end
  end
  return false
end

local function tbl_forall(self, f)
  checkType(1, self)
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i, j in mpairs(self) do
    if not f(parCnt(i, j)) then
      return false
    end
  end
  return true
end

local function op_lt(a, b)
  return a < b
end

local function tbl_sortby(self, by, comp)
  checkType(1, self, "list", "array")
  checkFunc(2, by, 1)
  checkType(3, comp, "function", "nil")
  comp = comp or op_lt
  checkFunc(3, comp, 2)
  local cache = {}
  local sorted = shallowcopy(self._tbl)
  for _, j in ipairs(sorted) do
    cache[j] = by(j)
  end
  table.sort(sorted, function(a, b)
    return comp(cache[a], cache[b])
  end)
  return _newList(sorted)
end

local function tbl_unique(self)
  checkType(1, self)
  local unique = {}
  local contains = {}
  for _, j in mpairs(self) do
    if not contains[j] then
      contains[j] = true
      table.insert(unique, j)
    end
  end
  return _newList(unique)
end

local function tbl_occurences(self)
  checkType(1, self)
  local occurences = {}
  for _, j in mpairs(self) do
    occurences[j] = occurences[j] and occurences[j] + 1 or 1
  end
  return new(occurences)
end

local function rawkeys(self)
  local keys = {}
  for i in mpairs(self) do
    table.insert(keys, i)
  end
  return keys
end

local function rawvalues(self)
  local values = {}
  for _, j in mpairs(self) do
    table.insert(values, j)
  end
  return values
end

local function tbl_clear(self)
  checkType(1, self, allMaps)
  for _, j in ipairs(rawkeys(self)) do
    self._tbl[j] = nil
  end
  return self
end

local function tbl_keys(self)
  checkType(1, self)
  return _newList(rawkeys(self))
end

local function tbl_values(self)
  checkType(1, self)
  return _newList(rawvalues(self))
end

local function tbl_unwrap(self)
  checkType(1, self, "map", "list", "stringlist", "array")
  return self._tbl
end

-- for iterable objects

local function itr_collect(self)
  local collected = {}
  for i, j in mpairs(self) do
    insert(collected, false, i, j)
  end
  return newListOrMap(collected)
end

local function itr_drop(self, amt)
  amt = math.max(amt, 0)
  for i = 1, amt do
    if self() == nil then
      break
    end
  end
  return self
end

local function itr_take(self, amt)
  local taken = {}
  amt = math.max(amt, 0)
  for i = 1, amt do
    local next = self()
    if next == nil then
      break
    else
      insert(taken, false, next)
    end
  end
  return _newList(taken)
end

local function itr_unwrap(self)
  return self._spl
end

-- For functions

local function fnc_unwrap(self)
  return self._fnc
end

-- for the actual table library

local function tbl_range(start, stop, step)
  checkArg(1, start, "number")
  checkArg(2, stop, "number")
  checkArg(3, step, "number", "nil")
  step = step or 1
  local nT = {}
  for i = start, stop, step do
    table.insert(nT, i)
  end
  return nT
end

local function tbl_rep(val, amt)
  local nT = {}
  for i = 1, amt do
    nT[i] = val
  end
  return nT
end

local function tbl_zipped(one, two)
  checkType(1, one, "table")
  checkType(2, two, "table", "function")
  checkList(1, one)
  checkList(2, two)
  local zipped = {}
  assert(#one == #two, "length mismatch in zip: Argument #1 has " .. tostring(#one) .. ", argument #2 has " .. tostring(#two))
  for i in ipairs(one) do
    table.insert(zipped, { one[i], two[i] })
  end
  return zipped
end

local function tbl_call(self, f, ...)
  checkType(1, self, allMaps)
  checkFunc(2, f)
  local res = f(self._tbl, ...)
  local tRes = tblType(res)
  if tRes == "table" or tRes == "string" then
    return newGeneric(res)
  else
    return res
  end
end

local function tbl_tclear(self)
  checkType(1, self, "table")
  for _, j in ipairs(rawkeys(self)) do
    self[j] = nil
  end
  return self
end

local function tbl_tsortby(self, by, comp)
  checkList(1, self)
  comp = comp or op_lt
  checkFunc(3, comp, 2)
  local cache = {}
  local sorted = shallowcopy(self)
  for _, j in ipairs(sorted) do
    cache[j] = by(j)
  end
  table.sort(sorted, function(a, b)
    return comp(cache[a], cache[b])
  end)
  return _newList(sorted)
end

--------
-- Bulk data operations on stringlists
--------
local function strl_filter(self, f)
  checkType(1, self, "stringlist")
  checkFunc(2, f)
  local filtered = {}
  local parCnt = checkParCnt(parCount(f))
  for i, j in mpairs(self) do
    if f(parCnt(i, j)) then
      insert(filtered, false, parCnt(i, j))
    end
  end
  return _newStringList(filtered)
end

local function strl_dropOrTake(self, amt, wrap)
  checkType(1, self, "stringlist")
  self = wrap(self, amt)
  return table.concat(self._tbl)
end

local function strl_drop(self, amt)
  return strl_dropOrTake(self, amt, tbl_drop)
end

local function strl_dropright(self, amt)
  return strl_dropOrTake(self, amt, tbl_dropright)
end

local function strl_take(self, amt)
  return strl_dropOrTake(self, amt, tbl_take)
end

local function strl_takeright(self, amt)
  return strl_dropOrTake(self, amt, tbl_takeright)
end

local function strl_dropwhile(self, f)
  return strl_dropOrTake(self, f, tbl_dropwhile)
end

local function strl_takewhile(self, f)
  return strl_dropOrTake(self, f, tbl_takewhile)
end

local function strl_slice(self, start, stop, step)
  checkType(1, self, "stringlist")
  return wrap_rawslice(self, start, stop, step, wrap_returnselfentry, _newStringList)
end

local function strl_reverse(self)
  return wrap_reverse(self, _newStringList)
end

--------
-- Bulk data operations on strings
--------
local function wrap_emptystring(self)
  return ""
end

-- Calls a functions once per character, returns nil
local function str_foreach(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f))
  for i = 1, #self do
    f(parCnt(i, self:sub(i,i)))
  end
end

-- Iterates through each character and calls the function, returns a list if possible, a map otherwise
local function str_map(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local mapped = {}
  local parCnt = checkParCnt(parCount(f))
  for i = 1, #self do
    insert(mapped, false, f(parCnt(i, self:sub(i, i))))
  end
  return newListOrMap(mapped)
end

local function str_flatmap(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local mapped = {}
  local parCnt = checkParCnt(parCount(f))
  for i = 1, #self do
    local mk, mv = f(parCnt(i, self:sub(i, i)))
    if mv == nil and type(mk) == "table" and isList(mk) then
      for k, v in ipairs(mk) do
        if v ~= nil then
          insert(mapped, false, v)
        end
      end
    elseif mk ~= nil then
      insert(mapped, false, mk, mv)
    end
  end
  return newListOrMap(mapped)
end

-- Only returns the characters that match the filter, returns a list
local function str_filter(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local filtered = {}
  local parCnt = checkParCnt(parCount(f))
  for i = 1, #self do
    local j = self:sub(i, i)
    if f(parCnt(i, j)) then
      insert(filtered, false, parCnt(i, j))
    end
  end
  return table.concat(filtered)
end

local function wrap_str_dropOrTake(self, amt, wrap, whenzero, whenall)
  checkArg(1, self, "string")
  checkArg(2, amt, "number")
  amt = clamp(amt, 0, #self)
  return wrap_handleDropReturn(self, amt, wrap, whenzero, whenall, wrap_stringDropReturn)
end

-- Removes the first amt characters of the string, returns a string
local function str_drop(self, amt)
  return wrap_str_dropOrTake(self, amt, wrap_dropfromleft, wrap_returnself, wrap_emptystring)
end

-- Removes the last amt characters of the string, returns a string
local function str_dropright(self, amt)
  return wrap_str_dropOrTake(self, amt, wrap_dropfromright, wrap_returnself, wrap_emptystring)
end

-- Takes the first amt characters of the string, returns a string
local function str_take(self, amt)
  return wrap_str_dropOrTake(self, amt, wrap_takefromleft, wrap_emptystring, wrap_returnself)
end

-- Takes the last amt characters of the string, returns a string
local function str_takeright(self, amt)
  return wrap_str_dropOrTake(self, amt, wrap_takefromright, wrap_emptystring, wrap_returnself)
end

-- Removes characters while the function returns true, returns a string
local function wrap_str_dropOrTakeWhile(self, f, wrap, whenzero, whenall)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f))
  local index = 0
  for i = 1, #self do
    local s = self:sub(i, i)
    if not f(parCnt(i, s)) then
      break
    end
    index = i
  end
  return wrap_handleDropReturn(self, index, wrap, whenzero, whenall, wrap_stringDropReturn)
end

local function str_dropwhile(self, f)
  return wrap_str_dropOrTakeWhile(self, f, wrap_dropfromleft, wrap_returnself, wrap_emptystring)
end

local function str_takewhile(self, f)
  return wrap_str_dropOrTakeWhile(self, f, wrap_takefromleft, wrap_emptystring, wrap_returnself)
end

local function wrap_str_slicepart(self, i)
  return self:sub(i, i)
end

local function str_slice(self, start, stop, step)
  checkArg(1, self, "string")
  return wrap_rawslice(self, start, stop, step, wrap_str_slicepart, _newStringList)
end

-- Returns the accumulator
local function str_foldleft(self, start, f)
  checkArg(1, self, "string")
  checkFunc(3, f)
  local m = start
  for i = 1, #self do
    m = f(m, self:sub(i, i))
  end
  return m
end

-- Returns the accumulator
local function str_foldright(self, start, f)
  return str_foldleft(self:reverse(), start, f)
end

local function str_reduceleft(self, f)
  checkArg(1, self, "string")
  checkFunc(3, f)
  if #self <= 0 then
    error("[Selene] bad argument #1 (got empty string)", 2)
  end
  local m = self:sub(1, 1)
  for i = 2, #self do
    m = f(m, self:sub(i, i))
  end
  return m
end

local function str_reduceright(self, f)
  return str_reduceleft(self:reverse(), f)
end

local function str_split(self, sep, maxSize, useRegex)
  checkArg(1, self, "string")
  checkArg(2, sep, "string", "number", "nil")

  local t = {}

  if not sep or sep == "" then
    sep = 1
  end

  if type(sep) == "number" then
    while #self > 0 do
      table.insert(t, self:sub(1, sep))
      self = self:sub(sep + 1)
    end
  else
    checkArg(3, maxSize, "number", "nil")
    checkArg(4, useRegex, "boolean", "nil")
    -- http://lua-users.org/wiki/SplitJoin
    -- "Function: true Python semantics for split"
    assert(maxSize == nil or maxSize >= 1, "invalid argument 3: max table size is smaller than 1.")

    if self:len() > 0 then
      local plain = not useRegex
      maxSize = maxSize or -1

      local tblIndex, searchIndex = 1, 1
      local start, stop = self:find(sep, searchIndex, plain)
      while start and maxSize ~= 0 do
        t[tblIndex] = self:sub(searchIndex, start - 1)
        tblIndex = tblIndex + 1
        searchIndex = stop + 1
        start, stop = self:find(sep, searchIndex, plain)
        maxSize = maxSize - 1
      end
      t[tblIndex] = self:sub(searchIndex)
    end
  end
  return _newList(t)
end

local function str_contains(self, val)
  checkArg(1, self, "string")
  return string.find(self, val, 1, true) ~= nil
end

local function str_count(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  local cnt = 0
  for i = 1, #self do
    if f(parCnt(i, self:sub(i, i))) then
      cnt = cnt + 1
    end
  end
  return cnt
end

local function str_exists(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i = 1, #self do
    if f(parCnt(i, self:sub(i, i))) then
      return true
    end
  end
  return false
end

local function str_forall(self, f)
  checkArg(1, self, "string")
  checkFunc(2, f)
  local parCnt = checkParCnt(parCount(f, 2))
  for i = 1, #self do
    if not f(parCnt(i, self:sub(i, i))) then
      return false
    end
  end
  return true
end

-- ipairs iterator for strings
local function str_ipairs_iter(str, i)
  i = i + 1
  local s = str:sub(i, i)
  if s and #s >= 1 then
    return i, s
  else
    return nil
  end
end

local function str_iter(str)
  return str_ipairs_iter, str, 0
end

-- The famous and infamous fish-or
local function bfor(one, two, three)
  checkArg(1, one, "number")
  checkArg(2, two, "number")
  checkArg(3, three, "number")
  if not bit32 then return end
  return bit32.bor(bit32.band(bit32.bnot(one), two, three), bit32.band(one, bit32.bnot(two), three), bit32.band(one, two, bit32.bnot(three)))
end

local function nfor(one, two, three)
  return (not one and two and three) or (one and not two and three) or (one and two and not three)
end

--------
-- Parsing
--------

local selene = {}

do
  selene.parser = require("lua.selene_parser")
end

local function parse(chunk, stripcomments)
  return selene.parser.parse(chunk, stripcomments)
end

--------
-- Maths
--------

--------
-- Constructing arrays
--------

local function newArray(tbl, size, len)
  if not len then
    len = 1
    for i = 1, #size do
      len = len * size[i]
    end
  end
  if len ~= #tbl then
    error(string.format("[Selene] attempt to reshape table of length %d into array of size (%s).", #tbl, table.concat(size, ", ")), 2)
  end
  local newObj = {}
  for i, j in pairs(_Array) do
    newObj[i] = j
  end
  newObj._tbl = tbl
  setmetatable(newObj, mdmt)
  newObj._size = size
  return newObj
end

local function fillArray(val, size)
  local len = 1
  for i = 1, #size do
    len = len * size[i]
  end
  return newArray(tbl_rep(val, len), size, len)
end

--------
-- array operators
--------

local function allEqual(first, second)
  if #first ~= #second then
    return false
  end
  for i = 1, #first do
    if first[i] ~= second[i] then
      return false
    end
  end
  return true
end

local function checkSize(first, second)
  if not allEqual(first._size, second._size) then
    error(string.format("[Selene] attempt to do arithmetic between %d-dimensional and %d-dimensional array.", #first._size, #second._size), 3)
  end
end

local function posToIndex(pos, size)
  local i, m = 0, 1
  for ki = 1, #pos do
    if pos[ki] < 1 or pos[ki] > size[ki] then return end
    i = i + ((pos[ki] - 1) * m)
    m = m * size[ki]
  end
  return i + 1
end

local function mod1(x, y)
  local m = x % y
  return m == 0 and y or m
end

local function fld(x, y)
  return math.floor(x / y)
end

local function fld1(x, y)
  return fld(x + y - mod1(x, y), y)
end

local function indexToPos(index, size)
  local pos = {}
  for dim = 1, #size do
    pos[dim] = mod1(fld1(index, dim), size[dim])
  end
  return pos
end

local function op_addToRes(tbl, c, r, i)
  r[i] = tbl[posToIndex(c, tbl._size)]
end

local function op_setIndex(tbl, c, val)
  tbl[posToIndex(c, tbl._size)] = val
end

local function sliceIndexOp(tbl, key, val, op)
  local c, starts, ends = {}, {}, {}
  for i = 1, #key do
    if type(key[i]) == "table" then
      if key[i][1] > key[i][2] or key[i][1] < 1 or key[i][2] > tbl._size[i] then
        error(string.format("[Selene] attempt to index array of size %d in dimension %d at range %d:%d.", tbl._size[i], i, key[i][1], key[i][2]), 3)
      end
      c[i] = key[i][1]
      starts[i] = key[i][1]
      ends[i] = key[i][2]
    else
      c[i] = key[i]
      starts[i] = key[i]
      ends[i] = key[i]
    end
  end
  local endreached = true
  op(tbl, c, val, 1)
  for i = 1, #c do
    if c[i] ~= ends[i] then
      endreached = false
      break
    end
  end
  local newI = 2
  while not endreached do
    for i = 1, #c do
      if c[i] < ends[i] then
        c[i] = c[i] + 1
        op(tbl, c, val, newI)
        newI = newI + 1
        break
      else
        c[i] = starts[i]
      end
    end
    endreached = true
    for i = 1, #c do
      if c[i] ~= ends[i] then
        endreached = false
        break
      end
    end
  end
end

local function sliceIndex(tbl, key)
  local r, s = {}, {}

  sliceIndexOp(tbl, key, r, op_addToRes)

  for i = 1, #key do
    if type(key[i]) == "table" then
      s[i] = key[i][2] - key[i][1] + 1
    else
      s[i] = 1
    end
  end

  return newArray(r, s)
end

local function sliceSetIndex(tbl, key, val)
  sliceIndexOp(tbl, key, val, op_setIndex)
end

mdmt.__len = function(tbl)
  local r = 1
  for _, s in ipairs(tbl._size) do
    r = r * s
  end
  return r
end

mdmt.__index = function(tbl, key)
  if type(key) == "table" then
    if #key ~= #tbl._size then
      error(string.format("[Selene] attempt to index %d dimensions of %d-dimensional array.", #key, #tbl._size), 2)
    end
    local i, m = 0, 1
    for ki = 1, #key do
      if type(key[ki]) == "table" then
        return sliceIndex(tbl, key)
      end
      if key[ki] < 1 or key[ki] > tbl._size[ki] then
        return nil
      end
      i = i + ((key[ki] - 1) * m)
      m = m * tbl._size[ki]
    end
    return tbl._tbl[i + 1]
  else
    return tbl._tbl[key]
  end
end

mdmt.__newindex = function(tbl, key, val)
  if type(key) == "table" then
    if #key ~= #tbl._size then
      error(string.format("[Selene] attempt to index %d dimensions of %d-dimensional array.", #key, tbl._dims), 2)
    end
    local i, m = 0, 1
    for ki = 1, #key do
      if type(key[ki]) == "table" then
        return sliceSetIndex(tbl, key, val)
      end
      i = i + ((key[ki] - 1) * m)
      m = m * tbl._size[ki]
      if key[ki] > tbl._size[ki] then
        error(string.format("[Selene] attempt to access index %d of array with length %d in dimension %d.", key[ki], tbl._size[ki], ki), 2)
      end
    end
    tbl._tbl[i + 1] = val
  else
    tbl._tbl[key] = val
  end
end

mdmt.__eq = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType ~= "array" or sType ~= "array" then
    error(string.format("[Selene] attempt to compare %s and %s", fType, sType), 3)
  end
  return allEqual(first._size, second._size) and allEqual(first._tbl, second._tbl)
end

local function op_add(a, b)
  return a + b
end

local function op_mul(a, b)
  return a * b
end

local function op_sub(a, b)
  return a - b
end

local function op_div(a, b)
  return a / b
end

local function op_mod(a, b)
  return a % b
end

local function opAN(tbl, n, op)
  local r = {}
  for i = 1, #tbl do
    r[i] = op(tbl[i], n)
  end
  return newArray(r, tbl._size)
end

local function opAA(first, second, op)
  checkSize(first, second)
  local r = {}
  for i = 1, #first do
    r[i] = op(first[i], second[i])
  end
  return newArray(r, first._size)
end

local function opAerr(fType, sType, op)
  error(string.format("[Selene] attempt to %s %s and %s", op, fType, sType), 3)
end

mdmt.__add = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "array" and sType == "array" then
    return opAA(first, second, op_add)
  elseif fType == "number" and sType == "array" then
    return opAN(second, first, op_add)
  elseif fType == "array" and sType == "number" then
    return opAN(first, second, op_add)
  else
    opAerr(fType, sType, "add")
  end
end

mdmt.__mul = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "number" and sType == "array" then
    return opAN(second, first, op_mul)
  elseif fType == "array" and sType == "number" then
    return opAN(first, second, op_mul)
  else
    opAerr(fType, sType, "multiply")
  end
end

mdmt.__sub = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "array" and sType == "array" then
    return opAA(first, second, op_sub)
  elseif fType == "array" and sType == "number" then
    return opAN(first, second, op_sub)
  else
    opAerr(fType, sType, "subtract")
  end
end

mdmt.__div = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "array" and sType == "number" then
    return opAN(first, second, op_div)
  else
    opAerr(fType, sType, "divide")
  end
end

mdmt.__mod = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "array" and sType == "number" then
    return opAN(first, second, op_mod)
  else
    opAerr(fType, sType, "divide")
  end
end

mdmt.__pow = function(first, second)
  local fType, sType = tblType(first), tblType(second)
  if fType == "array" and sType == "number" then
    return opAN(first, second, op_mod)
  else
    opAerr(fType, sType, "calculate power of")
  end
end

mdmt.__unm = function(arr)
  local r = {}
  for i = 1, #arr do
    r[i] = -arr[i]
  end
  return newArray(r, arr._size)
end

--------
-- Operations on arrays
--------

local function arr_size(self)
  return table.unpack(self._size)
end

local function arr_dims(self)
  return #self._size
end

local function arr_get_slices(self, def, dims)
  local newsize = {}
  local c, starts, ends = {}, {}, {}
  local co = {}
  local pure = true
  for i, s in ipairs(self._size) do
    local start, stop
    if type(dims[i]) == "table" then
      start, stop = dims[i][1], dims[i][2]
    else
      start, stop = dims[i], dims[i]
    end
    if stop < start then
      error(string.format("[Selene] attempt to get slice where upper limit %d is smaller than lower limit %d.", stop, start), 2)
    end
    if start < 1 or stop > s then
      pure = false
    end
    local realstart, realstop = clamp(start, 1, s), clamp(stop, 1, s)
    newsize[i] = stop - start + 1
    c[i] = realstart
    co[i] = realstart - start + 1
    starts[i] = realstart
    ends[i] = realstop
    dims[i] = {start, stop}
  end
  if pure then
    return self[dims]
  end
  local res = fillArray(def, newsize)
  do
    local ind = posToIndex(co, res._size)
    if ind then
      res[ind] = self[posToIndex(c, self._size)]
    end
  end
  local endreached = true
  for i = 1, #c do
    if c[i] ~= ends[i] then
      endreached = false
      break
    end
  end
  while not endreached do
    for i = 1, #c do
      if c[i] < ends[i] then
        c[i] = c[i] + 1
        co[i] = c[i] - dims[i][1] + 1
        res[posToIndex(co, res._size)] = self[posToIndex(c, self._size)]
        break
      else
        c[i] = starts[i]
        co[i] = c[i] - dims[i][1] + 1
      end
    end

    endreached = true
    for i = 1, #c do
      if c[i] ~= ends[i] then
        endreached = false
        break
      end
    end
  end
  return res
end

local function arr_get(self, def, ...)
  local dims = {...}
  if #dims ~= #self._size then
    error(string.format("[Selene] attempt to get %d-dimensional slice of %d-dimensional array.", #dims, #self._size), 2)
  end
  local i, m = 0, 1
  for ki = 1, #dims do
    if type(dims[ki]) == "table" then
      return arr_get_slices(self, def, dims)
    end
    if dims[ki] < 1 or dims[ki] > self._size[ki] then
      return def
    end
    i = i + ((dims[ki] - 1) * m)
    m = m * self._size[ki]
  end
  return self._tbl[i + 1]
end

-- Broadcast calls
local function broadcast(func, ...)
  checkFunc(1, func)
  local vectors = {}
  local args = {...}
  local len = -1
  local makeList = false
  for i, arg in ipairs(args) do
    local t_len, t_type = getLen(arg)
    if t_len then
      if len < 0 then
        len = t_len
      elseif t_len ~= len then
        error(string.format("[Selene] attempt to broadcast call with table of length %d and table of length %d", len, t_len), 2)
      end
      vectors[i] = true
      makeList = makeList or t_type == "list"
    end
  end
  if len < 0 then
    return func(...)
  end
  local res = {}
  for i = 1, len do
    local i_arg = {}
    for j, arg in ipairs(args) do
      if vectors[j] then
        i_arg[j] = arg[i]
      else
        i_arg[j] = arg
      end
    end
    res[i] = func(table.unpack(i_arg))
  end
  return makeList and _newList(res) or res
end

--------
-- Adding to global variables
--------

local VERSION = "Selene 0.1.0.7"

local function patchNativeLibs(env)
  env.string.foreach = str_foreach
  env.string.map = str_map
  env.string.flatmap = str_flatmap
  env.string.filter = str_filter
  env.string.drop = str_drop
  env.string.dropright = str_dropright
  env.string.dropwhile = str_dropwhile
  env.string.take = str_take
  env.string.takeright = str_takeright
  env.string.takewhile = str_takewhile
  env.string.slice = str_slice
  env.string.fold = str_foldleft
  env.string.foldleft = str_foldleft
  env.string.foldright = str_foldright
  env.string.reduce = str_reduceleft
  env.string.reduceleft = str_reduceleft
  env.string.reduceright = str_reduceright
  env.string.split = str_split
  env.string.contains = str_contains
  env.string.count = str_count
  env.string.exists = str_exists
  env.string.forall = str_forall
  env.string.iter = str_iter

  env.table.shallowcopy = shallowcopy
  env.table.flatten = function(tbl)
    checkList(1, tbl)
    return rawflatten(tbl)
  end
  env.table.range = tbl_range
  env.table.rep = tbl_rep
  env.table.flip = function(tbl)
    checkArg(1, tbl, "table")
    return rawflip(tbl)
  end
  env.table.zipped = tbl_zipped
  env.table.clear = tbl_tclear
  env.table.keys = function(tbl)
    checkArg(1, tbl, "table")
    return rawkeys(tbl)
  end
  env.table.values = function(tbl)
    checkArg(1, tbl, "table")
    return rawvalues(tbl)
  end
  env.table.sortby = tbl_tsortby
  env.table.filled = fillArray

  if env.bit32 then
    env.bit32.bfor = bfor
    env.bit32.nfor = nfor
  end
end

local function populateTables()
  _Table.concat = tbl_concat
  _Table.foreach = tbl_foreach
  _Table.map = tbl_map
  _Table.flatmap = tbl_flatmap
  _Table.filter = tbl_filter
  _Table.drop = tbl_drop
  _Table.dropright = tbl_dropright
  _Table.dropwhile = tbl_dropwhile
  _Table.take = tbl_take
  _Table.takeright = tbl_takeright
  _Table.takewhile = tbl_takewhile
  _Table.slice = tbl_slice
  _Table.splice = tbl_splice
  _Table.reverse = tbl_reverse
  _Table.flip = tbl_flip
  _Table.fold = tbl_foldleft
  _Table.foldleft = tbl_foldleft
  _Table.foldright = tbl_foldright
  _Table.reduce = tbl_reduceleft
  _Table.reduceleft = tbl_reduceleft
  _Table.reduceright = tbl_reduceright
  _Table.find = tbl_find
  _Table.index = tbl_index
  _Table.flatten = tbl_flatten
  _Table.zip = tbl_zip
  _Table.contains = tbl_contains
  _Table.containskey = tbl_containskey
  _Table.count = tbl_count
  _Table.exists = tbl_exists
  _Table.forall = tbl_forall
  _Table.sortby = tbl_sortby
  _Table.call = tbl_call
  _Table.clear = tbl_clear
  _Table.keys = tbl_keys
  _Table.values = tbl_values
  _Table.unwrap = tbl_unwrap
  _Table.unique = tbl_unique
  _Table.occurences = tbl_occurences

  _Table.shallowcopy = function(self)
    checkType(1, self, allMaps)
    return _newListOrMap(shallowcopy(self._tbl), getmetatable(self).islist)
  end

  _Table.switch = function(self, ...)
    checkType(1, self, allMaps)
    return switch(self, ...)
  end

  _Table.len = function(self)
    return #self._tbl
  end
end

local function populateOthers()
  _Array = shallowcopy(_Table)
  _Array.dims = arr_dims
  _Array.size = arr_size
  _Array.get = arr_get

  _Iterable = shallowcopy(_Table)
  _Iterable.collect = itr_collect
  _Iterable.drop = itr_drop
  _Iterable.take = itr_take
  _Iterable.unwrap = itr_unwrap

  _Func.unwrap = fnc_unwrap

  _String.foreach = tbl_foreach
  _String.map = tbl_map
  _String.flatmap = tbl_flatmap
  _String.filter = strl_filter
  _String.drop = strl_drop
  _String.dropright = strl_dropright
  _String.dropwhile = strl_dropwhile
  _String.take = strl_take
  _String.takeright = strl_takeright
  _String.takewhile = strl_takewhile
  _String.slice = strl_slice
  _String.reverse = strl_reverse
  _String.flip = tbl_flip
  _String.foldleft = tbl_foldleft
  _String.foldright = tbl_foldright
  _String.reduce = tbl_reduceleft
  _String.reduceleft = tbl_reduceleft
  _String.reduceright = tbl_reduceright
  _String.split = function(self, sep)
    checkType(1, self, "stringlist")
    return str_split(tostring(self), sep)
  end
  _String.contains = tbl_contains
  _String.count = tbl_count
  _String.exists = tbl_exists
  _String.forall = tbl_forall
  _String.iter = function(self)
    checkType(1, self, "stringlist")
    return str_iter(tostring(self))
  end
  _String.call = tbl_call
  _String.unwrap = tbl_unwrap
  _String.unique = tbl_unique
  _String.occurences = tbl_occurences

  _String.switch = function(self, ...)
    checkType(1, self, "stringlist")
    return switch(self, ...)
  end
end

local function loadSeleneConstructs()
  populateTables()

  populateOthers()
end

local function loadSelene(env, lvMode)
  if not env or type(env) ~= "table" then env = _G or _ENV end
  if env._selene and env._selene.isLoaded then return end
  if not env._selene then env._selene = {} end

  if lvMode then
    env._selene.liveMode = true
  end

  env._selene._new = newGeneric
  if not env.checkArg then
    env.checkArg = checkArg
    env._selene._checkArg = true
  end
  if env._selene.notypecheck then
    env._selene._oldtypecheck = checkArg
    checkArg = function() end
  end
  env._selene._newString = newString
  env._selene._newList = newList
  env._selene._newArray = newArray
  env._selene._newFunc = newFunc
  env._selene._newIterable = newIterable
  env._selene._newOptional = newOptional
  env._selene._VERSION = VERSION
  env._selene._parse = parse
  env._selene._broadcast = broadcast
  env.ltype = tblType
  env.checkType = checkType
  env.checkFunc = checkFunc
  env.parCount = parCount
  env.lpairs = lpairs
  env.isList = isList
  env.switch = switch
  env.broadcast = broadcast

  patchNativeLibs(env)
  loadSeleneConstructs(env)

  if env._selene and env._selene.liveMode then
    env._selene.oldload = env.load
    env.load = function(ld, src, mv, loadenv)
      if env._selene and env._selene.liveMode and env._selene.isLoaded then
        if type(ld) == "function" then
          local s = ""
          local nws = ld()
          while nws and #nws > 0 do
            s = s .. nws
            nws = ld()
          end
          ld = s
        end
        ld = env._selene._parse(ld)
      end
      return env._selene.oldload(ld, src, mv, loadenv)
    end
  end
  env._selene.isLoaded = true
end

local function unloadSelene(env)
  if not env or type(env) ~= "table" then env = _G or _ENV end
  if not env._selene or not env._selene.isLoaded then return end
  if env._selene and env._selene.liveMode and env._selene.oldload then
    env.load = env._selene.oldload
  end
  if env._selene._checkArg then
    env.checkArg = nil
  end
  if env._selene.notypecheck then
    checkArg = env._selene._oldTypeCheck
  end
  do
    local liveMode = env._selene.liveMode
    env._selene = {}
    env._selene.liveMode = liveMode
  end
  env.ltype = nil
  env.checkType = nil
  env.checkFunc = nil
  env.parCount = nil
  env.lpairs = nil
  env.isList = nil
  env.switch = nil
  env.broadcast = nil

  env.string.foreach = nil
  env.string.map = nil
  env.string.flatmap = nil
  env.string.filter = nil
  env.string.drop = nil
  env.string.dropright = nil
  env.string.dropwhile = nil
  env.string.take = nil
  env.string.takeright = nil
  env.string.takewhile = nil
  env.string.slice = nil
  env.string.fold = nil
  env.string.foldleft = nil
  env.string.foldright = nil
  env.string.reduce = nil
  env.string.reduceleft = nil
  env.string.reduceright = nil
  env.string.split = nil
  env.string.contains = nil
  env.string.count = nil
  env.string.exists = nil
  env.string.forall = nil
  env.string.iter = nil

  env.table.shallowcopy = nil
  env.table.flatten = nil
  env.table.range = nil
  env.table.rep = nil
  env.table.flip = nil
  env.table.zipped = nil
  env.table.clear = nil
  env.table.keys = nil
  env.table.values = nil
  env.table.sortby = nil
  env.table.filled = nil

  if env.bit32 then
    env.bit32.bfor = nil
    env.bit32.nfor = nil
  end
  env._selene.isLoaded = false
end

if _selene and not _selene.isLoaded and _selene.doAutoload then
  loadSelene(_G or _ENV)
end

selene.parse = parse
selene.load = loadSelene
selene.unload = unloadSelene
selene.isLoaded = function()
  return _selene and _selene.isLoaded
end

return selene