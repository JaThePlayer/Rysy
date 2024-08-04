local utils = require("utils")
local matrix = require("utils.matrix")

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

--[[
function fakeTilesHelper.getCombinedMaterialMatrix(entities, materialKey, default)
    _RYSY_unimplemented()
end

function fakeTilesHelper.getCombinedEntitySpriteFunction(entities, materialKey, blendIn, layer, color, x, y)
	print("yo")
    _RYSY_unimplemented()
end
]]

local function getMaterialCorners(entities)
    local tlx, tly = math.huge, math.huge
    local brx, bry = -math.huge, -math.huge

    for _, entity in ipairs(entities) do
        tlx = math.min(tlx, entity.x)
        tly = math.min(tly, entity.y)
        brx = math.max(brx, entity.x + entity.width)
        bry = math.max(bry, entity.y + entity.height)
    end

    return tlx, tly, brx, bry
end

function fakeTilesHelper.getCombinedMaterialMatrix(entities, materialKey, default)
    local tlx, tly, brx, bry = getMaterialCorners(entities)
    local materialWidth, materialHeight = math.ceil((brx - tlx) / 8), math.ceil((bry - tly) / 8)
    local materialMatrix = matrix.filled(default or "0", materialWidth, materialHeight)
    local fakeEntity = {
        x = tlx,
        y = tly
    }

    for _, entity in ipairs(entities) do
        local x, y = math.floor((entity.x - tlx) / 8), math.floor((entity.y - tly) / 8)
        local width, height = math.ceil(entity.width / 8), math.ceil(entity.height / 8)

        -- Vanilla maps might have tileset ids stored as integers
        local material = tostring(entity[materialKey] or "3")

        for i = 1, width do
            for j = 1, height do
                materialMatrix:set(x + i, y + j, material)
            end
        end
    end

    return materialMatrix, fakeEntity
end

function fakeTilesHelper.getCombinedEntitySpriteFunction(entities, materialKey, blendIn, layer, color, x, y)
    local materialMatrix, fakeEntity = fakeTilesHelper.getCombinedMaterialMatrix(entities, materialKey)
	local f = fakeTilesHelper.getEntitySpriteFunction(materialKey, blendIn, layer, color, x, y)

	-- todo: handle blending
    return function(room)
		local sprites = {}
		for _, entity in ipairs(entities) do
			local sprite = f(room, entity)

			table.insert(sprites, sprite)
		end

		return sprites
    end
end

-- Make sure to get this in a function if used for fieldInformation, otherwise it won't update!
function fakeTilesHelper.getTilesOptions(layer)
    layer = layer or "tilesFg"

	return _RYSY_fake_tiles_get(layer)
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

function fakeTilesHelper.getPlacementMaterial(fallback, layer, allowAir)
    fallback = fallback or "3"
    layer = layer or "tilesFg"

    local material = _RYSY_fakeTilesTileMaterialForLayer(layer) or fallback

    if not allowAir then
        if material == " " or material == "0" then
            material = fallback
        end
    end

    return material
end

return fakeTilesHelper