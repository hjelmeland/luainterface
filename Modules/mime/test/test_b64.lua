local mime= require'mime'

local function test_b64()
	do
		local r1,r2 = mime.b64('diego:password') 
		assert(r1 == 'ZGllZ286cGFzc3dvcmQ=')
		assert(r2 == nil)
	end
	
	do
		local r1,r2 = mime.b64('diego:passwo') 
		assert(r1 == 'ZGllZ286cGFzc3dv')
		assert(r2 == nil)
	end

	do
		local r1,r2 = mime.b64('diego:password','') 
		--print(r1,r2)
		assert(r1 == 'ZGllZ286cGFzc3dv')
		assert(r2 == 'rd')
	end

	do
		local r1,r2 = mime.b64('diego:passwo','') 
		--print(r1,r2)
		assert(r1 == 'ZGllZ286cGFzc3dv')
		assert(r2 == '')
	end

	do
		local r1,r2 = mime.b64('diego:passw','ord') 
		--print(r1,r2)
		assert(r1 == 'ZGllZ286cGFzc3dv')
		assert(r2 == 'rd')
	end
	
end

local function test_unb64()

	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcmQ=') 
		assert(r1 == 'diego:password')
		assert(r2 == nil)
	end
	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcm') 
		--print(r1,r2)
		assert(r1 == 'diego:passwo')
		assert(r2 == nil)
	end
	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcmQ=', '') 
		--print(r1,r2)
		assert(r1 == 'diego:password')
		assert(r2 == '')
	end
	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcm', '') 
		--print(r1,r2)
		assert(r1 == 'diego:passwo')
		assert(r2 == 'cm')
	end
	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcm', 'Q=') 
		--print(r1,r2)
		assert(r1 == 'diego:password')
		assert(r2 == '')
	end
	do
		local r1,r2 = mime.unb64('ZGllZ286cGFzc3dvcm', 'Q') 
		--print(r1,r2)
		assert(r1 == 'diego:passwo')
		assert(r2 == 'cmQ')
	end
end

test_b64()
test_unb64()

