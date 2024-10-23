local utils = require("utils")
local drawing = require("utils.drawing")

--[[
RYSY INTERNALS
]]
local __metaMt = {}

-- Add support for Rysy lazyloading - only load in the size of the texture if actually needed.
function __metaMt.__index(self, key)
    local loaded = rawget(self, "_RYSY_loaded")
    if not loaded then
        local x, y, width, height, offsetX, offsetY = _RYSY_DRAWABLE_getTextureSize(rawget(self, "_RYSY_INTERNAL_texture"))
        
        rawset(self, "x", x)
        rawset(self, "y", y)
        rawset(self, "width", width)
        rawset(self, "height", height)
		rawset(self, "offsetX", offsetX)
		rawset(self, "offsetY", offsetY)
        rawset(self, "_RYSY_loaded", true)

		rawset(self, "realWidth", width)
		rawset(self, "realHeight", height)
    end

    local raw = rawget(self, key)
    if raw then 
        return raw 
    end

    return nil
end

local _metaCache = {}

local function __newMeta(texture)
    local cached = _metaCache[texture]
    if cached then
        return cached
    end
    
	local m = {
        _RYSY_INTERNAL_texture = texture
    }
    setmetatable(m, __metaMt)
    _metaCache[texture] = m

    return m
end
-- END RYSY INTERNALS

local drawableSpriteStruct = {}

local drawableSpriteMt = {}
drawableSpriteMt.__index = {}

function drawableSpriteMt.__index:setJustification(justificationX, justificationY)
    if type(justificationX) == "table" then
        justificationX, justificationY = justificationX[1], justificationX[2]
    end

    self.justificationX = justificationX
    self.justificationY = justificationY

    return self
end

function drawableSpriteMt.__index:setPosition(x, y)
    if type(x) == "table" then
        x, y = x[1] or x.x, x[2] or x.y
    end

    self.x = x
    self.y = y

    return self
end

function drawableSpriteMt.__index:addPosition(x, y)
    if type(x) == "table" then
        x, y = x[1] or x.x, x[2] or x.y
    end

    self.x += x
    self.y += y

    return self
end

function drawableSpriteMt.__index:setScale(scaleX, scaleY)
    if type(scaleX) == "table" then
        scaleX, scaleY = scaleX[1], scaleX[2]
    end

    self.scaleX = scaleX
    self.scaleY = scaleY

    return self
end

function drawableSpriteMt.__index:setOffset(offsetX, offsetY)
    if type(offsetX) == "table" then
        offsetX, offsetY = offsetX[1], offsetX[2]
    end

    self.offsetX = offsetX
    self.offsetY = offsetY

    return self
end

local function setColor(target, color)
    local tableColor = utils.getColor(color)

    if tableColor then
        target.color = tableColor
    end

    return tableColor ~= nil
end

function drawableSpriteMt.__index:setColor(color)
    return setColor(self, color)
end

function drawableSpriteMt.__index:setAlpha(alpha)
    local r, g, b = unpack(self.color or {})
    local newColor = {r or 1, g or 1, b or 1, alpha}

    return setColor(self, newColor)
end

function drawableSpriteMt.__index:getRectangleRaw()
    local x,y,w,h = _RYSY_DRAWABLE_getRectangle(self)
	return x, y, w, h
end

function drawableSpriteMt.__index:getRectangle()
    local x,y,w,h = _RYSY_DRAWABLE_getRectangle(self)
	return utils.rectangle(x, y, w, h)
end

function drawableSpriteMt.__index:drawRectangle(mode, color)
    _RYSY_unimplemented()
end

function drawableSpriteMt.__index:draw()
    _RYSY_unimplemented()
end

function drawableSpriteMt.__index:getRelativeQuad(x, y, width, height, hideOverflow, realSize)
    _RYSY_unimplemented()
end

function drawableSpriteMt.__index:useRelativeQuad(x, y, width, height, hideOverflow, realSize)
    self._RYSYqX = x
    self._RYSYqY = y
    self._RYSYqW = width
    self._RYSYqH = height

	self.quad = {
		x=x,y=y,w=width,h=height
	}
end

function drawableSpriteMt.__newindex(self, key, value)
	rawset(self, key, value)
	if value and key == "quad" then
		self._RYSYqX = value.x
		self._RYSYqY = value.y
		self._RYSYqW = value.w
		self._RYSYqH = value.h
	end
end

function RYSY_UNPACKSPR(drawableSprite)
	return rawget(drawableSprite, "x"), rawget(drawableSprite, "y"),
		   rawget(drawableSprite, "justificationX"), rawget(drawableSprite, "justificationY"),
		   rawget(drawableSprite, "scaleX"), rawget(drawableSprite, "scaleY"),
		   rawget(drawableSprite, "rotation"), rawget(drawableSprite, "depth"),
		   rawget(drawableSprite, "color"), rawget(drawableSprite, "_RYSY_INTERNAL_texture"),
		   rawget(drawableSprite, "_RYSYqX")
end

local function __create(meta, data, texture)
    if not meta and not texture then
        return nil
    end

    data = data or {}

    local drawableSprite = _RYSY_DRAWABLE_makeFromEntity(data, texture) or nil
    
    if not drawableSprite then
        drawableSprite = {
            _type = "drawableSprite",
            x = data.x or 0,
            y = data.y or 0,
            -- long names swapped with short ones, because most plugins use long names (if any)
            justificationX = data.justificationX or data.jx or 0.5,
            justificationY = data.justificationY or data.jy or 0.5,
            scaleX = data.scaleX or data.sx or 1,
            scaleY = data.scaleY or data.sy or 1,
            rotation = data.rotation or data.r or 0,
            depth = data.depth,
        }
    end
    
    if texture then
        drawableSprite._RYSY_INTERNAL_texture = texture
        drawableSprite.meta = __newMeta(texture)
    else
        drawableSprite.meta = meta
        drawableSprite._RYSY_INTERNAL_texture = rawget(meta, "_RYSY_INTERNAL_texture")
    end

    if data.color then
        setColor(drawableSprite, data.color)
    end

    return setmetatable(drawableSprite, drawableSpriteMt)
end

function drawableSpriteStruct.fromMeta(meta, data)
    return __create(meta, data)
end

function drawableSpriteStruct.fromTexture(texture, data)
    texture = _RYSY_DRAWABLE_fixPath(texture)

	if not _RYSY_DRAWABLE_exists(texture) then
		return nil
	end

    return __create(nil, data, texture)
end

function drawableSpriteStruct.fromInternalTexture(texture, data)
    return drawableSpriteStruct.fromTexture(string.format("@Internal@/%s", texture), data)
end

return drawableSpriteStruct