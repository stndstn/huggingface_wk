#pragma once
#ifdef _WINDOWS
    #include <windows.h>

    #ifdef OPENCVWRAPPER_EXPORTS
    #define OPENCVWRAPPER_API __declspec(dllexport)
    #else
    #define OPENCVWRAPPER_API __declspec(dllimport)
    #endif
#else
    #define OPENCVWRAPPER_API
#endif
/**************************** Types declaration ******************************/
/*************************** Functions declaration ***************************/
/************************** Global Data declaration **************************/

#ifdef __cplusplus
extern "C"{
#endif
    const unsigned int     MATCH_TEMPLATE_RESULT_ITEM_NAME_MAXLEN = 255;
typedef struct _MATCH_TEMPLATE_RESULT_ITEM {
    char name[MATCH_TEMPLATE_RESULT_ITEM_NAME_MAXLEN + 1];
    int locX;
    int locY;
    int width;
    int height;
    double MatchResult;
} MATCH_TEMPLATE_RESULT_ITEM;
typedef MATCH_TEMPLATE_RESULT_ITEM* PMATCH_TEMPLATE_RESULT_ITEM;
typedef const MATCH_TEMPLATE_RESULT_ITEM* PCMATCH_TEMPLATE_RESULT_ITEM;

typedef struct _MATCH_TEMPLATE_RESULT {
    PMATCH_TEMPLATE_RESULT_ITEM pItems;
    unsigned int countItems;
} MATCH_TEMPLATE_RESULT;
typedef MATCH_TEMPLATE_RESULT* PMATCH_TEMPLATE_RESULT;
typedef const MATCH_TEMPLATE_RESULT* PCMATCH_TEMPLATE_RESULT;

OPENCVWRAPPER_API int Hello(const char* pszName);
//OPENCVWRAPPER_API PCMATCH_TEMPLATE_RESULT DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath);
OPENCVWRAPPER_API void* DoMatchTemplate(unsigned char* pDocImageData, unsigned int docImageLen, const char* pszTemplateFolderPath);
OPENCVWRAPPER_API int FreeMatchTemplateResult(void* pResult);

#ifdef __cplusplus
}
#endif

