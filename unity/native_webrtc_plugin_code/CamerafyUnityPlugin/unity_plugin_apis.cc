#include "unity_plugin_apis.h"
#include "Conductor.h"

#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"

#include "RenderAPI/RenderAPI.h"

#include <unordered_map>
#include <string>
#include <mutex>

#define GET_PEERCONNECTION_SAFE(id)								\
	if(g_peer_connection_map.count(id) == 0)					\
		return false;											\
	auto PeerConnection = g_peer_connection_map[id].Conductor;
	


namespace 
{
	struct PeerConnectionContext
	{
		rtc::scoped_refptr<Conductor>	Conductor;

		void*							YTexPtr;
		void*							UTexPtr;
		void*							VTexPtr;
	};

	static int														g_next_valid_connection_id { 1 };
	static std::unordered_map<int, PeerConnectionContext>			g_peer_connection_map;
	static std::mutex												g_peer_connection_map_mutex;

	static IUnityInterfaces*										g_UnityInterfaces = nullptr;
	static IUnityGraphics*											g_Graphics = nullptr;
	static RenderAPI*												g_RenderAPI = nullptr;

}  // namespace

// forward declaration
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType);

extern "C"
{
	// UNITY SPECIFIC STUFF
	void WEBRTC_PLUGIN_API UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
	{
		g_UnityInterfaces = unityInterfaces;
		g_Graphics = g_UnityInterfaces->Get<IUnityGraphics>();
		g_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

		// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
		OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
	}

	void WEBRTC_PLUGIN_API UNITY_INTERFACE_API UnityPluginUnload()
	{
		g_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}
}

void SetRenderAPILogCallback(LOG_CALLBACK callback)
{
	if(g_RenderAPI)
	{
		g_RenderAPI->SetLogCallback(callback);
	}
}

void QueryRenderAPIStats(LOG_CALLBACK callback)
{
	if(g_RenderAPI)
	{
		g_RenderAPI->QueryStats(callback);
	}
	else
	{
		if(callback)
		{
			callback("RenderAPI not initialized yet.");
		}
	}
}

static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	switch(eventType)
	{
		case kUnityGfxDeviceEventInitialize:
		{
			// initialize render api
			g_RenderAPI = CreateRenderAPI(g_Graphics->GetRenderer());
			break;
		}

		case kUnityGfxDeviceEventShutdown:
		{
			// release render api
			delete g_RenderAPI;
			g_RenderAPI = nullptr;
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

	// delegate event to specific render api implementation
	if(g_RenderAPI != nullptr)
	{
		g_RenderAPI->ProcessRenderDeviceEvent(eventType, g_UnityInterfaces);
	}
}




int CreatePeerConnection() 
{
	std::lock_guard<std::mutex> lock(g_peer_connection_map_mutex);

	int connectionId = g_next_valid_connection_id++;
	g_peer_connection_map[connectionId] = PeerConnectionContext
	{
		// Conductor
		new rtc::RefCountedObject<Conductor>()
	};

	if (!g_peer_connection_map[connectionId].Conductor)
		return -1;

	return connectionId;
}

bool ClosePeerConnection(int peer_connection_id)
{
	std::lock_guard<std::mutex> lock(g_peer_connection_map_mutex);

	auto connection = g_peer_connection_map.find(peer_connection_id);
	if (connection != g_peer_connection_map.end())
	{
		PeerConnectionContext& ctx = connection->second;

		// release YUV textures
		g_RenderAPI->ReleaseTextureResources(ctx.YTexPtr);
		g_RenderAPI->ReleaseTextureResources(ctx.UTexPtr);
		g_RenderAPI->ReleaseTextureResources(ctx.VTexPtr);

		// Close peer conection
		ctx.Conductor->DeletePeerConnection();

		// remove context
		g_peer_connection_map.erase(connection);
		return true;
	}

	return false;
}

bool InitializePeerConnection(int peer_connection_id)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->InitializePeerConnection();
}

