﻿using LuaDLL = Lua511.LuaDLL;
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
		private static Socket udata2Socket(lua_State L, int pos){
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
		
		// settimeout method
		private static int l_sock_settimeout(lua_State L) {
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			double timeout = LuaDLL.luaL_checknumber(L,2);
			LuaDLL.lua_pushstring (L, "timeout"); // key..
			LuaDLL.lua_pushvalue(L,2); // value: copy of timeout
			LuaDLL.lua_rawset(L, 1); // socket.timeout = timeout
			
			LuaDLL.lua_rawgeti(L,1, 1); 
			
			Socket s = udata2Socket(L, -1);
			if ( s == null ) return 2; // error results from udata2Socket()
			
			if (timeout == 0) {
				// s.Blocking = false; // not supported for now
			} else {
				int ms = (int)(1000*timeout);
				s.ReceiveTimeout  = ms;
				s.SendTimeout = ms;
			}
			LuaDLL.lua_pushnumber(L, 1);
			return 1;
		}

		private static int return_sock_err(lua_State L, Sockets.SocketException ex) {
			string err = string.Format( "({0}) {1}", ex.ErrorCode, ex.Message); 
			LuaDLL.lua_pushnil(L);
			LuaDLL.lua_pushstring(L, err);
			//System.Console.WriteLine("got error: {0}",err);
			return 2;
		}
		
		// connect method
		private static int l_sock_connect(lua_State L) {
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			LuaDLL.luaL_checktype(L, 2, LuaTypes.LUA_TSTRING);
			string address = LuaDLL.lua_tostring(L,2);
			int port =  (int)LuaDLL.luaL_checknumber(L, 3);
			
			LuaDLL.lua_rawgeti(L,1, 1); // socket[1]
			// 2do: handle timeout and nonblocking sockets
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
		
		// send method
		private static int l_sock_send(lua_State L) {
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			LuaDLL.luaL_checktype(L, 2, LuaTypes.LUA_TSTRING);
			byte[] data = LuaDLL.lua_tobytes(L,2);
			// 2do: handle optional substring indexes
			LuaDLL.lua_rawgeti(L,1, 1); // socket[1]
			Socket s = udata2Socket(L, -1);
			if ( s == null ) return 2; // error results from udata2Socket()
			int n;
			try {
				n = s.Send(data);
			}
			catch (Sockets.SocketException ex) {
				return return_sock_err(L, ex);
			}
 
			LuaDLL.lua_pushnumber(L, n);
			return 1;
		}

		// close method
		private static int l_sock_close(lua_State L) {
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

		// Helper for receive method: sock_receive(sock-udata, nbytes) return string 
		private static int l_sock_receive_sz(lua_State L) {
			/*
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TTABLE);
			LuaDLL.lua_rawgeti(L,1, 1); // socket[1]
			*/
			Socket s = udata2Socket(L, 1);
			if ( s == null ) return 2; // error results from udata2Socket()
			int nbytes = (int)LuaDLL.luaL_checknumber(L,2);
			byte[] buff = new byte[nbytes]; 
			int n;
			try {
				n = s.Receive(buff);
			}
			catch (Sockets.SocketException ex) {
				return return_sock_err(L, ex);
			}
			
			if (n == 0) {  // EOF
				LuaDLL.lua_pushnil(L);
				LuaDLL.lua_pushstring(L, "closed");
				return 2;
			}
			
			LuaDLL.lua_pushbytes(L, buff, n);
			return 1;
		}


		private static int l_new_tcp(lua_State L) {
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
			return function(tcp_constructor, sock_receive)
				local error,getmetatable,pcall,setmetatable,type
					= error,getmetatable,pcall,setmetatable,type
				
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
						pattern = pattern or '*l'
						local net_socket = self[1]
						local partial_data = prefix or ''
						partial_data = partial_data .. (self.rcv_buffer or '')
						
						if type(pattern) == 'number' then -- pattern is length
							local length = pattern - #partial_data
							if length < 0 then length = 0 end
							local data, err = sock_receive(net_socket, length)
							if not data then return nil, err end
							return partial_data .. data
						elseif pattern == '*l' then  -- return single line
							local line, rest = partial_data:match'([^\n]*)\n(.*)'
							if line then
								line = line:gsub('[\r\n]', '')
								self.rcv_buffer = rest
								return line
							else
								while true do
									local data, err =  sock_receive(net_socket, BLOCK_SZ)
									if not data then return nil, err, partial_data end
									--if data == '' then data = '\n' end -- force end of file
									local line, rest = data:match'([^\n]*)\n(.*)'
									if line then
										line = partial_data..line
										self.rcv_buffer = rest
										line = line:gsub('[\r\n]', '')
										return line
									end
									partial_data = partial_data .. data
								end 
							end
						elseif pattern == '*a' then  -- read all
							while true do
								local data, err =  sock_receive(net_socket, BLOCK_SZ)
								if err == 'closed' then
									return partial_data
								end
								if not data then return nil, err end
								partial_data = partial_data .. data
							end 
						end
						return nil, 'illegal format'
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

		public static int load(lua_State L) {
			LuaDLL.luaL_dostring(L, lua_code); // return function (function(tcp_constructor,..)
			LuaDLL.lua_pushstdcallcfunction(L, l_new_tcp); // set parameter..
			LuaDLL.lua_pushstdcallcfunction(L, l_sock_receive_sz); // set parameter..
			LuaDLL.lua_call(L, 2, 2); //call the returned function, returning module table + metatable
			
			// set Socket methods
			table_add_func(L, "settimeout", l_sock_settimeout);
			table_add_func(L, "connect",    l_sock_connect);
			table_add_func(L, "send",       l_sock_send);
			table_add_func(L, "close",      l_sock_close);
			
			LuaDLL.lua_pop(L, 1); // pop the metatable
			return 1; // return the module table
		}
	}
}
