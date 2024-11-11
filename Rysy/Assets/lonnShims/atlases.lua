local modHandler = require("mods")
local utils = require("utils")

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
    local atlas = atlases[name] or { _metaCache = {} }
    local atlasMt = {
        __index = function(self, key)
            return atlases.getResource(key, name)
        end
    }

    atlases[name] = setmetatable(atlas, atlasMt)
end

addAtlasMetatable("Gameplay")
addAtlasMetatable("Gui")

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


--[[
RYSY INTERNALS
]]
local __metaMt = {}

-- Add support for Rysy lazyloading - only load in the size of the texture if actually needed.
function __metaMt.__index(self, key)
    local loaded = rawget(self, "_RYSY_loaded")
    if not loaded then
        local texture = rawget(self, "_RYSY_INTERNAL_texture")
        local atlasName = rawget(self, "_RYSY_INTERNAL_atlasName")
        local x, y, width, height, offsetX, offsetY = _RYSY_DRAWABLE_getTextureSize(texture, atlasName)
        
        rawset(self, "x", x)
        rawset(self, "y", y)
        rawset(self, "width", width)
        rawset(self, "height", height)
		rawset(self, "offsetX", offsetX)
		rawset(self, "offsetY", offsetY)
        rawset(self, "_RYSY_loaded", true)

		rawset(self, "realWidth", width)
		rawset(self, "realHeight", height)
		rawset(self, "filename", string.format("@ModsCommon@/Graphics/Atlases/%s/%s.png", atlasName, texture))
    end

    local raw = rawget(self, key)
    if raw then 
        return raw 
    end

    return nil
end

local function __getMeta(atlasName, texture)
    texture = _RYSY_DRAWABLE_fixPath(texture)

    local atlas = atlases[atlasName]
    local cache = rawget(atlas, "_metaCache")
    
    local cached = cache[texture]
    if cached then
        return cached
    end

    if not _RYSY_DRAWABLE_exists(texture, atlasName) then
        return nil
    end
    
	local m = {
        _RYSY_INTERNAL_atlasName = atlasName,
        _RYSY_INTERNAL_texture = texture
    }
    setmetatable(m, __metaMt)
    cache[texture] = m

    return m
end
-- END RYSY INTERNALS

function atlases.getResource(resource, name)
	return __getMeta(name, resource)
end

function atlases.loadExternalAtlases()
    _RYSY_unimplemented()
end

return atlases