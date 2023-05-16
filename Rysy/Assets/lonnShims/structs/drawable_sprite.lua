-- A partial implementation of drawable sprite
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
    end

    local raw = rawget(self, key)
    if raw then 
        return raw 
    end

    return nil
end

local function __newMeta(sprite)
	local m = {
        _RYSY_INTERNAL_texture = sprite._RYSY_INTERNAL_texture
    }

    return setmetatable(m, __metaMt)
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
    _RYSY_unimplemented()
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


function drawableSpriteStruct.fromMeta(meta, data)
    data = data or {}

    local drawableSprite = {
        _type = "drawableSprite"
    }

    drawableSprite.x = data.x or 0
    drawableSprite.y = data.y or 0

    drawableSprite.justificationX = data.jx or data.justificationX or 0.5
    drawableSprite.justificationY = data.jy or data.justificationY or 0.5

    drawableSprite.scaleX = data.sx or data.scaleX or 1
    drawableSprite.scaleY = data.sy or data.scaleY or 1

    drawableSprite.rotation = data.r or data.rotation or 0

    drawableSprite.depth = data.depth

    drawableSprite.meta = meta or __newMeta(drawableSprite)

	-- handle creating clones of sprites
	if meta then
		local baseSprite = rawget(meta, "_RYSY_INTERNAL_texture")
		if baseSprite then
			drawableSprite._RYSY_INTERNAL_texture = baseSprite
		end
	end

    if data.color then
        setColor(drawableSprite, data.color)
    end

    return setmetatable(drawableSprite, drawableSpriteMt)
end

function drawableSpriteStruct.fromTexture(texture, data)
	if not _RYSY_DRAWABLE_exists(texture) then
		return nil
	end

    local spr = drawableSpriteStruct.fromMeta(spriteMeta, data)
    spr._RYSY_INTERNAL_texture = texture
	rawset(spr.meta, "_RYSY_INTERNAL_texture", texture)

    return spr
end

function drawableSpriteStruct.fromInternalTexture(texture, data)
    return drawableSpriteStruct.fromTexture(string.format("Rysy:%s", texture), data)
end

return drawableSpriteStruct