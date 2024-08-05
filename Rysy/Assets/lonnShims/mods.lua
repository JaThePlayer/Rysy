local utils = require("utils")
local logging = require("logging")

local modHandler = {}

local everestBuildNumberMatch = "EverestBuild(%d*)"

modHandler.internalModContent = "@Internal@"
modHandler.commonModContent = "@ModsCommon@"
modHandler.everestYamlFilenames = {
    "everest.yaml",
    "everest.yml"
}
modHandler.specificModContentSymbol = "$"
modHandler.specificModContent = "$%s$"
modHandler.pluginFolderNames = {
}

modHandler.loadedMods = {}
modHandler.loadedNameLookup = {}
modHandler.knownPluginRequires = {}
modHandler.modMetadata = {}
modHandler.modSettings = {}
modHandler.modPersistence = {}

modHandler.modNamesFormat = "[%s]"
modHandler.modNamesSeparator = " + "

modHandler.persistenceBufferTime = 300

-- Finds files in all folders that are recognized as plugin folders
function modHandler.findPluginFiletype(startFolder, filetype)
	_RYSY_unimplemented()
end

-- Fine tuned search for exactly one mod folder
function modHandler.findModFolderFiletype(modFolderName, filenames, startFolder, fileType)
	_RYSY_unimplemented()
end

-- Finds files relative to the root of every loaded mod
-- This is more performant than using the common mount point when looking for files recursively
function modHandler.findModFiletype(startFolder, filetype, folderNames)
	_RYSY_unimplemented()
end

function modHandler.findPlugins(pluginType)
    _RYSY_unimplemented()
end

function modHandler.findLanguageFiles(startPath)
    _RYSY_unimplemented()
end

function modHandler.mountable(path)
    _RYSY_unimplemented()
end

function modHandler.getFilenameModName(filename)
    _RYSY_unimplemented()
end

function modHandler.getFilenameModPath(filename)
    _RYSY_unimplemented()
end

-- Assumes entity names are "modName/entityName"
function modHandler.getEntityModPrefix(name)
    return name:match("^(.-)/")
end

function modHandler.findPluginLoennFolder(mountPoint)
    _RYSY_unimplemented()
end

function modHandler.getEverestBuildNumber()
    _RYSY_unimplemented()
end

function modHandler.getEverestVersion()
    _RYSY_unimplemented()
end

function modHandler.findEverestYaml(mountPoint)
    _RYSY_unimplemented()
end

-- Find existing or fall back to first search filename
function modHandler.findEverestYamlOrDefault(mountPoint)
    _RYSY_unimplemented()
end

function modHandler.updateModMetadataCache(modMetadata, folderName)
    _RYSY_unimplemented()
end

function modHandler.readModMetadata(path, mountPoint, folderName, updateCache)
    _RYSY_unimplemented()
end

function modHandler.findLoadedMod(name)
    local mod = _RYSY_MODS_find(name)

	if not mod then 
		return nil 
	end

	local realMod = {
		Name = mod.Name,
		Version = mod.Version
	}

	return realMod, realMod
end

function modHandler.hasLoadedMod(name)
    return _RYSY_MODS_find(name) ~= nil
end

-- Only works on files loaded with requireFromPlugin
function modHandler.unrequireKnownPluginRequires()
    _RYSY_unimplemented()
end

local loadedFromPlugins = {}

local function revertHotReloadData(prevHotReloadData)
	_RYSY_OnHotReload_name = prevHotReloadData[1]
	_RYSY_OnHotReload_type = prevHotReloadData[2]
	_RYSY_CURRENT_MOD = prevHotReloadData[3]
end

local function _handleRequire(lib, modName, registerWatcher)
    -- update globals used to keep track of hot reload data, to handle nested libraries
    local prevHotReloadData = { _RYSY_OnHotReload_name, _RYSY_OnHotReload_type, _RYSY_CURRENT_MOD }
    _RYSY_OnHotReload_name = lib
    _RYSY_OnHotReload_type = "lib"
    _RYSY_CURRENT_MOD = modName

	local required = _RYSY_INTERNAL_requireFromPlugin(lib, modName, registerWatcher)
	
	if not required then
	    revertHotReloadData(prevHotReloadData)
		logging.error(string.format("library %s [%s] not found!", lib, modName))
		loadedFromPlugins[modName][lib] = "__nil"
		return nil
	end

    if type(required) == "string" then
        required = loadstring(required)()
    end
    
    revertHotReloadData(prevHotReloadData)
    
    return required
end

