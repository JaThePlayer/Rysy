local languageRegistry = {}

local lang_mt = nil
lang_mt = {
    __tostring = function(l)
        return _RYSY_lang_get(l._key)
    end,
    __index = function(l, i)
        local key = rawget(l, "_key")
        return setmetatable({ _key = key and (key.."."..i) or i}, lang_mt)
    end
}

languageRegistry.language = setmetatable({}, lang_mt)
languageRegistry.currentLanguageName = "en_gb" -- todo: call into rysy and use metatables to keep this up-to-date
languageRegistry.languages = { ["en_gb"] = languageRegistry.language }

function languageRegistry.setLanguage(name)
    _RYSY_unimplemented()
end

function languageRegistry.getLanguage()
    return languageRegistry.language
end

function languageRegistry.loadLanguageFile(filename)
    _RYSY_unimplemented()
end

function languageRegistry.unloadFiles()
    _RYSY_unimplemented()
end

function languageRegistry.loadInternalFiles()
    _RYSY_unimplemented()
end

function languageRegistry.loadExternalFiles()
    _RYSY_unimplemented()
end

return languageRegistry