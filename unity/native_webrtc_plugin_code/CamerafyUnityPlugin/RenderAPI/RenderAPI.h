#ifndef __RENDER_API_H__
#define __RENDER_API_H__

#pragma once

#include "../Unity/IUnityGraphics.h"

#define D3D11_RENDERAPI

struct IUnityInterfaces;

typedef int						TextureId;
typedef void*					NativeTexturePtr;
typedef const unsigned char*	ReadOnlyFrameDataPtr;
typedef size_t					FrameDataSize;

typedef void(*LOG_CALLBACK)(const char* msg);

const TextureId INVALID_TEXTUREID { -1 };

class RenderAPI
{
protected:

	LOG_CALLBACK LogCallback { nullptr };

	void Log(const char* fmt, ...);
	
public:

	void SetLogCallback(LOG_CALLBACK callback);

	virtual void QueryStats(LOG_CALLBACK callback) = 0;

	virtual ~RenderAPI() {}

	virtual void ProcessRenderDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) = 0;

	virtual ReadOnlyFrameDataPtr GetTextureData(NativeTexturePtr Texture) = 0;

	virtual void ReleaseTextureResources(NativeTexturePtr Texture) = 0;
};

RenderAPI* CreateRenderAPI(UnityGfxRenderer apiType);

#endif // __RENDER_API_H__