-- global func called from C# when a library file gets updated
function _RYSY_clear_requireFromPlugin_cache(lib, modName)
	if not loadedFromPlugins[modName] then
		return
	end
	
	local existing = loadedFromPlugins[modName][lib]
	if existing == "__nil" then
	    loadedFromPlugins[modName][lib] = nil
	    return
	end
	
	-- Update the table to point to the new version of the library.
    if existing and type(existing.ret) == "table" then
        local newVer = _handleRequire(lib, modName, false)
        
        local toHotReload = existing.toHotReload
        existing.toHotReload = {}
        existing.ret = newVer
        -- Hot reload anything depending on this lib
        for k, v in pairs(toHotReload) do
            print(string.format("Hot reloading %s as it depends on %s", v.lib, lib))
            if v.type == "entity" then
                _RYSY_INTERNAL_hotReloadPlugin(v.lib, v.modName, "entity")
            elseif v.type == "trigger" then
                _RYSY_INTERNAL_hotReloadPlugin(v.lib, v.modName, "trigger")
            elseif v.type == "style" then
                _RYSY_INTERNAL_hotReloadPlugin(v.lib, v.modName, "style")
            elseif v.type == "lib" then
                _RYSY_clear_requireFromPlugin_cache(v.lib, v.modName)
            end
        end
        
        return
    end
	
	-- Couldn't update the metatable, just clear the cache for when users of the library get reloaded later.
	loadedFromPlugins[modName][lib] = nil
end

-- Defaults to current mod directory
function modHandler.requireFromPlugin(lib, modName)
	modName = modName or _RYSY_CURRENT_MOD

	if not loadedFromPlugins[modName] then
		loadedFromPlugins[modName] = {}
	end

    if not loadedFromPlugins[modName][lib] then
        local required = _handleRequire(lib, modName, true)
        if not required then
            loadedFromPlugins[modName][lib] = "__nil"
            return nil
        end

		loadedFromPlugins[modName][lib] = {
		    ret = required,
		    toHotReload = { }
		}
	end

	if loadedFromPlugins[modName][lib] == "__nil" then
		return nil
	end

    local toHotReload = loadedFromPlugins[modName][lib].toHotReload
    if _RYSY_OnHotReload_name then
        -- print(string.format("%s lib required by %s", lib, _RYSY_OnHotReload_name))
        toHotReload[modName .. "|" .. _RYSY_OnHotReload_name] = { 
            lib = _RYSY_OnHotReload_name, 
            modName = modName,
            type = _RYSY_OnHotReload_type,
        }
    end

	return loadedFromPlugins[modName][lib].ret
	
end

-- Defaults to current mod directory
function modHandler.readFromPlugin(filename, modName)
    _RYSY_unimplemented()
end

local function createModSettingDirectory(modName)
    _RYSY_unimplemented()
end

-- Work our way up the stack to find the first plugin related source
-- This makes sure the function can be used from anywhere
function modHandler.getCurrentModName(maxDepth)
    _RYSY_unimplemented()
end


local _modSettingMt = {}

function _modSettingMt.__index(self, key)
	local modName = rawget(self, "__mod")

	--print("get", modName, key, _RYSY_INTERNAL_getModSetting(modName, key))
	return _RYSY_INTERNAL_getModSetting(modName, key)
end

function _modSettingMt.__newindex(self, key, value)
	local modName = rawget(self, "__mod")

	--print("set", modName, key, value)
	local err = _RYSY_INTERNAL_setModSetting(modName, key, value)
	if err then
		error(err)
	end
end


-- Defaults to current mod
function modHandler.getModSettings(modName)
	modName = modName or _RYSY_CURRENT_MOD
    --_RYSY_unimplemented()

	--print("WARN: getting mod settings doesn't do anything rn!'")
	local t = {
		__mod = modName
	}
	t = setmetatable(t, _modSettingMt)

	return t
end

-- Defaults to current mod
function modHandler.getModPersistence(modName)
    _RYSY_unimplemented()
end

function modHandler.writeModPersistences()
    _RYSY_unimplemented()
end

function modHandler.mountMod(path, force)
    _RYSY_unimplemented()
end

function modHandler.mountMods(directory, force)
    _RYSY_unimplemented()
end

local function getModMetadataByKeyCached(value, key, hasTriedMount)
    _RYSY_unimplemented()
end

local function getModMetadataFromRealFilename(filename)
    _RYSY_unimplemented()
end

local function getModMetadataFromSpecific(filename)
    _RYSY_unimplemented()
end

function modHandler.getModMetadataFromPath(path)
    _RYSY_unimplemented()
end

function modHandler.getModNamesFromMetadata(metadata)
    _RYSY_unimplemented()
end

function modHandler.getDependencyModNames(metadata, addSelf)
    _RYSY_unimplemented()
end

function modHandler.getAvailableModNames()
    _RYSY_unimplemented()
end

local entityPrefixShownWarningFor = {}

local function entityPrefixDeprecationWarning(language, modNames, modPrefix)
    _RYSY_unimplemented()
end

function modHandler.formatAssociatedMods(language, modNames, modPrefix)
    _RYSY_unimplemented()
end

return modHandler