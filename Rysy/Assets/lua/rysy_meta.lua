RYSY = {} -- Set up a global RYSY variable, so that plugins know they're running in Rysy if needed.
_RYSY_entities = {}

_MAP_VIEWER = {
	name = "rysy",
	version = "0.0.0" -- todo: provide this automatically
}

_RYSY_unimplemented = function()
	local info = debug.getinfo(2)
	local caller = info.name
	local src = info.short_src

	local traceback = debug.traceback(string.format("The method '%s->%s' is not implemented in Rysy", src, caller), 3)

	error(traceback)
end