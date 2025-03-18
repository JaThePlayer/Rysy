-- Partial reimplementation of LuaJIT's bit library
local bit = {}

bit.lshift = _RYSY_bit_lshift
bit.bor = _RYSY_bit_bor
bit.band = _RYSY_bit_band
bit.bnot = _RYSY_bit_bnot

return bit