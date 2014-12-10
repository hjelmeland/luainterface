using LuaDLL = Lua511.LuaDLL;
using lua_State = System.IntPtr;

namespace Lua511.Module
{
    public class socket_core
    {
        public static int load(lua_State L)
        {
            LuaDLL.lua_newtable(L);
            return 1;
        }
    }
}