bool AddIceServer(int peer_connection_id, const char* url, const char* user, const char* password)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->AddIceServer(url, user, password);
}



bool SetRenderTargetFrameProperties(int peer_connection_id, int sourceWidth, int sourceHeight)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->SetRenderTargetFrameProperties(sourceWidth, sourceHeight);
}

bool AddRenderTargetVideoTrack(int peer_connection_id)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->AddRenderTargetVideoTrack();
}

bool AddDataChannel(int peer_connection_id) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->CreateDataChannel();
}

bool CreateOffer(int peer_connection_id) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->CreateOffer();
}

bool CreateAnswer(int peer_connection_id) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->CreateAnswer();
}

bool AddNewIceCandidate(int peer_connection_id, const char* candidate, const int sdp_mlineindex, const char* sdp_mid)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->AddNewIceCandidate(candidate, sdp_mlineindex, sdp_mid);
}

bool SendData(int peer_connection_id, const char* data) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->SendData(std::string(data));
}

bool SendRenderTargetData(int peer_connection_id, void* YTexPtr, void* UTexPtr, void* VTexPtr)
{
	if (g_peer_connection_map.count(peer_connection_id) != 0 && g_RenderAPI != nullptr)
	{
		PeerConnectionContext& ctx = g_peer_connection_map[peer_connection_id];

		// create/re-create backbuffer texture, if target texture changed.
		if(ctx.YTexPtr != YTexPtr) { g_RenderAPI->ReleaseTextureResources(ctx.YTexPtr); ctx.YTexPtr = YTexPtr; }
		if(ctx.UTexPtr != UTexPtr) { g_RenderAPI->ReleaseTextureResources(ctx.UTexPtr); ctx.UTexPtr = UTexPtr; }
		if(ctx.VTexPtr != VTexPtr) { g_RenderAPI->ReleaseTextureResources(ctx.VTexPtr); ctx.VTexPtr = VTexPtr; }

		// read textures data
		ReadOnlyFrameDataPtr YData = g_RenderAPI->GetTextureData(ctx.YTexPtr);
		ReadOnlyFrameDataPtr UData = g_RenderAPI->GetTextureData(ctx.UTexPtr);
		ReadOnlyFrameDataPtr VData = g_RenderAPI->GetTextureData(ctx.VTexPtr);
		
		// send data to conductor
		ctx.Conductor->SendRenderTargetData(YData, UData, VData);

		return true;
	}

	return false;
}

bool SetRemoteDescription(int peer_connection_id, const char* type, const char* sdp) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	return PeerConnection->SetRemoteDescription(type, sdp);
}

// Register callback functions.

bool RegisterOnLocalDataChannelReady(int peer_connection_id, LOCALDATACHANNELREADY_CALLBACK callback) 
{
  GET_PEERCONNECTION_SAFE(peer_connection_id)
  PeerConnection->RegisterOnLocalDataChannelReady(callback);
  return true;
}

bool RegisterOnDataFromDataChannelReady(int peer_connection_id, DATAFROMEDATECHANNELREADY_CALLBACK callback) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	PeerConnection->RegisterOnDataFromDataChannelReady(callback);
	return true;
}

bool RegisterOnFailure(int peer_connection_id, FAILURE_CALLBACK callback) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	PeerConnection->RegisterOnFailure(callback);
	return true;
}

// Singnaling channel related functions.
bool RegisterOnLocalSdpReadytoSend(int peer_connection_id, LOCALSDPREADYTOSEND_CALLBACK callback) 
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	PeerConnection->RegisterOnLocalSdpReadytoSend(callback);
	return true;
}

bool SetWebrtcLogCallback(int peer_connection_id, LOG_CALLBACK callback)
{
	GET_PEERCONNECTION_SAFE(peer_connection_id)
	PeerConnection->RegisterLogCallback(callback);
	return true;
}
