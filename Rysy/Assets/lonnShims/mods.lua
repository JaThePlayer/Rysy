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

local function _handleRequire(lib, modName)
	local required = _RYSY_INTERNAL_requireFromPlugin(lib, modName)
	if not required then
		logging.error(string.format("library %s [%s] not found!", lib, modName))
		loadedFromPlugins[modName][lib] = "__nil"
		return nil
	end

    if type(required) == "string" then
        required = loadstring(required)()
    end
    
    return required
end

-- global func called from C# when a library file gets updated
function _RYSY_clear_requireFromPlugin_cache(lib, modName)
	if not loadedFromPlugins[modName] then
		return
	end
	
	-- Update the hot reload metatable to point to the new version of the library.
	local existing = loadedFromPlugins[modName][lib]
	if existing and rawget(existing, "___ishotreloadable") then
	    local newVer = _handleRequire(lib, modName)
	    if newVer and type(newVer) == "table" then
	        rawset(existing, "___tbl", newVer)
	        return
	    end
	end
	
	-- Couldn't update the metatable, just clear the cache for when users of the library get reloaded later.
	loadedFromPlugins[modName][lib] = nil
end

-- Defaults to current mod directory
function modHandler.requireFromPlugin(lib, modName)
	modName = modName or _RYSY_CURRENT_MOD

	if NEO_LUA then
		return _RYSY_INTERNAL_requireFromPlugin(lib, modName)
	end

	if not loadedFromPlugins[modName] then
		loadedFromPlugins[modName] = {}
	end

    if not loadedFromPlugins[modName][lib] then
        local required = _handleRequire(lib, modName)
        if not required then
            return nil
        end

        -- Wrap returned tables with a metatable.
        -- This way, when we hot reload the library, everything using that lib gets their reference automatically updated.
        local wrapper = required
        if type(required) == "table" and not getmetatable(required) then
            wrapper = setmetatable({___tbl = required, ___ishotreloadable = true }, {
                __index = function(self, key) return rawget(self, "___tbl")[key] end,
                __newindex = function(self, key, value) rawget(self, "___tbl")[key] = value end,
            })
        end

		loadedFromPlugins[modName][lib] = wrapper
	end

	if loadedFromPlugins[modName][lib] == "__nil" then
		return nil
	end

	return loadedFromPlugins[modName][lib]
	
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