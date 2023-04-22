local utils = require("utils")

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
-- Defaults to current mod directory
function modHandler.requireFromPlugin(lib, modName)
	modName = modName or _RYSY_CURRENT_MOD

	if not loadedFromPlugins[modName] then
		loadedFromPlugins[modName] = {}
	end

    if not loadedFromPlugins[modName][lib] then
		local required = _RYSY_INTERNAL_requireFromPlugin(lib, modName)
		if not required then
			error(string.format("library %s [%s] not found!", lib, modName))
		end

		loadedFromPlugins[modName][lib] = loadstring(required)()
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

-- Defaults to current mod
function modHandler.getModSettings(modName)
    --_RYSY_unimplemented()

	--print("WARN: getting mod settings doesn't do anything rn!'")
	return {}
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