local colors = require("consts.colors")
local utils = require("utils")
local atlases = require("atlases")
local drawableRectangle = require("structs.drawable_rectangle")
local viewportHandler = require("viewport_handler")
local matrix = require("utils.matrix")
local bit = require("bit")
local modHandler = require("mods")
local depths = require("consts.object_depths")
local logging = require("logging")
local modificationWarner = require("modification_warner")

local entityHandler = require("entities")
local triggerHandler = require("triggers")
local decalHandler = require("decals")

local celesteRender = {}

--[[
local autotiler = require("autotiler")
local fileLocations = require("file_locations")
local tilesetFileFg = utils.joinpath(fileLocations.getCelesteDir(), "Content", "Graphics", "ForegroundTiles.xml")
local tilesetFileBg = utils.joinpath(fileLocations.getCelesteDir(), "Content", "Graphics", "BackgroundTiles.xml")

celesteRender.tilesMetaFgVanilla = autotiler.loadTilesetXML(tilesetFileFg)
celesteRender.tilesMetaBgVanilla = autotiler.loadTilesetXML(tilesetFileBg)

celesteRender.tilesMetaFg = celesteRender.tilesMetaFgVanilla
celesteRender.tilesMetaBg = celesteRender.tilesMetaBgVanilla
]]
celesteRender.tilesSpriteMetaCache = {}
celesteRender.tilesSceneryMetaCache = {}

local tilesFgDepth = depths.fgTerrain
local tilesBgDepth = depths.bgTerrain

local decalsFgDepth = depths.fgDecals
local decalsBgDepth = depths.bgDecals

local triggersDepth = depths.triggers

local YIELD_RATE = 100
local PRINT_BATCHING_DURATION = false
local ALWAYS_REDRAW_UNSELECTED_ROOMS = false
local ALLOW_NON_VISIBLE_BACKGROUND_DRAWING = false

local SCENERY_GAMEPLAY_PATH = "tilesets/scenery"

local roomCache = {}
local roomRandomMatrixCache = {}

local batchingTasks = {}

function celesteRender.loadCustomTilesetAutotiler(state)
    _RYSY_unimplemented()
end

function celesteRender.sortBatchingTasks(state, taskList)
    _RYSY_unimplemented()
end

function celesteRender.processTasks(state, calcTime, maxTasks, backgroundTime, backgroundTasks)
end

function celesteRender.clearBatchingTasks()
end

function celesteRender.releaseBatch(roomName, key)
end

function celesteRender.invalidateRoomCache(roomName, key)
    -- TODO: would this make sense in Rysy?
end

function celesteRender.getRoomRandomMatrix(room, key)
    local roomName = room.name
    local tileWidth, tileHeight = room[key].matrix:size()
    local regen = false

    if roomRandomMatrixCache[roomName] and roomRandomMatrixCache[roomName][key] then
        local m = roomRandomMatrixCache[roomName][key]
        local randWidth, randHeight = m:size()

        regen = tileWidth ~= randWidth or tileHeight ~= randHeight

    else
        regen = true
    end

    if regen then
        utils.setRandomSeed(roomName)

        local m = matrix.fromFunction(math.random, tileWidth, tileHeight)

        roomRandomMatrixCache[roomName] = roomRandomMatrixCache[roomName] or {}
        roomRandomMatrixCache[roomName][key] = m
    end

    return roomRandomMatrixCache[roomName][key]
end

function celesteRender.getRoomCache(roomName, key)
    return false
end

function celesteRender.getRoomBackgroundColor(room, selected, state)
    if not state.showRoomBackground then
        return nil
    end

    local roomColor = room.color or 0
    local color = colors.roomBackgroundDefault

    if roomColor >= 0 and roomColor < #colors.roomBackgroundColors then
        color = colors.roomBackgroundColors[roomColor + 1]
    end

    local r, g, b = color[1], color[2], color[3]
    local a = selected and 1.0 or 0.3

    return {r, g, b, a}
end

function celesteRender.getRoomBorderColor(room, selected, state)
    if not state.showRoomBorders then
        return nil
    end

    local roomColor = room.color or 0
    local color = colors.roomBorderDefault

    if roomColor >= 0 and roomColor < #colors.roomBorderColors then
        color = colors.roomBorderColors[roomColor + 1]
    end

    return color
end

function celesteRender.getSceneryMeta()
    return atlases.gameplay[SCENERY_GAMEPLAY_PATH]
end

function celesteRender.clearTileSpriteQuadCache()
end

function celesteRender.clearScenerySpriteQuadCache()
end

function celesteRender.getOrCacheTileSpriteQuad(cache, tile, texture, quad, fg)
    _RYSY_unimplemented()
end

function celesteRender.getOrCacheScenerySpriteQuad(index)
    _RYSY_unimplemented()
end

function celesteRender.drawInvalidTiles(batch, missingTiles, fg)
    _RYSY_unimplemented()
end

-- randomMatrix is for custom randomness, mostly to give the correct "slice" of the matrix when making fake tiles
function celesteRender.getTilesBatch(room, tiles, meta, scenery, fg, randomMatrix, batchMode, shouldYield)
    _RYSY_unimplemented()
end

local function getRoomTileBatch(room, tiles, fg)
    _RYSY_unimplemented()
end

function celesteRender.getTilesFgBatch(room, tiles, viewport)
    _RYSY_unimplemented()
end

function celesteRender.getTilesBgBatch(room, tiles, viewport)
    _RYSY_unimplemented()
end

local function getRoomDecalsBatch(room, decals, fg, viewport)
    _RYSY_unimplemented()
end

function celesteRender.getDecalsFgBatch(room, decals, viewport)
    _RYSY_unimplemented()
end

function celesteRender.getDecalsBgBatch(room, decals, viewport)
    _RYSY_unimplemented()
end

function celesteRender.drawDecalsFg(room, decals)
    _RYSY_unimplemented()
end

function celesteRender.drawDecalsBg(room, decals)
    _RYSY_unimplemented()
end

local function getEntityBatchTaskFunc(room, entities, viewport, registeredEntities)
    _RYSY_unimplemented()
end

function celesteRender.getEntityBatch(room, entities, viewport, registeredEntities, forceRedraw)
    _RYSY_unimplemented()
end

local function getTriggerBatchTaskFunc(room, triggers, viewport, registeredTriggers)
    _RYSY_unimplemented()
end

function celesteRender.getTriggerBatch(room, triggers, viewport, registeredTriggers, forceRedraw)
    _RYSY_unimplemented()
end

function celesteRender.forceRoomBatchRender(room, state)
    _RYSY_unimplemented()
end

function celesteRender.getRoomBatches(room, state)
    _RYSY_unimplemented()
end

function celesteRender.forceRoomCanvasRender(room, state, selected)
end

function celesteRender.forceRedrawRoom(room, state, selected)
end

function celesteRender.forceRedrawVisibleRooms(rooms, state, selectedItem, selectedItemType)
end

function celesteRender.drawRooms(rooms, state, selectedItem, selectedItemType)
end

function celesteRender.drawRoom(room, state, selected, visible)
end

function celesteRender.drawFillers(fillers, state, selectedItem, selectedItemType)
end

function celesteRender.drawFiller(filler, viewport)
end

function celesteRender.drawMap(state)
end

modificationWarner.addModificationWarner(celesteRender)

return celesteRender