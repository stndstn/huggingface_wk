#pragma once

#ifdef _WINDOWS
    #include <windows.h>

    #ifdef DLIBWRAPPER_EXPORTS
    #define DLIBWRAPPER_API __declspec(dllexport)
    #else
    #define DLIBWRAPPER_API __declspec(dllimport)
    #endif
#else
    #define DLIBWRAPPER_API
    typedef unsigned short WORD;
    typedef unsigned char BYTE;
    typedef unsigned long DWORD;
#endif
/**************************** Types declaration ******************************/
/*************************** Functions declaration ***************************/
/************************** Global Data declaration **************************/

#ifdef __cplusplus
extern "C"{
#endif

DLIBWRAPPER_API bool DlibDetectFace(unsigned char* pImageData, int nSize, int pts[8]);

#ifdef __cplusplus
}
#endif

