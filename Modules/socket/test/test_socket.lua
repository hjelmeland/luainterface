-- semi automatic test, need a basic webserver on localhost:8080

local arg,assert,collectgarbage,luanet,print,require,tonumber,type
    = arg,assert,collectgarbage,luanet,print,require,tonumber,type


local address = arg[1] or 'localhost'
local port = arg[2] and tonumber(arg[2]) or 8080

local S=require'socket'
assert(type(S.try) == 'function')
local s=S.tcp()
if luanet then 
	assert(type(s) == 'table')
	assert(type(s[1]) == 'userdata')
end
assert(s:settimeout (344 ) == 1)
if luanet then
	assert(s.timeout == 344)
end

print(s:connect(address, port));
assert(s:close() == 1, 'close');
do
	local res, err = s:connect(address, port);
	assert(res == nil)
	assert(err == 'closed')
end
s=nil
collectgarbage()


