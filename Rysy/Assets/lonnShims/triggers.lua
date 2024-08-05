local utils = require("utils")
local modHandler = require("mods")
local logging = require("logging")
local depths = require("consts.object_depths")

local drawing = require("utils.drawing")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableText = require("structs.drawable_text")

local colors = require("consts.colors")

local triggers = {}

missingTriggerHandler = {}

local triggerRegisteryMT = {
    __index = function() return missingTriggerHandler end
}

triggers.triggerFontSize = 1
triggers.registeredTriggers = nil

-- Sets the registry to the given table (or empty one)
function triggers.initDefaultRegistry(t)
    _RYSY_unimplemented()
end

local function addHandler(handler, registerAt, filenameNoExt, filename, verbose)
    _RYSY_unimplemented()
end

function triggers.registerTrigger(filename, registerAt, verbose)
    _RYSY_unimplemented()
end

function triggers.loadTriggers(path, registerAt)
    _RYSY_unimplemented()
end

function triggers.loadInternalTriggers(registerAt)
    _RYSY_unimplemented()
end

function triggers.loadExternalTriggers(registerAt)
    _RYSY_unimplemented()
end

local humanizedNameCache = {}
local humanizedNameTrimmedModNameCache = {}

function triggers.getDrawableDisplayText(trigger)
    return _RYSY_triggers_getDrawableDisplayTextForSid(trigger._name)
end

function triggers.getCategory(trigger)
    return "general"
end

function triggers.triggerColor(room, trigger)
    return colors.triggerColor, colors.triggerBorderColor
end

function triggers.triggerText(room, trigger)
    local name = trigger._name
    local handler = triggers.registeredTriggers[name]
    local fallbackText = triggers.getDrawableDisplayText(trigger)

    if handler.triggerText then
        if utils.isCallable(handler.triggerText) then
            return handler.triggerText(room, trigger) or fallbackText

        else
            return handler.triggerText or fallbackText
        end
    end

    return fallbackText
end

-- Returns drawable, depth
function triggers.getDrawable(name, handler, room, trigger, viewport)
    local displayName = triggers.triggerText(room, trigger)

    local x = trigger.x or 0
    local y = trigger.y or 0

    local width = trigger.width or 16
    local height = trigger.height or 16

    local fillColor, borderColor, textColor = triggers.triggerColor(room, trigger)
    local borderedRectangle = drawableRectangle.fromRectangle("bordered", x, y, width, height, fillColor, borderColor)
    local textDrawable = drawableText.fromText(displayName, x, y, width, height, nil, triggers.triggerFontSize, textColor)

    local drawables = borderedRectangle:getDrawableSprite()
    table.insert(drawables, textDrawable)

    textDrawable.depth = depths.triggers - 1

    return drawables, depths.triggers
end

-- Returns main trigger selection rectangle, then table of node rectangles
function triggers.getSelection(room, trigger)
    _RYSY_unimplemented()
end

function triggers.drawSelected(room, layer, trigger, color)
    _RYSY_unimplemented()
end

local function updateSelectionNaive(room, trigger, node, selection)
    _RYSY_unimplemented()
end

function triggers.areaFlipSelection(room, layer, selection, horizontal, vertical, area)
    _RYSY_unimplemented()
end

function triggers.moveSelection(room, layer, selection, offsetX, offsetY)
    _RYSY_unimplemented()
end

-- Negative offsets means we are growing up/left, should move the selection as well as changing size
function triggers.resizeSelection(room, layer, selection, offsetX, offsetY, directionX, directionY)
    _RYSY_unimplemented()
end

function triggers.deleteSelection(room, layer, selection)
    _RYSY_unimplemented()
end

function triggers.addNodeToSelection(room, layer, selection)
    _RYSY_unimplemented()
end

function triggers.ignoredSimilarityKeys(trigger)
    _RYSY_unimplemented()
end

function triggers.selectionsSimilar(selectionA, selectionB, strict)
    _RYSY_unimplemented()
end

-- Returns all triggers of room
function triggers.getRoomItems(room, layer)
    return room.triggers
end

local function selectionRenderFilterPredicate(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.selectionFilterPredicate(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.renderFilterPredicate(room, trigger)
    _RYSY_unimplemented()
end

local function getPlacements(handler)
    _RYSY_unimplemented()
end

local function getDefaultPlacement(handler, placements)
    _RYSY_unimplemented()
end

local function getPlacementLanguage(language, triggerName, name, key, default)
    _RYSY_unimplemented()
end

local function getAlternativeDisplayNames(placementInfo, name, language)
    _RYSY_unimplemented()
end

local function getPlacement(placementInfo, defaultPlacement, name, handler, language)
    _RYSY_unimplemented()
end

local function addPlacement(placementInfo, defaultPlacement, res, name, handler, language, specificMods)
    _RYSY_unimplemented()
end

local function guessPlacementFromData(item, name, handler)
    _RYSY_unimplemented()
end

function triggers.getPlacements(layer, specificMods)
    _RYSY_unimplemented()
end

function triggers.cloneItem(room, layer, item)
    _RYSY_unimplemented()
end

function triggers.placeItem(room, layer, item)
    _RYSY_unimplemented()
end

function triggers.getHandler(trigger)
    _RYSY_unimplemented()
end

function triggers.getHandlerValue(trigger, room, key, ...)
    _RYSY_unimplemented()
end

function triggers.canResize(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.minimumSize(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.maximumSize(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.warnBelowSize(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.warnAboveSize(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.nodeLimits(room, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.nodeLineRenderType(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.nodeVisibility(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.ignoredFields(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.ignoredFieldsMultiple(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.fieldOrder(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.fieldInformation(layer, trigger)
    _RYSY_unimplemented()
end

function triggers.languageData(language, layer, trigger)
    _RYSY_unimplemented()
end

function triggers.associatedMods(trigger, layer)
    _RYSY_unimplemented()
end

return triggers