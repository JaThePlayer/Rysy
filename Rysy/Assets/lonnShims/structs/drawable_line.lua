local utils = require("utils")

local drawableLine = {}

local drawableLineMt = {}
drawableLineMt.__index = {}

function drawableLineMt.__index:getDrawableSprite()
    -- this is not lonn code, but lonn's code breaks in Rysy for some reason, and this is more efficient anyway :p
    return {
        self
    }
end

function drawableLineMt.__index:setOffset(x, y)
    self.offsetX = x
    self.offsetY = y
end

function drawableLineMt.__index:setMagnitudeOffset(offset)
    self.magnitudeOffset = offset
end

function drawableLineMt.__index:setThickness(thickness)
    self.thickness = thickness
end

function drawableLineMt.__index:setColor(color)
    local tableColor = utils.getColor(color)

    if tableColor then
        self.color = tableColor
    end
end

function drawableLineMt.__index:draw()
    _RYSY_unimplemented()
end

function drawableLine.fromPoints(points, color, thickness, offsetX, offsetY, magnitudeOffset)
    local line = {
        _type = "drawableLine"
    }

    line.points = points
    line.color = utils.getColor(color)
    line.thickness = thickness or 1
    line.offsetX = offsetX or 0
    line.offsetY = offsetY or 0
    line.magnitudeOffset = magnitudeOffset or 0

    return setmetatable(line, drawableLineMt)
end

return drawableLine