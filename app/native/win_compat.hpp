#pragma once

// Keep the Win32/OLE declarations available before GDI+ when
// WIN32_LEAN_AND_MEAN is enabled by the build.
#ifndef NOMINMAX
#define NOMINMAX
#endif
#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif

#include <windows.h>
#include <objidl.h>
#include <gdiplus.h>
