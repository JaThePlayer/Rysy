local drawableText = {}
local drawableTextMt = {}
drawableTextMt.__index = {}

function drawableTextMt.__index:draw()
    _RYSY_unimplemented()
end

function drawableTextMt.__index:addToBatch(batch)
    _RYSY_unimplemented()
end

function drawableText.fromText(text, x, y, width, height, font, fontSize)
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

    return setmetatable(drawable, drawableTextMt)
end

return drawableText