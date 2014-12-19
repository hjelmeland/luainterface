/*
 * This is the bit32 library (lbitlib.c) from lua 5.2.2,
 * backported to lua 5.1.
 *
 * version 5.2.2
 *
 * Copyright (C) 1994-2013 Lua.org, PUC-Rio.  All rights reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

/* ported to C# by Egil Hjelmeland 2014-12-08 */

using LuaDLL = Lua511.LuaDLL;
using b_uint = System.UInt32;
using lua_State = System.IntPtr;

namespace Lua511.Module
{
	public class bit32
	{
		private const b_uint ALLONES = 0xffffffff;
		private const int LUA_NBITS = 32;

		private static uint andaux(lua_State L)
		{
			int n = LuaDLL.lua_gettop(L);
			b_uint r = ALLONES;
			for (int i = 1; i <= n; i++)
				r &= (b_uint)LuaDLL.luaL_checknumber(L, i);
			return r & ALLONES;
		}

		private static int b_and(lua_State L)
		{
			b_uint r = andaux(L);
			LuaDLL.lua_pushnumber(L, r);
			return 1;
		}

		private static int b_test(lua_State L)
		{
			b_uint r = andaux(L);
			LuaDLL.lua_pushboolean(L, r != 0);
			return 1;
		}

		private static int b_or(lua_State L)
		{
			int n = LuaDLL.lua_gettop(L);
			b_uint r = 0;
			for (int i = 1; i <= n; i++)
				r |= (b_uint)LuaDLL.luaL_checknumber(L, i);
			LuaDLL.lua_pushnumber(L, r & ALLONES);
			return 1;
		}

		private static int b_xor(lua_State L)
		{
			int n = LuaDLL.lua_gettop(L);
			b_uint r = 0;
			for (int i = 1; i <= n; i++)
				r ^= (b_uint)LuaDLL.luaL_checknumber(L, i);
			LuaDLL.lua_pushnumber(L, r & ALLONES);
			return 1;
		}

		private static int b_not(lua_State L)
		{
			b_uint r = ~(b_uint)LuaDLL.luaL_checknumber(L, 1);
			LuaDLL.lua_pushnumber(L, r & ALLONES);
			return 1;
		}


		private static int b_shift(lua_State L, b_uint r, int i)
		{
			if (i < 0)
			{  /* shift right? */
				i = -i;
				r &= ALLONES;
				if (i >= LUA_NBITS) r = 0;
				else r >>= i;
			}
			else
			{  /* shift left */
				if (i >= LUA_NBITS) r = 0;
				else r <<= i;
				r &= ALLONES;
			}
			LuaDLL.lua_pushnumber(L, r);
			return 1;
		}

		private static int b_lshift(lua_State L)
		{
			return b_shift(L, (b_uint)LuaDLL.luaL_checknumber(L, 1), (int)LuaDLL.luaL_checknumber(L, 2));
		}

		private static int b_rshift(lua_State L)
		{
			return b_shift(L, (b_uint)LuaDLL.luaL_checknumber(L, 1), -(int)LuaDLL.luaL_checknumber(L, 2));
		}

		private static int b_arshift(lua_State L)
		{
			b_uint r = (b_uint)LuaDLL.luaL_checknumber(L, 1);
			int i = (int)LuaDLL.luaL_checknumber(L, 2);
			if (i < 0 || ( (r & ((b_uint)1 << (LUA_NBITS - 1)))== 0) )
				return b_shift(L, r, -i);
			else
			{  /* arithmetic shift for 'negative' number */
				if (i >= LUA_NBITS) r = ALLONES;
				else
					r = (r >> i) | ~(~(b_uint)0 >> i);  /* add signal bit */
				LuaDLL.lua_pushnumber(L, r & ALLONES);
				return 1;
			}
		}

		private static int b_rot(lua_State L, int i)
		{
			b_uint r = (b_uint)LuaDLL.luaL_checknumber(L, 1);
			i &= (LUA_NBITS - 1);  /* i = i % NBITS */
			r &= ALLONES;
			r = (r << i) | (r >> (LUA_NBITS - i));
			LuaDLL.lua_pushnumber(L, r & ALLONES);
			return 1;
		}


