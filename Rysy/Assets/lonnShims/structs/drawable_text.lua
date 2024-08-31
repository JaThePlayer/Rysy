local utils = require("utils")

local drawableText = {}
local drawableTextMt = {}

drawableTextMt.__index = {}

function drawableTextMt.__index:draw()
    _RYSY_unimplemented()
end

function drawableTextMt.__index:addToBatch(batch)
    _RYSY_unimplemented()
end

local function setColor(target, color)
    local tableColor = utils.getColor(color)

    if tableColor then
        target.color = tableColor
    end

    return tableColor ~= nil
end

function drawableTextMt.__index:setColor(color)
    return setColor(self, color)
end


function drawableText.fromText(text, x, y, width, height, font, fontSize, color)
    local drawable = {
        _type = "drawableText"
    }

    drawable.text = text

    drawable.x = x
    drawable.y = y

    drawable.width = width
    drawable.height = height

    drawable.font = font
    drawable.fontSize = fontSize
    
    if color then
        setColor(drawable, color)
    end

    return setmetatable(drawable, drawableTextMt)
end

return drawableText