local modHandler = require("mods")
local utils = require("utils")
local drawableSpriteStruct = require("structs.drawable_sprite")

local atlases = {}
local atlasNames = {}

local atlasesMt = {}

function atlasesMt.__index(self, key)
    local target = rawget(self, key) or rawget(self, atlasNames[key])

    if target then
        return target

    else
        for name, atlas in pairs(atlases) do
            if name:lower() == key:lower() then
                atlasNames[key] = name

                return atlas
            end
        end
    end
end

setmetatable(atlases, atlasesMt)

local function addAtlasMetatable(name)
    local atlas = atlases[name] or {}
    local atlasMt = {
        __index = function(self, key)
            return atlases.getResource(key, name)
        end
    }

    atlases[name] = setmetatable(atlas, atlasMt)
end

function atlases.loadCelesteAtlas(name, meta, path)
	_RYSY_unimplemented()
end

function atlases.createAtlas(name)
    _RYSY_unimplemented()
end

function atlases.loadCelesteAtlases()
	_RYSY_unimplemented()
end

-- Remove everything until after the atlas name
local function getResourceName(filename)
    local parts = filename:split("/")()
    local resourceNameWithExt = table.concat({select(5, unpack(parts))}, "/")

    return utils.stripExtension(resourceNameWithExt)
end

function atlases.loadExternalAtlas(name)
    _RYSY_unimplemented()
end

function atlases.addInternalPrefix(resource)
    return modHandler.internalModContent .. "/" .. resource
end

function atlases.getInternalResource(resource, name)
    _RYSY_unimplemented()
end

function atlases.getResource(resource, name)
	if name == "Gameplay" then
		return drawableSpriteStruct.fromTexture(resource).meta
	end

    return nil
end

function atlases.loadExternalAtlases()
    _RYSY_unimplemented()
end

return atlases