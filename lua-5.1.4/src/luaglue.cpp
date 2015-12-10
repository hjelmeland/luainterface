// #define LUA_BUILD_AS_DLL
#define LUA_LIB
#define LUA_CORE
#define lua_c
#define luac_c
#define loslib_c

#include <Windows.h>
#include "stdio.h"

FILE *FOPENUTF8(
	const char *filename,
	const char *mode) {

	int file_wchars_num = MultiByteToWideChar(CP_UTF8, 0, filename, -1, NULL, 0);
	wchar_t* filename_wstr = new wchar_t[file_wchars_num];
	MultiByteToWideChar(CP_UTF8, 0, filename, -1, filename_wstr, file_wchars_num);

	int mode_wchars_num = MultiByteToWideChar(CP_UTF8, 0, mode, -1, NULL, 0);
	wchar_t* mode_wstr = new wchar_t[mode_wchars_num];
	MultiByteToWideChar(CP_UTF8, 0, mode, -1, mode_wstr, mode_wchars_num);

	FILE * fp = _wfopen(filename_wstr, mode_wstr);

	delete [] filename_wstr;
	delete [] mode_wstr;
	return fp;
}

#define fopen FOPENUTF8


#include "lua.h"

#include "lapi.c"
#include "lauxlib.c"
#include "lbaselib.c"
#include "lcode.c"
#include "ldblib.c"
#include "ldebug.c"
#include "ldo.c"
#include "ldump.c"
#include "lfunc.c"
#include "lgc.c"
#include "linit.c"
#include "liolib.c"
#include "llex.c"
#include "lmathlib.c"
#include "lmem.c"
#include "loadlib.c"
#include "lobject.c"
#include "lopcodes.c"
#include "loslib.c"
#include "lparser.c"
#include "lstate.c"
#include "lstring.c"
#include "lstrlib.c"
#include "ltable.c"
#include "ltablib.c"
#include "ltm.c"
#include "lua.c"
// #include "luac.c"
#include "lundump.c"
#include "lvm.c"
#include "lzio.c"
// #include "print.c"
