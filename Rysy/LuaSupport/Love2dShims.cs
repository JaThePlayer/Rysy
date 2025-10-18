using KeraLua;

namespace Rysy.LuaSupport;

internal static class Love2dShims {
    public static void Init(Lua lua) {
        lua.PCallStringThrowIfError("""
            local _mt_font = {
                __index = {
                    -- TODO: newlines
                    getWidth = function(self, text) return #text * self._fontSize end,
                    getHeight = function(self, text) return self._fontSize end,
                }
            }
            
            love = {
                graphics = {
                    newFont = function(fontSize) return setmetatable({ 
                        _type = "_rysy_love2d_font",
                        _fontSize = fontSize
                    }, _mt_font) end
                }
            }
            
            local font = love.graphics.newFont(8)
            function love.graphics.getFont() return font end
            
            local _mt_text = {
                __index = {
                    set = function(self, value)
                        self._text = value
                    end,
                    getWidth = function(self)
                        return self._font:getWidth(self._text)
                    end,
                    getHeight = function(self)
                        return self._font:getHeight(self._text)
                    end
                }
            }
            
            function love.graphics.newText(font, txt) return setmetatable({
                _type = "_rysy_love2d_text",
                _font = font,
                _text = txt
            }, _mt_text) end
            
            function love.getVersion() return 12 end -- TODO: Which version does lonn use
            """u8, "init_love2d");

        InitImage(lua);
    }

    private static void InitImage(Lua lua) {
        lua.PCallStringThrowIfError("""
            local _mt_image = {
                __index = {
                    setFilter = function(self, min, mag, anisotropy)
                        self._filterMin = min
                        self._filterMag = mag
                        self._filterAnisotropy = anisotropy or 1
                    end,
                    setMipmapFilter = function(self, filtermode, sharpness)
                        self._filterSharpness = sharpness
                        self._filterMode = filtermode
                    end,
                    getWidth = function(self)
                        return 32 -- todo
                    end,
                    getHeight = function(self)
                        return 32 -- todo
                    end,
                    getDimensions = function(self)
                        return self:getWidth(), self:getHeight()
                    end,
                }
            }
            
            function love.graphics.newImage(filename, settings)
                return setmetatable({
                    _type = "_rysy_love2d_image",
                    _filename = filename,
                    _mipmaps = settings.mipmaps or false,
                    _linear = settings.linear or false,
                }, _mt_image)
            end
            """u8, "init_love2d_image");
    }
}
