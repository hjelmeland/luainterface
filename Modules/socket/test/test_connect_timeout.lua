
local arg,assert,collectgarbage,debug,luanet,print,require,type,xpcall
    = arg,assert,collectgarbage,debug,luanet,print,require,type,xpcall




local address = 'bt.no'
local port =  arg[1] or 2431

local S=require'socket'
assert(type(S.try) == 'function')

local function test ()

	local s1= assert( S.tcp() )
	do
		assert( s1:settimeout (1.3 ) == 1)
		if luanet then
			assert(s1.timeout == 1.3 )
		end

		local ok, err = s1:connect(address, port);
		print(ok, err)
		
		if ok then
			local cmd = 'GET / HTTP 1.1\r\n\r\n'

			assert( s1:send(cmd) == #cmd )
		

			local res, err, rest = s1:receive('*l')
			--print(res, err, rest)
			print(#res, res)
		end
		
		assert(s1:close() == 1, 'close');
	end

end

local start = S.gettime()

local res,err = xpcall(test, debug.traceback)
if not res then print( err) end

collectgarbage()
print('Time',   S.gettime() - start)
print('Lua usage kB',  collectgarbage'count')
