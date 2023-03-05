local utils = require("utils")

local drawing = {}

function drawing.drawSprite(spriteMeta, x, y, r, sx, sy, ox, oy)
    _RYSY_unimplemented()
end

function drawing.getCurvePoint(start, stop, control, percent)
    local startMul = (1 - percent)^2
    local controlMul = 2 * (1 - percent) * percent
    local stopMul = percent^2

    local x = start[1] * startMul + control[1] * controlMul + stop[1] * stopMul
    local y = start[2] * startMul + control[2] * controlMul + stop[2] * stopMul

    return x, y
end

function drawing.getSimpleCurve(start, stop, control, resolution)
    control = control or {(start[1] + stop[1]) / 2, (start[2] + stop[2]) / 2}
    resolution = resolution or 16

    local res = {}

    for i = 0, resolution do
        local x, y = drawing.getCurvePoint(start, stop, control, i / resolution)

        table.insert(res, x)
        table.insert(res, y)
    end

    return res
end

function drawing.getRelativeQuad(spriteMeta, x, y, width, height, hideOverflow, realSize)
    _RYSY_unimplemented()
end

function drawing.printCenteredText(text, x, y, width, height, font, fontSize, trim)
    _RYSY_unimplemented()
end

function drawing.addCenteredText(batch, text, x, y, width, height, font, fontSize, trim)
    _RYSY_unimplemented()
end

function drawing.getTrianglePoints(x, y, theta, height)
    theta = theta - math.pi / 2

    local px1 = x + height * math.cos(theta + math.pi / 4)
    local py1 = y + height * math.sin(theta + math.pi / 4)

    local px2 = x + height * math.cos(theta - math.pi / 4)
    local py2 = y + height * math.sin(theta - math.pi / 4)

    return x, y, px1, py1, px2, py2
end

function drawing.triangle(mode, x, y, theta, height)
    _RYSY_unimplemented()
end

function drawing.callKeepOriginalColor(func)
    _RYSY_unimplemented()
end

function drawing.getDashedLineSegments(x1, y1, x2, y2, dash, space)
    dash = dash or 6
    space = space or 4

    local length = math.sqrt((x1 - x2)^2 + (y1 - y2)^2)
    local progress = 0
    local segments = {}

    while progress < length do
        local startPercent = progress / length
        local stopPercent = math.min(length, progress + dash) / length
        local startX = x1 + (x2 - x1) * startPercent
        local startY = y1 + (y2 - y1) * startPercent
        local stopX = x1 + (x2 - x1) * stopPercent
        local stopY = y1 + (y2 - y1) * stopPercent

        table.insert(segments, {startX, startY, stopX, stopY})

        progress += dash + space
    end

    return segments
end

function drawing.drawDashedLine(x1, y1, x2, y2, dash, space)
    _RYSY_unimplemented()
end

return drawing