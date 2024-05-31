-- todo: reimplement in c# for performance

local utils = require("utils")

local connectedEntities = {}

-- Useful for adding a placement preview entity into the entities list
function connectedEntities.appendIfMissing(entities, target)
    for _, entity in ipairs(entities) do
        if entity == target then
            return true
        end
    end

    table.insert(entities, target)

    return false
end

function connectedEntities.getEntityRectangles(entities)
    local rectangles = {}
    local seenExtra = false

    for _, entity in ipairs(entities) do
        table.insert(rectangles, utils.rectangle(entity.x, entity.y, entity.width, entity.height))

        if entity == extra then
            seenExtra = true
        end
    end

    return rectangles
end

function connectedEntities.hasAdjacent(entity, offsetX, offsetY, rectangles, checkWidth, checkHeight)
    local x, y = entity.x or 0, entity.y or 0
    local checkX, checkY = x + offsetX, y + offsetY
    
    return _RYSY_CONNECTED_ENTITIES_hasAdjacent(rectangles, checkX, checkY, checkWidth or 8, checkHeight or 8)
--[[
    for _, rect in ipairs(rectangles) do
    -- (x1, y1, w1, h1, x2, y2, w2, h2)
    -- not (x2 >= x1 + w1 or x2 + w2 <= x1 or y2 >= y1 + h1 or y2 + h2 <= y1)
        if utils.aabbCheckInline(rect.x, rect.y, rect.width, rect.height, checkX, checkY, checkWidth or 8, checkHeight or 8) then
            return true
        end
    end

    return false
]]
end

return connectedEntities