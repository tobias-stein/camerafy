#include "../RenderAPI.h"

#ifdef D3D11_RENDERAPI

#include <d3d11.h>
#include "IUnityGraphicsD3D11.h"

#include <unordered_map>
#include <mutex>
#include <sstream>

class D3D11RenderAPI : public RenderAPI
{
private:

	std::mutex					TextureMapMutex;

	struct Ctx
	{
		ID3D11Texture2D*		AccessorTexture;
		D3D11_TEXTURE2D_DESC	Desc;
		unsigned char*			Data;
	};

	ID3D11Device*	Device;

	std::unordered_map<ID3D11Texture2D*, Ctx> TextureCopyMap;

public:

	D3D11RenderAPI() :
		Device(nullptr)
	{}

	virtual ~D3D11RenderAPI()
	{
		std::lock_guard<std::mutex> lock(this->TextureMapMutex);

		for(auto kvp : this->TextureCopyMap)
		{
			Ctx& ctx = kvp.second;

			if(ctx.AccessorTexture)
			{
				ctx.AccessorTexture->Release();
				ctx.AccessorTexture = nullptr;
			}

			if(ctx.Data)
			{
				delete ctx.Data;
				ctx.Data = nullptr;
			}
		}

		this->TextureCopyMap.clear();

		this->Device = nullptr;
	}

	virtual void ProcessRenderDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) override
	{
		switch (type)
		{
			case kUnityGfxDeviceEventInitialize:
			{
				IUnityGraphicsD3D11* d3d = interfaces->Get<IUnityGraphicsD3D11>();
				this->Device = d3d->GetDevice();
				break;
			}

			case kUnityGfxDeviceEventShutdown:
			{
				break;
			}

			case kUnityGfxDeviceEventBeforeReset:
			{
				break;
			}

			case kUnityGfxDeviceEventAfterReset:
			{
				break;
			}
		}
	}

	virtual ReadOnlyFrameDataPtr GetTextureData(NativeTexturePtr Texture) override
	{
		std::lock_guard<std::mutex> lock(this->TextureMapMutex);

		ID3D11Texture2D* texture = reinterpret_cast<ID3D11Texture2D*>(Texture);
		if (texture == nullptr)
			return nullptr;

		// read texture description
		D3D11_TEXTURE2D_DESC desc;
		texture->GetDesc(&desc);

		// get cached accessor texture
		Ctx* ctx = nullptr;

		auto it = this->TextureCopyMap.find(texture);
		if(it != this->TextureCopyMap.end())
		{
			ctx = &it->second;
		}
		else
		{
			desc.BindFlags = 0;
			desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ | D3D11_CPU_ACCESS_WRITE;
			desc.Usage = D3D11_USAGE_STAGING;

			ID3D11Texture2D* accessorTexture = nullptr;
			if (this->Device->CreateTexture2D(&desc, NULL, &accessorTexture) != 0)
			{
				Log("FAILURE: Failed to create a back-end texture for %p (%dx%d)!", Texture, desc.Width, desc.Height);
				if (accessorTexture)
				{
					accessorTexture->Release();
					accessorTexture = nullptr;
				}
				return nullptr;
			}
			else
			{
				Log("New back-end texture [%p] created for %p (%dx%d)!", accessorTexture, Texture, desc.Width, desc.Height);
				this->TextureCopyMap[texture] = Ctx
				{
					accessorTexture,
					desc,
					nullptr
				};

				ctx = &this->TextureCopyMap[texture];
			}
		}

		ID3D11DeviceContext* DeviceCtx = nullptr;
		this->Device->GetImmediateContext(&DeviceCtx);
		if(DeviceCtx != nullptr)
		{
			DeviceCtx->CopyResource(ctx->AccessorTexture, texture);

			D3D11_MAPPED_SUBRESOURCE mapped;
			auto hr = DeviceCtx->Map(ctx->AccessorTexture, 0, D3D11_MAP_READ, 0, &mapped);
			if(hr != 0)
			{
				Log("FAILURE: Failed to back-end texture %p. Device-Error: %d", ctx->AccessorTexture, hr);
				return nullptr;
			}

			if(ctx->Data == nullptr)
			{
				size_t size = ctx->Desc.Width * ctx->Desc.Height;

				Log("Allocate cpu-readable texture buffer [%d bytes] for back-end texture %p", size, ctx->AccessorTexture);
				
				ctx->Data = new unsigned char[size];
				memset(ctx->Data, 0, size);
			}

			unsigned char* src = reinterpret_cast<unsigned char*>(mapped.pData);
			unsigned char* dst = ctx->Data;
			
			for(unsigned int i = 0; i < ctx->Desc.Height; ++i)
			{
				memcpy(dst, src, ctx->Desc.Width);
				src += mapped.RowPitch;
				dst += ctx->Desc.Width;
			}
			
			DeviceCtx->Unmap(ctx->AccessorTexture, 0);

			// return copied texture data
			return ctx->Data;
		}

		return nullptr;
	}

	virtual void ReleaseTextureResources(NativeTexturePtr Texture) override
	{
		std::lock_guard<std::mutex> lock(this->TextureMapMutex);

		ID3D11Texture2D* texture = reinterpret_cast<ID3D11Texture2D*>(Texture);
		if (texture == nullptr)
			return;

		auto it = this->TextureCopyMap.find(texture);
		if(it != this->TextureCopyMap.end())
		{
			Ctx& ctx = (*it).second;

			Log("Releasing back-end texture [%p, %dx%d] for %p ...", ctx.AccessorTexture, ctx.Desc.Width, ctx.Desc.Height, Texture);
			if (ctx.AccessorTexture)
			{
				ctx.AccessorTexture->Release();
				ctx.AccessorTexture = nullptr;
			}

			if (ctx.Data)
			{
				delete ctx.Data;
				ctx.Data = nullptr;
			}

			this->TextureCopyMap.erase(it);
		}
	}

	virtual void QueryStats(LOG_CALLBACK callback) override
	{
		std::lock_guard<std::mutex> lock(this->TextureMapMutex);

		if(callback == nullptr)
			return;

		std::stringstream BUFFER;

		size_t TotalTextureMemory = 0;

		BUFFER << "=== BEGIN - NATIVE RENDERAPI STATS =============================================" << std::endl;
		BUFFER << "back-end texture, front-end texture, tex. width, tex. height, tex. size" << std::endl;

		for(auto kvp : this->TextureCopyMap)
		{
			const Ctx& ctx = kvp.second;

			size_t AllocatedMemory = ctx.Desc.Width * ctx.Desc.Height;
			TotalTextureMemory += AllocatedMemory;

			BUFFER << (void*)kvp.first << ", " 
			       << (void*)ctx.AccessorTexture << ", "
				   << ctx.Desc.Width << ", " << ctx.Desc.Height << ", "
				   << AllocatedMemory << std::endl;
		}

		BUFFER << "--------------------------------------------------------------------------------" << std::endl;
		BUFFER << "TotalTextureMemory-CPU: " << TotalTextureMemory << std::endl;
		BUFFER << "TotalTextureMemory-GPU: " << TotalTextureMemory << std::endl;
		BUFFER << "=== END - NATIVE RENDERAPI STATS ===============================================" << std::endl;

		callback(BUFFER.str().c_str());
	}
};

RenderAPI* CreateD3D11RenderAPI() { return new D3D11RenderAPI(); }

#endif // D3D11_RENDERAPI