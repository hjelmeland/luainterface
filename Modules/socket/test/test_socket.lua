-- semi automatic test, need a basic webserver on localhost:8080

local arg,assert,collectgarbage,luanet,print,require,type
    = arg,assert,collectgarbage,luanet,print,require,type


local address = 'localhost'
local port = 8080
local verbose = arg[1]

local S=require'socket'
assert(type(S.try) == 'function')

local function test ()

	local s1= assert( S.tcp() )
	local s2= assert( S.tcp() )
	local s3= assert( S.tcp() )
	local s4= assert( S.tcp() )
	do
		if luanet then 
			assert(type(s2) == 'table')
			assert(type(s2[1]) == 'userdata')
		end
		assert(s2:settimeout (3.2 ) == 1)
		if luanet then
			assert(s2.timeout == 3.2)
		end
		
		assert(s2:settimeout () == 1)
		if luanet then
			assert(s2.timeout == nil)
		end

		assert(s2:connect(address, port) == 1);
		assert(s2:close() == 1, 'close');
		
		do
			local res, err = s2:connect(address, port);
			assert(res == nil)
			assert(err == 'closed')
		end
		s2=nil
		collectgarbage()
	end

	do  -- test read line
		assert( s1:settimeout (1 ) == 1)
		assert( s1:connect(address, port) == 1);
		local cmd = 'GET / HTTP 1.0\r\n\r\n'
		assert( s1:send(cmd) == #cmd )
		
		repeat 
			local res, err, rest = s1:receive('*l', 'Got_line ')
			if res then 
				if verbose then
					print(#res, res)
				end
				assert(res:find'Got_line' == 1)
				assert(err== nil)
				assert(rest== nil)
			else
				assert(err=='closed')
			end
		until not res
	--]]
		assert( s1:close() == 1, 'close');
	end


	do -- test read block and lines
		assert( s3:settimeout (1 ) == 1)
		assert( s3:connect(address, port) == 1);
		local cmd = 'GET / HTTP 1.0\r\n\r\n'
		assert( s3:send(cmd) == #cmd )

		do 
			local res, err, rest = s3:receive(10, 'Got:')
			--print(res, err, rest)
			assert(res == 'Got:HTTP/1')
			assert(err == nil)
			assert(rest == nil)
		end
		
		do 
			local res, err, rest = s3:receive() -- rest of line
			--print(res, err, rest)
			assert(res:find'200/OK')
			assert(err == nil)
			assert(rest == nil)
			if verbose then
				print()
				print(#res, res)
			end
		end

		repeat 
			local res, err, rest = s3:receive('*l', 'Got_line ')
			if res then 
				if verbose then
					print(#res, res)
				end
				assert(res:find'Got_line' == 1)
				assert(err== nil)
				assert(rest== nil)
				--print(#res, res) 
			else
				assert(err=='closed')
			end
		until not res
	--]]
		assert( s3:close() == 1, 'close');
	end

	do -- test read line and then read all
		assert( s4:settimeout (1 ) == 1)
		assert( s4:connect(address, port) == 1);
		local cmd = 'GET / HTTP 1.0\r\n\r\n'
		assert( s4:send(cmd) == #cmd )
		
		do 
			local res, err, rest = s4:receive('*l')
			--print(res, err, rest)
			if verbose then
				print()
				print(#res, res)
			end
			assert(res:find'HTTP/1' == 1)
			assert(err == nil)
			assert(rest == nil)
		end
		do 
			local res, err, rest = s4:receive('*a')
			--print(res, err, rest)
			assert(res)
			if verbose then
				print(#res, res)
			end
			assert(err == nil)
			assert(rest == nil)
		end
	end
	
	do -- test socket.skip
		do
			local a, b, c = S.skip (1, 1, 2, 3, 4)
			assert(a==2)
			assert(b==3)
			assert(c==4)
		end
		do
			local a, b, c = S.skip (2, 1, 2, 3, 4)
			assert(a==3)
			assert(b==4)
			assert(c==nil)
		end
	end
end

local start = S.gettime()
local res,err = xpcall(test, debug.traceback)
if not res then print( err) end

collectgarbage()
print('Time',   S.gettime() - start)
print('Lua usage kB',  collectgarbage'count')
