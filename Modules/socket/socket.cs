using LuaDLL = Lua511.LuaDLL;
using LuaTypes =Lua511.LuaTypes;
using lua_State = System.IntPtr;
using System.Collections.Generic; // Dictionary
using Socket = System.Net.Sockets.Socket;
using Sockets = System.Net.Sockets;

namespace Lua511.Module
{
	public class socket_core
	{

		const string registername = "luasocket";
		
		// store sockets in map from integer to Socket
		private static readonly Dictionary<int, Socket> sockets = new Dictionary<int, Socket>();
		private static int nextSockIx = 0;

		/*
		* __gc metafunction of Socket userdata proxy. 
		* Will be unneccesary with Lua 5.2, which offer finalizer on tables
		*/
		private static int l_socket_gc(lua_State L) {
			Socket s = udata2Socket(L, 1);
			if ( s == null ) return 2;  // error results from udata2Socket()
			int ix = LuaDLL.luanet_rawnetobj(L, 1);
			s.Close();
			sockets.Remove(ix);
			//System.Console.WriteLine("l_socket_gc {0}", ix);
			return 0;
		}

		/*
		* __tostring metafunction of Socket userdata proxy.
		*/
		private static int l_sockToString(lua_State L) {
			Socket s = udata2Socket(L, 1);
			if ( s == null ) return 2; // error results from udata2Socket()
			int ix = LuaDLL.luanet_rawnetobj(L, 1);
			LuaDLL.lua_pushstring(L,  string.Format( "Socket {0} {1}", ix, s.GetHashCode()) );
			return 1;
		}
		// end of socket udata gc_stuff

		// go from socket proxy userdata to Socket object
		private static Socket udata2Socket(lua_State L, int pos)
		{
			int ix = LuaDLL.luanet_checkudata(L, pos, registername);
			
			if (ix == -1)
			{
				LuaDLL.lua_pushnil(L);
				LuaDLL.lua_pushstring(L, "l_sockToString illegal userdata");
				return null; 
			}
			Socket s;
			bool found = sockets.TryGetValue(ix, out s);
			if ( ! found )
			{
				LuaDLL.lua_pushnil(L);
				LuaDLL.lua_pushstring(L, "closed");
				return null;
			}
			return s;
		}

		// socket methods 
		private static int l_sock_settimeout(lua_State L)
		{
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			double timeout = LuaDLL.luaL_optnumber(L,2, 0);
			LuaDLL.lua_pushstring (L, "timeout"); // key..
			LuaDLL.lua_pushvalue(L,2); // value: copy of timeout
			LuaDLL.lua_rawset(L, 1); // socket.timeout = timeout
			
			LuaDLL.lua_rawgeti(L,1, 1); 
			
			Socket s = udata2Socket(L, -1);
			if ( s == null ) return 2; // error results from udata2Socket()
			
			if (timeout == 0) {
				s.Blocking = false;
			} else {
				int ms = (int)(1000*timeout);
				s.ReceiveTimeout  = ms;
				s.SendTimeout = ms;
			}
			LuaDLL.lua_pushnumber(L, 1);
			return 1;
		}

		private static int return_sock_err(lua_State L, Sockets.SocketException ex)
		{
			string err = string.Format( "({0}) {1}", ex.ErrorCode, ex.Message); 
			LuaDLL.lua_pushnil(L);
			LuaDLL.lua_pushstring(L, err);
			return 2;
		}
		
		private static int l_sock_connect(lua_State L)
		{
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			LuaDLL.luaL_checktype(L, 2, LuaTypes.LUA_TSTRING);
			string address = LuaDLL.lua_tostring(L,2);
			int port =  (int)LuaDLL.luaL_checknumber(L, 3);
			
			LuaDLL.lua_rawgeti(L,1, 1); // socket[1]
			Socket s = udata2Socket(L, -1);
			if ( s == null ) return 2; // error results from udata2Socket()
			try {
				s.Connect(address, port);
			}
			catch (Sockets.SocketException ex) {
				return return_sock_err(L, ex);
			}
 
			LuaDLL.lua_pushnumber(L, 1);
			return 1;
		}

		private static int l_sock_close(lua_State L)
		{
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			LuaDLL.lua_rawgeti(L,1, 1);  // socket[1]
			int ix = LuaDLL.luanet_rawnetobj(L, -1);
			Socket s = udata2Socket(L, -1);
			if ( s == null ) return 2;   // error results from udata2Socket()
			s.Close();
			sockets.Remove(ix);
			LuaDLL.lua_pushnumber(L, 1);
			return 1;
		}

