#ifndef CAMERAFY_UNITYPLUGIN_CONDUCTOR_H_
#define CAMERAFY_UNITYPLUGIN_CONDUCTOR_H_

#include <string>

#include "api/data_channel_interface.h"
#include "api/media_stream_interface.h"
#include "api/peer_connection_interface.h"
#include "api/video/video_frame.h"
#include "api/video/i420_buffer.h"
#include "media/base/video_broadcaster.h"
#include "pc/video_track_source.h"
#include "media/base/adapted_video_track_source.h"

#include "unity_plugin_apis.h"

#define USE_LOGGING
//#define USE_ADAPTIVE_VIDEOTRACKSOURCE

#define DATA_BUFFER_SIZE 4096

#ifdef USE_ADAPTIVE_VIDEOTRACKSOURCE
	class RenderTargetTrackSource : public rtc::AdaptedVideoTrackSource
	{
	public:

		static rtc::scoped_refptr<RenderTargetTrackSource> Create() { return new rtc::RefCountedObject<RenderTargetTrackSource>(); }

		inline void SendFrame(const webrtc::VideoFrame& frame) { this->OnFrame(frame); }

	protected:

		explicit RenderTargetTrackSource()
		{}

		~RenderTargetTrackSource() override = default;

		// Inherited via AdaptedVideoTrackSource
		virtual SourceState state() const override { return SourceState::kLive; }
		virtual bool remote() const override { return false; }
		virtual bool is_screencast() const override { return false; }
		virtual absl::optional<bool> needs_denoising() const override { return absl::optional<bool>(); }
	};
#else
	class RenderTargetTrackSource : public webrtc::VideoTrackSource
	{
	private:

		rtc::VideoBroadcaster VideoBroadcaster;

	public:

		static rtc::scoped_refptr<RenderTargetTrackSource> Create() { return new rtc::RefCountedObject<RenderTargetTrackSource>(); }

		inline void SendFrame(const webrtc::VideoFrame& frame) { this->VideoBroadcaster.OnFrame(frame); }

	protected:

		explicit RenderTargetTrackSource() : webrtc::VideoTrackSource(false)
		{}

		~RenderTargetTrackSource() override = default;

		// implement VideoTrackSource methods
		rtc::VideoSourceInterface<webrtc::VideoFrame>* source() override { return &this->VideoBroadcaster; }
	};
#endif // USE_ADAPTIVE_VIDEOTRACKSOURCE



#ifdef USE_LOGGING
class LogStream : public rtc::LogSink
{
private:
	
	LOG_CALLBACK	LogCallback = nullptr;

public:

	LogStream(LOG_CALLBACK callback) : LogCallback(callback)
	{
		rtc::LogMessage::AddLogToStream(this, rtc::INFO);
		this->LogCallback("Logger attached.");
	}

	virtual ~LogStream()
	{
		this->LogCallback("Logger detached.");
		
		rtc::LogMessage::RemoveLogToStream(this);
		this->LogCallback = nullptr;
	}

	// Inherited via LogSink
	virtual void OnLogMessage(const std::string& message) override
	{
		this->LogCallback(message.c_str());
	}
};
#endif // USE_LOGGING

