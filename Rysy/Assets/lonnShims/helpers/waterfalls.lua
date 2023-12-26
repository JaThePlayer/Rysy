local drawableRectangle = require("structs.drawable_rectangle")
local xnaColors = require("consts.xna_colors")
local utils = require("utils")

local waterfallHelper = {}

-- Waterfalls --

local function waterSearchPredicate(entity)
    return entity._name == "water"
end

local function anyCollisions(rectangle, rectangles)
    for _, rect in ipairs(rectangles) do
        if utils.aabbCheck(rect, rectangle) then
            return true
        end
    end

    return false
end

local lightBlue = xnaColors.LightBlue
local waterfallFillColor = {lightBlue[1] * 0.3, lightBlue[2] * 0.3, lightBlue[3] * 0.3, 0.3}
local waterfallBorderColor = {lightBlue[1] * 0.8, lightBlue[2] * 0.8, lightBlue[3] * 0.8, 0.8}

function waterfallHelper.addWaveLineSprite(sprites, entityY, entityHeight, x, y, width, height, color)
    local rectangle = drawableRectangle.fromRectangle("fill", x, y, width, height, color)
    local bottomY = entityY + entityHeight

    if rectangle.y <= bottomY and rectangle.y + rectangle.height >= entityY then
        -- Ajust bottom
        if rectangle.y + rectangle.height > bottomY then
            rectangle.height = bottomY - rectangle.y
        end

        -- Adjust top
        if rectangle.y < entityY then
            rectangle.height += (rectangle.y - entityY)
            rectangle.y = entityY
        end

        if rectangle.height > 0 then
            table.insert(sprites, rectangle:getDrawableSprite())
        end
    end
end

-- Height for the small waterfalls
function waterfallHelper.getWaterfallHeight(room, entity)
    return _RYSY_INTERNAL_getWaterfallHeight(room, entity.x, entity.y)
end

function waterfallHelper.getWaterfallSprites(room, entity, fillColor, borderColor)
    -- use the room so that C# doesn't cache the sprites
    local _ = room.x

    -- Return a fake sprite type, and let C# handle rendering the waterfall instead.
    -- Because the original function returns a list, we need to return one as well.
    return {
        {
            _type = "_RYSY_waterfall",
            x = entity.x or 0,
            y = entity.y or 0,
            fillColor = fillColor or waterfallFillColor,
            borderColor = borderColor or waterfallBorderColor,
        }
    }
end

function waterfallHelper.getWaterfallRectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local height = waterfallHelper.getWaterfallHeight(room, entity)

    return utils.rectangle(x - 1, y, 10, height)
end

-- Big Waterfalls --

-- Different color depending on layer
-- The background color might be incorrect
local function getBigWaterfallColors(entity)
    local foreground = waterfallHelper.isForeground(entity)

    if foreground then
        local baseColor = xnaColors.LightBlue
        local fillColor = {baseColor[1] * 0.3, baseColor[2] * 0.3, baseColor[3] * 0.3, 0.3}
        local borderColor = {baseColor[1] * 0.8, baseColor[2] * 0.8, baseColor[3] * 0.8, 0.8}

        return fillColor, borderColor

    else
        local fillSuccess, fillR, fillG, fillB = utils.parseHexColor("29a7ea")
        local borderSuccess, borderR, borderG, borderB = utils.parseHexColor("89dbf0")
        local fillColor = {fillR * 0.3, fillB * 0.3, fillG * 0.3, 0.3}
        local borderColor = {borderR * 0.5, borderG * 0.5, borderB * 0.5, 0.5}

        return fillColor, borderColor
    end
end

function waterfallHelper.isForeground(entity)
    return entity.layer == "FG"
end

-- Different gap depending on layer
function waterfallHelper.getBorderOffsetorderOffset(entity)
    local foreground = waterfallHelper.isForeground(entity)

    return foreground and 2 or 3
end

function waterfallHelper.getBigWaterfallSprite(room, entity, fillColor, borderColor)
    if not fillColor or not borderColor then
        local defaultFillColor, defaultBorderColor = getBigWaterfallColors(entity)

        fillColor = fillColor or defaultFillColor
        borderColor = borderColor or defaultBorderColor
    end
    
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 64
    
    -- Return a fake sprite type, and let C# handle rendering the waterfall instead.
    -- Because the original function returns a list, we need to return one as well.
    return {
        {
            _type = "_RYSY_big_waterfall",
            x = x,
            y = y,
            w = width,
            h = height,
            fillColor = fillColor,
            borderColor = borderColor,
            fg = waterfallHelper.isForeground(entity)
        }
    }
end

function waterfallHelper.getBigWaterfallRectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 64

    return utils.rectangle(x, y, width, height)
end

return waterfallHelper