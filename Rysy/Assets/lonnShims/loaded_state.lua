local state = {}

state.currentSaves = {}
state.pendingSaves = {}

-- Calls before save functions
function state.defaultBeforeSaveCallback(filename, state)
    _RYSY_unimplemented()
end

-- Updates state filename and flags history with no changes
function state.defaultAfterSaveCallback(filename, state)
    _RYSY_unimplemented()
end

function state.defaultVerifyErrorCallback(filename)
    _RYSY_unimplemented()
end

-- Check that the target file can be loaded again
function state.verifyFile(filename, successCallback, errorCallback)
    _RYSY_unimplemented()
end

function state.getTemporaryFilename(filename)
    return filename .. ".saving"
end

function state.loadFile(filename, roomName)
    _RYSY_unimplemented()
end

function state.saveFile(filename, afterSaveCallback, beforeSaveCallback, addExtIfMissing, verifyMap)
    _RYSY_unimplemented()
end

function state.selectItem(item, add)
    _RYSY_unimplemented()
end

function state.getSelectedRoom()
   return _RYSY_loaded_state_getSelectedRoom()
end

function state.getSelectedFiller()
   return false
end

function state.getSelectedItem()
   return false, ""
end

function state.isItemSelected(item)
    local selectedItem, selectedItemType = state.getSelectedItem()

    if selectedItem == item then
        return true

    elseif selectedItemType == "table" then
        return not not selectedItemType[item]
    end

    return false
end

function state.openMap()
    _RYSY_unimplemented()
end

function state.newMap()
    _RYSY_unimplemented()
end

function state.saveAsCurrentMap(afterSaveCallback, beforeSaveCallback, addExtIfMissing)
    _RYSY_unimplemented()
end

function state.saveCurrentMap(afterSaveCallback, beforeSaveCallback, addExtIfMissing)
    _RYSY_unimplemented()
end

function state.getRoomByName(name)
    return _RYSY_loaded_state_getRoomByName(name)
end

function state.initFromPersistence()
    _RYSY_unimplemented()
end

function state.getLayerInformation(layer, key, default)
    _RYSY_unimplemented()
end

function state.setLayerInformation(layer, key, value)
    _RYSY_unimplemented()
end

-- todo: maybe implementable
function state.clearRoomRenderCache()
    _RYSY_unimplemented()
end

-- todo: maybe implementable
function state.getLayerShouldRender(layer)
    _RYSY_unimplemented()
end

function state.setLayerForceRender(layer, currentValue, otherValue)
    _RYSY_unimplemented()
end

function state.getLayerVisible(layer)
    _RYSY_unimplemented()
end

function state.setLayerVisible(layer, visible, silent)
    _RYSY_unimplemented()
end

function state.setShowDependendedOnMods(layer, value)
    _RYSY_unimplemented()
end

function state.getShowDependedOnMods(layer)
    _RYSY_unimplemented()
end

-- The currently selected item (room or filler)
state.selectedItem = nil
state.selectedItemType = nil

-- Rendering information about layers
state.layerInformation = {}

-- Hide content that is not in Everest.yaml
state.onlyShowDependedOnMods = {}

-- Map rendering
state.showRoomBorders = true
state.showRoomBackground = true

state = setmetatable(state, { __index = function(self, key)
    if key == "map" then
        return _RYSY_loaded_state_getMap()
    end
    return rawget(self, key)
end })

return state