class Conductor : public webrtc::PeerConnectionObserver,
                  public webrtc::CreateSessionDescriptionObserver,
                  public webrtc::DataChannelObserver
{

private:

	std::unique_ptr<rtc::Thread>								Worker_thread;
	std::unique_ptr<rtc::Thread>								Signaling_thread;

	webrtc::PeerConnectionInterface::RTCConfiguration			RTCConfig;

	rtc::scoped_refptr<webrtc::PeerConnectionFactoryInterface>	PeerConnectionFactory;
	rtc::scoped_refptr<webrtc::PeerConnectionInterface>			PeerConnection;
	rtc::scoped_refptr<webrtc::DataChannelInterface>			DataChannel;
	rtc::scoped_refptr<RenderTargetTrackSource>					RendertargetTrackSource;

	int															SourceFrameWidth = 640;
	int															SourceFrameHeight = 480;
	int															SourceFrameBufferSize = this->SourceFrameWidth * this->SourceFrameHeight;
	int															HalfSourceFrameWidth = this->SourceFrameWidth >> 1;
	int															HalfSourceFrameHeight = this->SourceFrameHeight >> 1;
	int															HalfSourceFrameSize = this->HalfSourceFrameWidth * this->HalfSourceFrameHeight;

	rtc::scoped_refptr<webrtc::I420Buffer>						FrameBuffer;

	char														DataBuffer[DATA_BUFFER_SIZE];

#ifdef USE_LOGGING
	LogStream*													Logger = nullptr;
#endif // USE_LOGGING

	// callbacks
	LOCALDATACHANNELREADY_CALLBACK								OnLocalDataChannelReady = nullptr;
	DATAFROMEDATECHANNELREADY_CALLBACK							OnDataFromDataChannelReady = nullptr;
	FAILURE_CALLBACK											OnFailureMessage = nullptr;
	LOCALSDPREADYTOSEND_CALLBACK								OnLocalSdpReady = nullptr;
	
	// disallow copy-and-assign
	Conductor(const Conductor&) = delete;
	Conductor& operator=(const Conductor&) = delete;

public:

	Conductor();
	~Conductor();
	
	bool InitializePeerConnection();
	void DeletePeerConnection();
	bool CreateOffer();
	bool CreateAnswer();
	bool SetRemoteDescription(const char* type, const char* sdp);
	bool AddIceServer(const char* url, const char* username, const char* password);
	bool AddNewIceCandidate(const char* candidate, const int sdp_mlineindex, const char* sdp_mid);
	bool AddRenderTargetVideoTrack();
	bool CreateDataChannel();
	bool SetRenderTargetFrameProperties(int width, int height);
	bool SendRenderTargetData(const uint8_t* YData, const uint8_t* UData, const uint8_t* VData);
	bool SendData(const std::string& data);
	

	// Register callback functions.
	void RegisterOnLocalDataChannelReady(LOCALDATACHANNELREADY_CALLBACK callback);
	void RegisterOnDataFromDataChannelReady(DATAFROMEDATECHANNELREADY_CALLBACK callback);
	void RegisterOnFailure(FAILURE_CALLBACK callback);
	void RegisterOnLocalSdpReadytoSend(LOCALSDPREADYTOSEND_CALLBACK callback);
	void RegisterLogCallback(LOG_CALLBACK callback);

protected:

	// create a peerconneciton and add the turn servers info to the configuration.
	bool CreatePeerConnectionFactory();
	
	void CloseDataChannel();
	
	// PeerConnectionObserver implementation.
	void OnSignalingChange(webrtc::PeerConnectionInterface::SignalingState new_state) override {}
	void OnAddStream(rtc::scoped_refptr<webrtc::MediaStreamInterface> stream) override;
	void OnRemoveStream(rtc::scoped_refptr<webrtc::MediaStreamInterface> stream) override {}
	void OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel) override;
	void OnRenegotiationNeeded() override {}
	void OnIceConnectionChange(webrtc::PeerConnectionInterface::IceConnectionState new_state) override {}
	void OnIceGatheringChange(webrtc::PeerConnectionInterface::IceGatheringState new_state) override {}
	void OnIceCandidate(const webrtc::IceCandidateInterface* candidate) override;
	void OnIceConnectionReceivingChange(bool receiving) override {}
	
	// CreateSessionDescriptionObserver implementation.
	void OnSuccess(webrtc::SessionDescriptionInterface* desc) override;
	void OnFailure(webrtc::RTCError error) override;
	
	// DataChannelObserver implementation.
	void OnStateChange() override;
	void OnMessage(const webrtc::DataBuffer& buffer) override;
};

#endif  // CAMERAFY_UNITYPLUGIN_CONDUCTOR_H_