		private static int b_lrot(lua_State L)
		{
			return b_rot(L, (int)LuaDLL.luaL_checknumber(L, 2));
		}


		private static int b_rrot(lua_State L)
		{
			return b_rot(L, -(int)LuaDLL.luaL_checknumber(L, 2));
		}


		/*
		** get field and width arguments for field-manipulation functions,
		** checking whether they are valid.
		** ('luaL_error' called without 'return' to avoid later warnings about
		** 'width' being used uninitialized.)
		*/
		private static int fieldargs(lua_State L, int farg, int width)
		{
			int f = (int)LuaDLL.luaL_checknumber(L, farg);
			int w = width;
			LuaDLL.luaL_argcheck(L, 0 <= f, farg, "field cannot be negative");
			LuaDLL.luaL_argcheck(L, 0 < w, farg + 1, "width must be positive");
			if (f + w > LUA_NBITS)
				LuaDLL.luaL_error(L, "trying to access non-existent bits");
			return f;
		}

		private static b_uint mask (int n) {
			return (~((ALLONES << 1) << ((n) - 1)));
		}


		private static int b_extract(lua_State L)
		{
			b_uint r = (b_uint)LuaDLL.luaL_checknumber(L, 1);
			int w = (int)LuaDLL.luaL_optnumber(L, 3, 1);
			int f = fieldargs(L, 2, w);
			r = (r >> f) & mask(w);
			LuaDLL.lua_pushnumber(L, r & ALLONES); 
			return 1;
		}

		private static int b_replace(lua_State L)
		{
			b_uint r = (b_uint)LuaDLL.luaL_checknumber(L, 1);
			b_uint v = (b_uint)LuaDLL.luaL_checknumber(L, 2);
			int w = (int)LuaDLL.luaL_optnumber(L, 4, 1);
			int f = fieldargs(L, 3, w);
			b_uint m = mask(w);
			v &= m;  /* erase bits outside given width */
			r = (r & ~(m << f)) | (v << f);
			LuaDLL.lua_pushnumber(L, r & ALLONES); 
			return 1;
		}



		// add a function as name to table at top of stack
		private static void table_add_func (lua_State L, string name, LuaCSFunction function) {
			LuaDLL.lua_pushstring (L, name); // key..
			LuaDLL.lua_pushstdcallcfunction(L, function); // value..
			LuaDLL.lua_settable(L, -3); // top[key] = function
		}

		// need to anchor the delegate objects, so .net GC do not snatch them. http://stackoverflow.com/questions/7302045/callback-delegates-being-collected
		static private LuaCSFunction db_arshift   = new LuaCSFunction(b_arshift );
		static private LuaCSFunction db_and       = new LuaCSFunction(b_and );
		static private LuaCSFunction db_test      = new LuaCSFunction(b_test );
		static private LuaCSFunction db_or        = new LuaCSFunction(b_or );
		static private LuaCSFunction db_xor       = new LuaCSFunction(b_xor );
		static private LuaCSFunction db_not       = new LuaCSFunction(b_not );
		static private LuaCSFunction db_extract   = new LuaCSFunction(b_extract );
		static private LuaCSFunction db_replace   = new LuaCSFunction(b_replace );
		static private LuaCSFunction db_lrot      = new LuaCSFunction(b_lrot );
		static private LuaCSFunction db_rrot      = new LuaCSFunction(b_rrot );
		static private LuaCSFunction db_lshift    = new LuaCSFunction(b_lshift );
		static private LuaCSFunction db_rshift    = new LuaCSFunction(b_rshift );

		public static int load(lua_State L)
		{
			LuaDLL.lua_newtable(L);
			table_add_func(L, "arshift", b_arshift);
			table_add_func(L, "band", b_and);
			table_add_func(L, "btest", b_test);
			table_add_func(L, "bor", b_or);
			table_add_func(L, "bxor", b_xor);
			table_add_func(L, "bnot", b_not);
			table_add_func(L, "extract", b_extract);
			table_add_func(L, "replace", b_replace);
			table_add_func(L, "lrotate", b_lrot);
			table_add_func(L, "rrotate", b_rrot);
			table_add_func(L, "lshift", b_lshift);
			table_add_func(L, "rshift", b_rshift);
			return 1;
		}
	}
}
