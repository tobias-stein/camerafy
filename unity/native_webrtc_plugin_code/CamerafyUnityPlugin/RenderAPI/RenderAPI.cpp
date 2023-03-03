#include "RenderAPI.h"

#include <cstdarg>
#include <cstdio>

RenderAPI* CreateRenderAPI(UnityGfxRenderer apiType)
{
#ifdef D3D11_RENDERAPI
	if (apiType == kUnityGfxRendererD3D11)
	{
		extern RenderAPI* CreateD3D11RenderAPI();
		return CreateD3D11RenderAPI();
	}
#endif

	// unsupported render api
	return nullptr;
}

void RenderAPI::SetLogCallback(LOG_CALLBACK callback) 
{ 
	this->LogCallback = callback;
	if (this->LogCallback)
	{
		Log("RenderAPI log callback set.");
	}
}

void RenderAPI::Log(const char* fmt, ...) 
{ 
	static char STR_BUFFER[4096] { '\0' };

	if (LogCallback)
	{
		va_list argptr;
		va_start(argptr, fmt);
		vsprintf(STR_BUFFER, fmt, argptr);
		va_end(argptr);

		LogCallback(STR_BUFFER);
	}
}