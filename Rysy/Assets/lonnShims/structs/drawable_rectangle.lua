﻿-- A spritebatchable rectangle drawing implementation
-- Stretches a 1x1 white pixel to achieve the same effect

local utils = require("utils")
local drawing = require("utils.drawing")
local drawableSprite = require("structs.drawable_sprite")

local drawableRectangle = {}

drawableRectangle.tintingPixelTexture = "1x1-tinting-pixel"

local function getDrawableSpriteForRectangle(x, y, width, height, color)
    local data = {}

    data.x = x
    data.y = y

    data.scaleX = width
    data.scaleY = height

    data.justificationX = 0
    data.justificationY = 0

    data.color = utils.getColor(color)

    return drawableSprite.fromInternalTexture(drawableRectangle.tintingPixelTexture, data)
end


local drawableRectangleMt = {}
drawableRectangleMt.__index = {}

function drawableRectangleMt.__index:getRectangleRaw()
    return self.x, self.y, self.width, self.height
end

function drawableRectangleMt.__index:getRectangle()
    return utils.rectangle(self:getRectangleRaw())
end

function drawableRectangleMt.__index:drawRectangle(mode, color, secondaryColor)
    _RYSY_unimplemented()
end

-- Gets a drawable sprite, using a stretched version of the 1x1 tintable
-- Horizontal lines for "line" and "bordered" are offset to not overlap in the corners
function drawableRectangleMt.__index:getDrawableSprite()
    local mode = self.mode or "fill"

    if mode == "fill" then
        return getDrawableSpriteForRectangle(self.x, self.y, self.width, self.height, self.color)

    elseif mode == "line" then
        return {
            getDrawableSpriteForRectangle(self.x + 1, self.y, self.width - 2, 1, self.color),
            getDrawableSpriteForRectangle(self.x + 1, self.y + self.height - 1, self.width - 2, 1, self.color),
            getDrawableSpriteForRectangle(self.x, self.y, 1, self.height, self.color),
            getDrawableSpriteForRectangle(self.x + self.width - 1, self.y, 1, self.height, self.color)
        }

    elseif mode == "bordered" then
        -- Simplified if only the border is visible
        if self.width <= 2 or self.height <= 2 then
            return {
                getDrawableSpriteForRectangle(self.x, self.y, self.width, self.height, self.secondaryColor)
            }

        else
            return {
                getDrawableSpriteForRectangle(self.x + 1, self.y + 1, self.width - 2, self.height - 2, self.color),
                getDrawableSpriteForRectangle(self.x + 1, self.y, self.width - 2, 1, self.secondaryColor),
                getDrawableSpriteForRectangle(self.x + 1, self.y + self.height - 1, self.width - 2, 1, self.secondaryColor),
                getDrawableSpriteForRectangle(self.x, self.y, 1, self.height, self.secondaryColor),
                getDrawableSpriteForRectangle(self.x + self.width - 1, self.y, 1, self.height, self.secondaryColor)
            }
        end
    end
end

function drawableRectangleMt.__index:draw()
    self:drawRectangle(self.mode, self.color)
end

function drawableRectangleMt.__index:setColor(color, secondaryColor)
    local tableColor = utils.getColor(color)
    local tableSecondaryColor = utils.getColor(secondaryColor)

    if tableColor then
        self.color = tableColor
    end

    if tableSecondaryColor then
        self.secondaryColor = tableSecondaryColor
    end
end

-- Accepting rectangles on `x` argument, or passing in the values manually
function drawableRectangle.fromRectangle(mode, x, y, width, height, color, secondaryColor)
    local rectangle = {
        _type = "drawableRectangle"
    }

    rectangle.mode = mode

    if type(x) == "table" then
        rectangle.x = x.x or x[1]
        rectangle.y = x.y or x[2]

        rectangle.width = x.width or x[3]
        rectangle.height = x.height or x[4]

        rectangle.color = utils.getColor(y)
        rectangle.secondaryColor = utils.getColor(width)

    else
        rectangle.x = x
        rectangle.y = y

        rectangle.width = width
        rectangle.height = height

        rectangle.color = utils.getColor(color)
        rectangle.secondaryColor = utils.getColor(secondaryColor)
    end

    return setmetatable(rectangle, drawableRectangleMt)
end

return drawableRectangle