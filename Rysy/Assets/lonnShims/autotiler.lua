local utils = require("utils")
local matrix = require("utils.matrix")
local bit = require("bit")

local autotiler = {}

function autotiler.checkTile(value, target, ignore, air, wildcard)
    if ignore then
        return not (target == air or ignore[target] or (ignore[wildcard] and value ~= target))
    end

    return target ~= air
end

function autotiler.getQuads(x, y, tiles, meta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile)
    -- TODO: reverse-engineer returned format and try to get this working via C#, auspicioushelper uses this! 
    _RYSY_unimplemented()
end

function autotiler.getQuadsWithBitmask(x, y, tiles, meta, airTile, emptyTile, wildcard, defaultQuad, defaultSprite, checkTile, lshift, bxor, band)
    _RYSY_unimplemented()
end

local function convertTileString(s)
    local res = {}
    local parts = $(s):split(";")

    for i, part <- parts do
        local numbers = $(part):split(",")

        table.insert(res, {
            tonumber(numbers[1]),
            tonumber(numbers[2])
        })
    end

    return res
end

-- X values are stored as nil in the mask matrix
local function countMaskXs(mask)
    local maskMatrix = mask.mask
    local width, height = maskMatrix:size()
    local count = 0

    for x = 1, width do
        for y = 1, height do
            if maskMatrix:getInbounds(x, y) == nil then
                count += 1
            end
        end
    end

    return count
end

local function maskCompare(lhs, rhs)
    return countMaskXs(lhs) < countMaskXs(rhs)
end

-- Inline mask sort, more X -> later
local function sortTilesetMasks(masks)
    table.sort(masks, maskCompare)

    return masks
end

local function tileStringHashFunction(value)
    return string.format("%s, %s", value[1], value[2])
end

local function getTilesetStructure(id)
    return {
        id = id,
        path = "",
        padding = {},
        center = {},
        masks = {},
        ignores = {}
    }
end

local function readTilesetInfo(tileset, id, element)
    _RYSY_unimplemented()
end

function autotiler.loadTilesetXML(filename)
    _RYSY_unimplemented()
end

return autotiler