		private static int l_new_tcp(lua_State L)
		{
			Socket s = new Socket(
				Sockets.AddressFamily.InterNetwork,
				Sockets.SocketType.Stream,
				Sockets.ProtocolType.Tcp
			);

			int index = nextSockIx++;
			sockets[index] = s;

			LuaDLL.luanet_newudata(L, index);
			if (LuaDLL.luaL_newmetatable(L, registername) == 1)
			{
				table_add_func(L,  "__gc", l_socket_gc);
				table_add_func(L,  "__tostring", l_sockToString);
			}
			LuaDLL.lua_setmetatable(L, -2);
			return 1;
		}

		private const string lua_code = @"
			return function(tcp_constructor, tcp_receive)
				local error,getmetatable,pcall,setmetatable
					= error,getmetatable,pcall,setmetatable
				
				--protect/newtry adapted from https://github.com/hjelmeland/try-lua/blob/master/try.lua 
				local try_error_mt = { } -- tagging error as newtry error

				local function is_try_error(e)
					return getmetatable(e) == try_error_mt
				end

				local function newtry (finalizer)
					return function (ok, ...)
						if ok then return ok, ... end
						-- else idiomatic nil, error
						if finalizer then finalizer() end
						error( setmetatable({ (...) }, try_error_mt) ) -- raise wrapped error 
					end
				end

				local function fix_return_values(ok, ...)
					if ok then return ... end    
					if getmetatable (...) == try_error_mt then -- is_try_error(...)
						return nil, (...)[1] -- return idiomatic nil, error
					end
					error((...), 0) -- pass non-try error
				end

				local function protect(f)
					return function(...)
						return fix_return_values(pcall(f, ...))
					end
				end
				
				-- sockets 
				local BLOCK_SZ = 256
				local tcp_mt = {
					receive = function(self, pattern, prefix)
						local net_socket = self[1]
						local data, err
						prefix = prefix or ''
						if type(pattern) == 'number' then -- pattern is length
							data, err = tcp_receive(net_socket, pattern)
							if not data then return nil, err end
						elseif pattern == '*l' then  -- return single line
							local partial_line = self.rcv_buffer or ''
							local line, rest = partial_line:match'([^\n]*)\n(.*)'
							if line then
								data = line
								self.rcv_buffer = rest
							else
								while true do
									data, err =  tcp_receive(net_socket, BLOCK_SZ)
									if not data then return nil, err, (prefix or '') .. self.rcv_buffer end
									--if data == '' then data = '\n' end -- force end of file
									local line, rest = data:match'([^\n]*)\n(.*)'
									if line then
										data = partial_line..line
										self.rcv_buffer = rest
										data = data:gsub('[\r\n]', '')
										break
									end
									partial_line = partial_line .. data
								end 
							end
						elseif pattern == '*a' then  -- read all
							local partial_data = self.rcv_buffer or ''
							while true do
								data, err =  tcp_receive(net_socket, BLOCK_SZ)
								if err == 'closed' then
									data = partial_data
									break
								end
								if not data then return nil, err end
								partial_data = partial_data .. data
							end 
						end
						if data and prefix then
							data = prefix..data
						end 
						return data
					end,
				}
				tcp_mt.__index = tcp_mt
				
				local tcp = function()
					return setmetatable({tcp_constructor()}, tcp_mt)
				end
				
				return {
					newtry = newtry, 
					protect = protect,
					is_try_error = is_try_error,
					tcp = tcp,
				}, tcp_mt
			end
		";

		// add a function as name to table at top of stack
		private static void table_add_func (lua_State L, string name, LuaCSFunction function) {
			LuaDLL.lua_pushstring (L, name); // key..
			LuaDLL.lua_pushstdcallcfunction(L, function); // value..
			LuaDLL.lua_rawset(L, -3); // top[key] = function
		}

		public static int load(lua_State L)
		{
			LuaDLL.luaL_dostring(L, lua_code); // return function (function(tcp_constructor,..)
			LuaDLL.lua_pushstdcallcfunction(L, l_new_tcp); // set parameter..
			LuaDLL.lua_call(L, 1, 2); //call the returned function, returning module table + metatable
			
			// set Socket methods
			table_add_func(L, "settimeout", l_sock_settimeout);
			table_add_func(L, "connect",    l_sock_connect);
			table_add_func(L, "close",      l_sock_close);
			
			LuaDLL.lua_pop(L, 1); // pop the metatable
			return 1; // return the module table
		}
	}
}
