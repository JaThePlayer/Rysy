local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local drawableNinePatch = {}

local drawableNinePatchMt = {}
drawableNinePatchMt.__index = {}

function drawableNinePatchMt.__index:getSpriteSize(sprite)
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:cacheNinePatchMatrix()
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:getMatrix()
    _RYSY_unimplemented()
end

local function getMatrixSprite(atlas, texture, x, y, matrix, quadX, quadY)
	_RYSY_unimplemented()
end

local function getRelativeQuadSprite(atlas, texture, x, y, quadX, quadY, quadWidth, quadHeight, hideOverflow, realSize, color)
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:addCornerQuads(sprites, atlas, texture, x, y, width, height, matrix, spriteWidth, spriteHeight)
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:addEdgeQuads(sprites, atlas, texture, x, y, width, height, matrix, spriteWidth, spriteHeight)
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:addMiddleQuads(sprites, atlas, texture, x, y, width, height, matrix, spriteWidth, spriteHeight)
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:getDrawableSprite()
	return {
		self
	}
end

function drawableNinePatchMt.__index:draw()
	_RYSY_unimplemented()
end

function drawableNinePatchMt.__index:setColor(color)
    local tableColor = utils.getColor(color)

    if tableColor then
        self.color = tableColor
    end
end

function drawableNinePatch.fromTexture(texture, options, drawX, drawY, drawWidth, drawHeight)
	if not _RYSY_DRAWABLE_exists(texture) then
		return nil
	end

    local ninePatch = {
        _type = "drawableNinePatch"
    }

    options = options or {}

    if type(options) == "string" then
        options = {
            mode = options
        }
    end

    local atlas = options.atlas or "Gameplay"
    --local spriteMeta = atlases.getResource(texture, atlas)
	--
    --if not spriteMeta then
    --    return
    --end

    ninePatch.atlas = atlas
    ninePatch.texture = texture
    ninePatch.useRealSize = options.useRealSize or false
    ninePatch.hideOverflow = options.hideOverflow or true
    ninePatch.mode = options.mode or "fill"
    ninePatch.borderMode = options.borderMode or "repeat"
    ninePatch.fillMode = options.fillMode or "repeat"
    ninePatch.color = utils.getColor(options.color)

    ninePatch.drawX = drawX or 0
    ninePatch.drawY = drawY or 0
    ninePatch.drawWidth = drawWidth or 0
    ninePatch.drawHeight = drawHeight or 0

    ninePatch.tileSize = options.tileSize or 8
    ninePatch.tileWidth = options.tileWidth or ninePatch.tileSize
    ninePatch.tileHeight = options.tileHeight or ninePatch.tileSize
    ninePatch.borderLeft = options.borderLeft or options.border or ninePatch.tileWidth
    ninePatch.borderRight = options.borderRight or options.border or ninePatch.tileWidth
    ninePatch.borderTop = options.borderTop or options.border or ninePatch.tileHeight
    ninePatch.borderBottom = options.borderBottom or options.border or ninePatch.tileHeight

    return setmetatable(ninePatch, drawableNinePatchMt)
end

return drawableNinePatch