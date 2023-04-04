local logging = {}

logging.logLevels = {
    DEBUG = 0,
    INFO = 1,
    WARNING = 2,
    ERROR = 3
}

function logging.update(dt)
	_RYSY_unimplemented()
end

function logging.bufferAddMessage(filename, message)
	_RYSY_unimplemented()
end

function logging.bufferWrite(filename)
    _RYSY_unimplemented()
end

function logging.log(status, message, filename)
    _RYSY_log(status, message)
end

function logging.debug(message, filename)
    return logging.log("DEBUG", message, filename)
end

function logging.info(message, filename)
    return logging.log("INFO", message, filename)
end

function logging.warning(message, filename)
    return logging.log("WARNING", message, filename)
end

function logging.error(message, filename)
    return logging.log("ERROR", message, filename)
end

return logging