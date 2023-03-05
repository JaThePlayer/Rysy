local utils = require("utils")

local fakeTilesHelper = {}

function fakeTilesHelper.generateFakeTilesMatrix(room, x, y, material, layer, blendIn)
    _RYSY_unimplemented()
end

function fakeTilesHelper.generateFakeTiles(room, x, y, material, layer, blendIn)
    _RYSY_unimplemented()
end

function fakeTilesHelper.getMaterialMatrix(entity, material)
    _RYSY_unimplemented()
end

function fakeTilesHelper.generateFakeTilesBatch(room, x, y, fakeTiles, layer)
    _RYSY_unimplemented()
end

function fakeTilesHelper.generateFakeTilesSprites(room, x, y, fakeTiles, layer, offsetX, offsetY, color)
    _RYSY_unimplemented()
end

-- Material key might be a material itself
local function getEntityMaterialFromKey(entity, materialKey)
    local materialKeyType = utils.typeof(materialKey)
    local fromKey = entity[materialKey]
    local fromKeyType = type(fromKey)

    -- Vanilla maps might have tileset ids stored as integers
    if fromKeyType == "number" and utils.isInteger(fromKey) then
        fromKeyType = "string"
        fromKey = tostring(fromKey)
    end

    if fromKeyType == "string" and utf8.len(fromKey) == 1 then
        return fromKey

    elseif materialKeyType == "string" and utf8.len(materialKey) == 1 then
        return materialKey

    elseif materialKeyType == "matrix" then
        return materialKey
    end

    return "3"
end

-- Blend mode key can also be a boolean
local function getEntityBlendMode(entity, blendModeKey)
    if type(blendModeKey) == "string" then
        return entity[blendModeKey]
    end

    return blendModeKey
end

function fakeTilesHelper.getEntitySpriteFunction(materialKey, blendKey, layer, color, x, y)
    layer = layer or "tilesFg"

    return function(room, entity)
        local x = entity.x
        local y = entity.y
        local material = getEntityMaterialFromKey(entity, materialKey)
        local w = entity.width or 8
        local h = entity.height or 8

        return {
            _type = "_RYSY_fakeTiles",
            x = x, y = y, w = w, h = h, material = material, layer = layer
        }
    end
end

function fakeTilesHelper.getCombinedMaterialMatrix(entities, materialKey, default)
    _RYSY_unimplemented()
end

function fakeTilesHelper.getCombinedEntitySpriteFunction(entities, materialKey, blendIn, layer, color, x, y)
    _RYSY_unimplemented()
end

-- Make sure to get this in a function if used for fieldInformation, otherwise it won't update!
function fakeTilesHelper.getTilesOptions(layer)
    layer = layer or "tilesFg"

    local validTiles = brushes.getValidTiles(layer, false)
    local tileOptions = {}

    for id, path in pairs(validTiles) do
        local displayName = brushes.cleanMaterialPath(path)

        tileOptions[displayName] = id
    end

    return tileOptions
end

-- Returns a function to be up to date with any XML changes
function fakeTilesHelper.addTileFieldInformation(fieldInformation, materialKey, layer, room, entity)
    return function()
        if type(fieldInformation) == "function" then
            fieldInformation = fieldInformation(room, entity)
        end

        fieldInformation[materialKey] = {
            options = fakeTilesHelper.getTilesOptions(layer),
            editable = false
        }

        return fieldInformation
    end
end

-- Returns a function to be up to date with any XML changes
function fakeTilesHelper.getFieldInformation(materialKey, layer)
    return function()
        return {
            [materialKey] = {
                options = fakeTilesHelper.getTilesOptions(layer),
                editable = false
            }
        }
    end
end

return fakeTilesHelper