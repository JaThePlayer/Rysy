local roomStruct = {}

roomStruct.recommendedMinimumWidth = 320
roomStruct.recommendedMinimumHeight = 184

function roomStruct.decode(data)
	_RYSY_unimplemented()
end

-- Resize a room from a given side
-- Also cuts off background tiles
-- Amount in tiles
function roomStruct.directionalResize(room, side, amount)
	_RYSY_unimplemented()
end

-- Moves amount * step in the direction
-- Step defaults to 8, being a tile
function roomStruct.directionalMove(room, side, amount, step)
    _RYSY_unimplemented()
end

function roomStruct.getPosition(room)
    return room.x, room.y
end

function roomStruct.getSize(room)
    return room.width, room.height
end

function roomStruct.encode(room)
    _RYSY_unimplemented()
end

return roomStruct