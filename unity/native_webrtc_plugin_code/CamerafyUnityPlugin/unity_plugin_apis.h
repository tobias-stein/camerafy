
#ifndef CAEMRAFY_UNITY_PLUGIN_APIS_H_
#define CAEMRAFY_UNITY_PLUGIN_APIS_H_

#include <stdint.h>

// Definitions of callback functions.
typedef void (*LOCALDATACHANNELREADY_CALLBACK)();
typedef void (*DATAFROMEDATECHANNELREADY_CALLBACK)(const char* msg);
typedef void (*FAILURE_CALLBACK)(const char* msg);
typedef void (*LOCALSDPREADYTOSEND_CALLBACK)(const char* type, const char* sdp);
typedef void (*LOG_CALLBACK)(const char* msg);

#if defined(WEBRTC_WIN)
#define WEBRTC_PLUGIN_API __declspec(dllexport)
#else
#error Platform not supported.
#endif

extern "C" 
{
	WEBRTC_PLUGIN_API void SetRenderAPILogCallback(LOG_CALLBACK callback);
	WEBRTC_PLUGIN_API void QueryRenderAPIStats(LOG_CALLBACK callback);

	// WEBRTC SPECIFIC STUFF
	WEBRTC_PLUGIN_API int CreatePeerConnection();
	WEBRTC_PLUGIN_API bool InitializePeerConnection(int peer_connection_id);
	WEBRTC_PLUGIN_API bool ClosePeerConnection(int peer_connection_id);
	WEBRTC_PLUGIN_API bool CreateOffer(int peer_connection_id);
	WEBRTC_PLUGIN_API bool CreateAnswer(int peer_connection_id);
	WEBRTC_PLUGIN_API bool SetRemoteDescription(int peer_connection_id, const char* type, const char* sdp);
	WEBRTC_PLUGIN_API bool SetRenderTargetFrameProperties(int peer_connection_id, int sourceWidth, int sourceHeight);
	WEBRTC_PLUGIN_API bool AddRenderTargetVideoTrack(int peer_connection_id);
	WEBRTC_PLUGIN_API bool AddDataChannel(int peer_connection_id);
	WEBRTC_PLUGIN_API bool AddIceServer(int peer_connection_id, const char* url, const char* user, const char* password);	
	WEBRTC_PLUGIN_API bool AddNewIceCandidate(int peer_connection_id, const char* candidate, const int sdp_mlineindex, const char* sdp_mid);
	
	WEBRTC_PLUGIN_API bool SendData(int peer_connection_id, const char* data);
	WEBRTC_PLUGIN_API bool SendRenderTargetData(int peer_connection_id, void* YTexPtr, void* UTexPtr, void* VTexPtr);

	WEBRTC_PLUGIN_API bool RegisterOnLocalDataChannelReady(int peer_connection_id, LOCALDATACHANNELREADY_CALLBACK callback);
	WEBRTC_PLUGIN_API bool RegisterOnDataFromDataChannelReady(int peer_connection_id, DATAFROMEDATECHANNELREADY_CALLBACK callback);
	WEBRTC_PLUGIN_API bool RegisterOnFailure(int peer_connection_id,  FAILURE_CALLBACK callback);
	WEBRTC_PLUGIN_API bool RegisterOnLocalSdpReadytoSend(int peer_connection_id, LOCALSDPREADYTOSEND_CALLBACK callback);
	
	WEBRTC_PLUGIN_API bool SetWebrtcLogCallback(int peer_connection_id, LOG_CALLBACK callback);
}

#endif  // CAEMRAFY_UNITY_PLUGIN_APIS_H_
