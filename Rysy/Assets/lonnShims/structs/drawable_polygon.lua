--[[
Extension - not preset in lonn itself
]]
local utils = require("utils")

local polygon = {}

local function validateMode(mode)
  if mode ~= "line" and mode ~= "fill" and mode ~= "bordered" then
    error(string.format("Polygon mode '%s' is invalid in Rysy!", mode))
  end
end

function polygon.fromEntityAndNodes(mode, entity, color, secondaryColor)
  mode = mode or "fill"
  validateMode(mode)
  
  color = utils.getColor(color)
  secondaryColor = secondaryColor and utils.getColor(secondaryColor) or color

  return {
    _type = "drawablePolygon",
    mode = mode,
    color = color,
    secondaryColor = secondaryColor,
    __RYSY_entity = entity
  }
end

function polygon.fromPoints(mode, points, color, secondaryColor)
  mode = mode or "fill"
  validateMode(mode)
  
  color = utils.getColor(color)
  secondaryColor = secondaryColor and utils.getColor(secondaryColor) or color

  return {
    _type = "drawablePolygon",
    mode = mode,
    color = color,
    secondaryColor = secondaryColor,
    points = points,
  }
end

return polygon
