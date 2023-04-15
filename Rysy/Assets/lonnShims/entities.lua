local entities = {}

local registeredEntitiesMt = {
	__index = function (self, key)
		return _RYSY_entities[key]
	end,
}

entities.registeredEntities = setmetatable({}, registeredEntitiesMt)

local seenEntityErrors = {}

local function logEntityDefinitionError(definitionName, message, room, entity)
	_RYSY_unimplemented()
end

function entities.initLogging()
	_RYSY_unimplemented()
end

-- Sets the registry to the given table (or empty one) and sets the missing entity metatable
function entities.initDefaultRegistry(t)
	_RYSY_unimplemented()
end

local function addHandler(handler, registerAt, filenameNoExt, filename, verbose)
	_RYSY_unimplemented()
end

function entities.registerEntity(filename, registerAt, verbose)
	_RYSY_unimplemented()
end

function entities.loadEntities(path, registerAt)
	_RYSY_unimplemented()
end

function entities.loadInternalEntities(registerAt)
	_RYSY_unimplemented()
end

function entities.loadExternalEntities(registerAt)
	_RYSY_unimplemented()
end

local function addAutomaticDrawableFields(handler, drawable, room, entity, isNode)
	_RYSY_unimplemented()
end

-- Returns drawable, depth
function entities.getEntityDrawable(name, handler, room, entity, viewport)
	--_RYSY_unimplemented()
	return {}
end

-- Does not check for errors
function entities.getNodeDrawableUnsafe(name, handler, room, entity, node, nodeIndex, viewport)
	_RYSY_unimplemented()
end

-- Get node drawable with pcall, return drawables from the erroring entity handler if not successful
function entities.getNodeDrawable(name, handler, room, entity, node, nodeIndex, viewport)
	_RYSY_unimplemented()
end

-- Gets entity drawables
-- Does not check for errors
function entities.getDrawableUnsafe(name, handler, room, entity, viewport)
	_RYSY_unimplemented()
end

-- Get drawable with pcall, return drawables from the erroring entity handler if not successful
function entities.getDrawable(name, handler, room, entity, viewport)
	_RYSY_unimplemented()
end

function entities.getDrawableRectangle(drawables)
	_RYSY_unimplemented()
end

function entities.getNodeRectangles(room, entity, viewport)
	_RYSY_unimplemented()
end

-- Returns main entity selection rectangle, then table of node rectangles
-- Does not check for errors
function entities.getSelectionUnsafe(room, entity, viewport, handlerOverride)
	_RYSY_unimplemented()
end

-- Get selection with pcall, return selections from the erroring entity handler if not successful
function entities.getSelection(room, entity, viewport, handlerOverride)
	_RYSY_unimplemented()
end

-- TODO - Implement in more performant way?
function entities.drawSelected(room, layer, entity, color)
	_RYSY_unimplemented()
end

-- Update the selection by calling getSelections
-- This is absolute worst case way to update the selection, movement for example can easily offset the rectangle
local function updateSelectionNaive(room, entity, node, selection)
	_RYSY_unimplemented()
end

function entities.flipSelection(room, layer, selection, horizontal, vertical)
	_RYSY_unimplemented()
end

function entities.rotateSelection(room, layer, selection, direction)
	_RYSY_unimplemented()
end

function entities.moveSelection(room, layer, selection, offsetX, offsetY)
	_RYSY_unimplemented()
end

-- Negative offsets means we are growing up/left, should move the selection as well as changing size
function entities.resizeSelection(room, layer, selection, offsetX, offsetY, directionX, directionY)
	_RYSY_unimplemented()
end

function entities.deleteSelection(room, layer, selection)
	_RYSY_unimplemented()
end

function entities.addNodeToSelection(room, layer, selection)
	_RYSY_unimplemented()
end

local function guessPlacementType(name, handler, placement)
	_RYSY_unimplemented()
end

local function getPlacements(handler)
	_RYSY_unimplemented()
end

local function getDefaultPlacement(handler, placements)
	_RYSY_unimplemented()
end

local function getPlacement(placementInfo, defaultPlacement, name, handler, language)
	_RYSY_unimplemented()
end

local function addPlacement(placementInfo, defaultPlacement, res, name, handler, language, specificMods)
	_RYSY_unimplemented()
end

-- TODO - Make more sophisticated? Works for now
local function guessPlacementFromData(item, name, handler)
	_RYSY_unimplemented()
end

function entities.getPlacements(layer, specificMods)
	_RYSY_unimplemented()
end

-- We don't know which placement this is from, but getPlacement does most of the job for us
function entities.cloneItem(room, layer, item)
	_RYSY_unimplemented()
end

function entities.placeItem(room, layer, item)
	_RYSY_unimplemented()
end

function entities.canResize(room, layer, entity)
	_RYSY_unimplemented()
end

function entities.minimumSize(room, layer, entity)
	_RYSY_unimplemented()
end

function entities.maximumSize(room, layer, entity)
	_RYSY_unimplemented()
end

function entities.nodeLimits(room, layer, entity)
	_RYSY_unimplemented()
end

function entities.nodeLineRenderType(layer, entity)
	_RYSY_unimplemented()
end

function entities.nodeVisibility(layer, entity)
	_RYSY_unimplemented()
end

function entities.ignoredFields(layer, entity)
	_RYSY_unimplemented()
end

function entities.fieldOrder(layer, entity)
	_RYSY_unimplemented()
end

function entities.fieldInformation(layer, entity)
	_RYSY_unimplemented()
end

function entities.languageData(language, layer, entity)
	_RYSY_unimplemented()
end

function entities.associatedMods(entity, layer)
	_RYSY_unimplemented()
end

-- Returns all entities of room
function entities.getRoomItems(room, layer)
    _RYSY_unimplemented()
end

return entities