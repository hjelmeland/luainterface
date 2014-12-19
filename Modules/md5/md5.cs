using LuaDLL = Lua511.LuaDLL;
using LuaTypes = Lua511.LuaTypes;
using lua_State = System.IntPtr;
using MD5 = System.Security.Cryptography.MD5;

// Subset of http://keplerproject.org/md5/

namespace Lua511.Module {

	public class md5_core {
		static MD5 md5summer = MD5.Create();
		
		static private int l_md5sum(lua_State L) {
			LuaDLL.luaL_checktype(L, 1, LuaTypes.LUA_TSTRING);
			byte[] bytes = LuaDLL.lua_tobytes(L, 1);
			byte[] sum = md5summer.ComputeHash(bytes);
			LuaDLL.lua_pushbytes(L, sum); 
			return 1;
		}

		// add a function as name to table at top of stack
		private static void table_add_func (lua_State L, string name, LuaCSFunction function) {
			LuaDLL.lua_pushstring (L, name); // key..
			LuaDLL.lua_pushstdcallcfunction(L, function); // value..
			LuaDLL.lua_settable(L, -3); // top[key] = function
		}
		
		// need to anchor the delegate objects, so .net GC do not snatch them.
		static private LuaCSFunction dl_md5sum       = new LuaCSFunction(l_md5sum );

		public static int load(lua_State L) {
			LuaDLL.lua_newtable(L);
			table_add_func(L, "sum", dl_md5sum);
			return 1;
		}
	}
}
