using LuaDLL = Lua511.LuaDLL;
using LuaTypes = Lua511.LuaTypes;
using lua_State = System.IntPtr;
using Convert = System.Convert;
using Buffer = System.Buffer;

// Subset of luasockets mime/core.dll

namespace Lua511.Module {

	public class mime_core {
		
		static private int l_b64(lua_State L)
		{
			if (LuaDLL.lua_type(L, 1) != LuaTypes.LUA_TSTRING)
				return 0;

			byte[] a1 = LuaDLL.lua_tobytes(L, 1);
			string b64;
			if (LuaDLL.lua_type(L, 2) == LuaTypes.LUA_TSTRING) {
				byte[] a2 = LuaDLL.lua_tobytes(L, 2);
				int len = a1.Length + a2.Length;
				byte[] r1 = new byte[len];
				Buffer.BlockCopy( a1, 0, r1, 0, a1.Length );
				Buffer.BlockCopy( a2, 0, r1, a1.Length, a2.Length );
				int mod = len % 3;
				b64 = Convert.ToBase64String (r1, 0, len - mod);
				LuaDLL.lua_pushstring(L, b64);

				if ( mod > 0 ) {
					byte[] r2 = new byte[mod];
					Buffer.BlockCopy( r1, len - mod, r2, 0, mod );
					LuaDLL.lua_pushbytes(L, r2);
				}
				else
					LuaDLL.lua_pushstring(L, "");
				return 2;
			}
			
			b64 = Convert.ToBase64String (a1);
			LuaDLL.lua_pushstring(L, b64);
			LuaDLL.lua_pushnil(L);
			
			return 2;
		}
		
		static private int l_unb64(lua_State L)
		{
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TSTRING);
			string s = LuaDLL.lua_tostring(L, 1);
			bool p2 = false; 
			if (LuaDLL.lua_type(L, 2) == LuaTypes.LUA_TSTRING) {
				p2 = true;
				s = s + LuaDLL.lua_tostring(L, 2);
			}
			int len = s.Length;
			int mod = len % 4;
			string rest = "";
			if (mod > 0) {
				rest = s.Substring(len-mod, mod);
				s = s.Substring(0, len-mod);
			}
			byte[] b1 = Convert.FromBase64String(s);
			LuaDLL.lua_pushbytes(L, b1);
			if (p2)
				LuaDLL.lua_pushstring(L, rest);
			else
				LuaDLL.lua_pushnil(L);
			return 2;
		}


		// add a function as name to table at top of stack
		private static void table_add_func (lua_State L, string name, LuaCSFunction function) {
			LuaDLL.lua_pushstring (L, name); // key..
			LuaDLL.lua_pushstdcallcfunction(L, function); // value..
			LuaDLL.lua_settable(L, -3); // top[key] = function
		}
		
		public static int load(lua_State L) {
			LuaDLL.lua_newtable(L);
			table_add_func(L, "b64", l_b64);
			table_add_func(L, "unb64", l_unb64);
			return 1;
		}
	}
}
