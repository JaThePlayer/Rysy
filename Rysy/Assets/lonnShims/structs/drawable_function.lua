local drawableFunction = {}

function drawableFunction.fromFunction(func, ...)
    local drawable = {
        _type = "drawableFunction"
    }

    drawable.func = func
    drawable.args = {...}
    drawable.draw = function(self)
    end

    return drawable
end

return drawableFunction