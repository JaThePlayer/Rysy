local rectangles = {}

function rectangles.create(x, y, width, height)
    local rectangle = {
        _type = "rectangle",
        x = width < 0 and x + width or x,
        y = height < 0 and y + height or y,
        width = math.abs(width),
        height = math.abs(height)
    }

    return rectangle
end

return rectangles