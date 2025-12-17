local atlases = require("atlases")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local mods = require("mods")
local modificationWarner = require("modification_warner")
--local subLayers = require("sub_layers")

local languageRegistry = require("language_registry")

local decals = {}

local zipFileNamesCache = {}
local zipFileNamesNoAnimationsCache = {}

local decalsPrefix = "decals/"
local gameplayPath = "Graphics/Atlases/Gameplay"
local decalsPath = "Graphics/Atlases/Gameplay/decals"

-- A frame should only be kept if it has no trailing number
-- Or if the trailing number is 0, 00, 000, ... etc
-- Using manual byte checks for performance reasons
local function keepFrame(name, removeAnimationFrames)
    if removeAnimationFrames then
        for i = #name, 1, -1 do
            local byte = name:byte(i, i)
            local isNumber = byte >= 48 and byte <= 57

            if isNumber then
                local isZero = byte == 48

                if not isZero then
                    return false
                end

            else
                return true
            end
        end
    end

    return true
end

function decals.getDecalNamesFromMod(modFolder, removeAnimationFrames, yield, names, associatedMods, added)
    return {}, {}
end

function decals.getDecalNames(specificMods, removeAnimationFrames, yield)
    return {}, {}
end

function decals.getDrawable(texture, handler, room, decal, viewport)
    local x = decal.x or 0
    local y = decal.y or 0

    local scaleX = decal.scaleX or 1
    local scaleY = decal.scaleY or 1

    local rotation = math.rad(decal.rotation or 0)
    local depth = decal.depth

    local drawable = drawableSprite.fromTexture(texture, decal)
    if drawable then
        drawable.rotation = rotation
        drawable:setScale(scaleX, scaleY)
        drawable:setJustification(0.5, 0.5)
    
        return drawable, depth
    end
end

function decals.getSelection(room, decal)
    local drawable = decals.getDrawable(decal.texture, nil, room, decal, nil)

    if drawable then
        return drawable:getRectangle()

    else
        return utils.rectangle(decal.x - 2, decal.y - 2, 5, 5)
    end
end

local function updateSelection(selection, room, decal, layer)
    local newSelectionRectangle = decals.getSelection(room, decal)

    selection.x = newSelectionRectangle.x
    selection.y = newSelectionRectangle.y

    selection.width = newSelectionRectangle.width
    selection.height = newSelectionRectangle.height

    selection.layer = layer or selection.layer
end

function decals.moveSelection(room, layer, selection, x, y)
    local decal = selection.item

    decal.x += x
    decal.y += y

    selection.x += x
    selection.y += y

    return true
end

function decals.resizeSelection(room, layer, selection, offsetX, offsetY, directionX, directionY)
    local decal = selection.item

    local absX = math.abs(decal.scaleX)
    local absY = math.abs(decal.scaleY)

    local signX = utils.sign(decal.scaleX)
    local signY = utils.sign(decal.scaleY)

    if offsetX < 0 then
        decal.scaleX = math.max(absX / 2, 1.0) * signX

    elseif offsetX > 0 then
        decal.scaleX = math.min(absX * 2, 2^4) * signX
    end

    if offsetY < 0 then
        decal.scaleY = math.max(absY / 2, 1.0) * signY

    elseif offsetY > 0 then
        decal.scaleY = math.min(absY * 2, 2^4) * signY
    end

    if offsetX ~= 0 or offsetY ~= 0 then
        updateSelection(selection, room, decal, layer)
    end

    return offsetX ~= 0 or offsetY ~= 0
end

function decals.flipSelection(room, layer, selection, horizontal, vertical)
    local decal = selection.item

    if horizontal then
        decal.scaleX *= -1
    end

    if vertical then
        decal.scaleY *= -1
    end

    if horizontal or vertical then
        updateSelection(selection, room, decal, layer)
    end

    return horizontal or vertical
end

function decals.areaFlipSelection(room, layer, selection, horizontal, vertical, area)
    local decal = selection.item

    if horizontal then
        decal.scaleX *= -1
        decal.x = 2 * area.x + area.width - decal.x
    end

    if vertical then
        decal.scaleY *= -1
        decal.y = 2 * area.y + area.height - decal.y
    end

    if horizontal or vertical then
        updateSelection(selection, room, decal, layer)
    end

    return horizontal or vertical
end

function decals.rotateSelection(room, layer, selection, direction)
    local decal = selection.item

    if direction ~= 0 then
        decal.rotation = ((decal.rotation or 0) + direction * 90) % 360

        updateSelection(selection, room, decal, layer)
    end

    return direction ~= 0
end

function decals.deleteSelection(room, layer, selection)
    local targets = decals.getRoomItems(room, layer)
    local target = selection.item

    for i, decal in ipairs(targets) do
        if decal == target then
            table.remove(targets, i)

            return true
        end
    end

    return false
end

function decals.getPlacements(layer, specificMods)
    return {}
end

function decals.cloneItem(room, layer, item)
    local texture = item.texture
    local textureNoDecalPrefix = texture:sub(8)

    local placement = {
        name = texture,
        displayName = textureNoDecalPrefix,
        layer = layer,
        placementType = "point",
        itemTemplate = utils.deepcopy(item)
    }

    return placement
end

function decals.placeItem(room, layer, item)
    local items = decals.getRoomItems(room, layer)

    -- TODO: this does not actually work.
    table.insert(items, item)

    return true
end

-- Returns all decals of room
function decals.getRoomItems(room, layer)
    return layer == "decalsFg" and room.decalsFg or room.decalsBg
end

local function selectionRenderFilterPredicate(room, layer, subLayer,  decal)
    return true
end

function decals.selectionFilterPredicate(room, layer, subLayer, decal)
    return selectionRenderFilterPredicate(room, layer, subLayer, decal)
end

function decals.renderFilterPredicate(room, decal, fg)
    return selectionRenderFilterPredicate(room, fg and "decalsFg" or "decalsBg", nil, decal)
end

function decals.ignoredFields(layer, decal)
    return {}
end

function decals.ignoredFieldsMultiple(layer, decal)
    return {"x", "y"}
end

function decals.fieldOrder(layer, decal)
    return {"x", "y", "scaleX", "scaleY", "texture", "depth", "rotation", "color"}
end

function decals.fieldInformation(layer, decal)
    return {
        color = {
            fieldType = "color",
            useAlpha = true,
        },
        texture = {
            fieldType = "decalTexture"
        },
        depth = {
            fieldType = "integer",
            allowEmpty = true
        }
    }
end

function decals.selectionsSimilar(selectionA, selectionB, strict)
    local decalA = selectionA.item
    local decalB = selectionB.item
    local sameDecalTexture = decalA.texture == decalB.texture

    if strict and sameDecalTexture then
        return decalA.scaleX == decalB.scaleX and
            decalA.scaleY == decalB.scaleY and
            decalA.rotation == decalB.rotation and
            decalA.color == decalB.color
    end

    return sameDecalTexture
end

function decals.languageData(language, layer, decal)
    return language.decals
end

function decals.associatedMods(decal, layer)
    local texture = decal.texture
    local fullFilename = string.format("%s/%s/%s.png", mods.commonModContent, gameplayPath, texture)
    local modMetadata = mods.getModMetadataFromPath(fullFilename)

    if modMetadata then
        return mods.getModNamesFromMetadata(modMetadata)
    end
end

modificationWarner.addModificationWarner(decals)

return decals