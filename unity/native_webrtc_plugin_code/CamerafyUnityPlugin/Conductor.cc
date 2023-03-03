#include "Conductor.h"

#include <memory>
#include <utility>

#include "absl/memory/memory.h"
#include "api/create_peerconnection_factory.h"
#include "api/audio_codecs/builtin_audio_decoder_factory.h"
#include "api/audio_codecs/builtin_audio_encoder_factory.h"
#include "api/video_codecs/builtin_video_decoder_factory.h"
#include "api/video_codecs/builtin_video_encoder_factory.h"
#include "api/video/video_frame_buffer.h"
#include "third_party/libyuv/include/libyuv.h"


namespace 
{
	class DummySetSessionDescriptionObserver : public webrtc::SetSessionDescriptionObserver 
	{
	public:
	  static DummySetSessionDescriptionObserver* Create() { return new rtc::RefCountedObject<DummySetSessionDescriptionObserver>(); }
	
	  virtual void OnSuccess() { RTC_LOG(INFO) << __FUNCTION__; }
	  virtual void OnFailure(webrtc::RTCError error) { RTC_LOG(INFO) << "Webrtc session failure: " << error.message(); }
	
	 protected:
	  DummySetSessionDescriptionObserver() {}
	  ~DummySetSessionDescriptionObserver() {}
	};

}  // namespace

Conductor::Conductor()
{
	RTC_LOG(INFO) << "Conductor instance created.";
}

Conductor::~Conductor()
{
#ifdef USE_LOGGING
	if (Logger != nullptr)
	{
		delete Logger;
		Logger = nullptr;
	}
#endif //USE_LOGGING

	RTC_LOG(INFO) << "Conductor instance released.";
}

bool Conductor::InitializePeerConnection() 
{
	RTC_LOG(INFO) << "Conductor::InitializePeerConnection called.";

	// initialize the peer connection factory
	if(!this->CreatePeerConnectionFactory())
	{
		RTC_LOG(LS_ERROR) << "Failed to create peer connection factory.";
		return false;
	}

	// setup rtc configuration
	this->RTCConfig.enable_dtls_srtp			= true;
	this->RTCConfig.sdp_semantics				= webrtc::SdpSemantics::kUnifiedPlan;
	//this->RTCConfig.offer_extmap_allow_mixed	= true;

	// create a new peer connection
	this->PeerConnection = this->PeerConnectionFactory->CreatePeerConnection(this->RTCConfig, nullptr, nullptr, this);

	if (PeerConnection.get() != nullptr)
		RTC_LOG(INFO) << "Peer connection created.";

	return PeerConnection.get() != nullptr;
}

bool Conductor::CreatePeerConnectionFactory() 
{
	RTC_DCHECK(this->PeerConnection.get() == nullptr);
	if (this->PeerConnectionFactory == nullptr)
	{
		this->Worker_thread = std::unique_ptr<rtc::Thread>(new rtc::Thread());
		this->Worker_thread->Start();
		this->Signaling_thread = std::unique_ptr<rtc::Thread>(new rtc::Thread());
		this->Signaling_thread->Start();

		this->PeerConnectionFactory = webrtc::CreatePeerConnectionFactory(
			nullptr,
			this->Worker_thread.get(),
			this->Signaling_thread.get(),
			nullptr /* default_adm */,
			webrtc::CreateBuiltinAudioEncoderFactory(),
			webrtc::CreateBuiltinAudioDecoderFactory(),
			webrtc::CreateBuiltinVideoEncoderFactory(),
			webrtc::CreateBuiltinVideoDecoderFactory(),
			nullptr /* audio_mixer */,
			nullptr /* audio_processing */);
	}

	if (!this->PeerConnectionFactory.get())
	{
		DeletePeerConnection();
		return false;
	}

	return true;
}

bool Conductor::AddIceServer(const char* url, const char* username, const char* password)
{
	RTC_LOG(INFO) << "Conductor::AddIceServer(" << url << ", " << username << ", " << password << ") called.";

	// Add the stun/turn server.
	if (url != nullptr)
	{
		webrtc::PeerConnectionInterface::IceServer server;
		{
			server.urls.push_back(std::string(url));
			server.username = std::string(username ? username : "");
			server.password = std::string(password ? password : "");
		}
		this->RTCConfig.servers.push_back(server);
	}

	return true;
}

void Conductor::DeletePeerConnection() 
{
	RTC_LOG(INFO) << "Conductor::DeletePeerConnection() called.";

	if(this->PeerConnection.get())
	{
		RTC_LOG(INFO) << "Closing Peer connection...";
		CloseDataChannel();
		PeerConnection->Close();
		this->PeerConnection = nullptr;
		this->PeerConnectionFactory = nullptr;
		this->Signaling_thread.reset(nullptr);
		this->Worker_thread.reset(nullptr);
		RTC_LOG(INFO) << "Connection closed.";
	}
}

bool Conductor::CreateOffer() 
{
	RTC_LOG(INFO) << "Conductor::CreateOffer() called.";

	if (!PeerConnection.get())
	  return false;

	auto config = webrtc::PeerConnectionInterface::RTCOfferAnswerOptions();
	config.offer_to_receive_video = true;

	PeerConnection->CreateOffer(this, config);
	return true;
}

bool Conductor::CreateAnswer()
{
	RTC_LOG(INFO) << "Conductor::CreateAnswer() called.";

	if (!PeerConnection.get())
		return false;

	PeerConnection->CreateAnswer(this, webrtc::PeerConnectionInterface::RTCOfferAnswerOptions());
	return true;
}

bool Conductor::SetRemoteDescription(const char* type, const char* sdp) 
{
	RTC_LOG(INFO) << "Conductor::SetRemoteDescription(" << type << ", " << sdp << ") called.";

	if (!PeerConnection.get())
	  return false;
	
	absl::optional<webrtc::SdpType> type_maybe = webrtc::SdpTypeFromString(type);
	if (!type_maybe)
	{
		RTC_LOG(LS_ERROR) << "Unknown SDP type: " << type;
		return false;
	}
	webrtc::SdpType sdp_type = *type_maybe;
	std::string remote_desc(sdp);
	webrtc::SdpParseError error;
	std::unique_ptr<webrtc::SessionDescriptionInterface> session_description(webrtc::CreateSessionDescription(sdp_type, remote_desc, &error));
	if (!session_description) 
	{
	  RTC_LOG(WARNING) << "Can't parse received session description message. " << "SdpParseError was: " << error.description;
	  return false;
	}

	RTC_LOG(INFO) << "Received session description: " << remote_desc;
	PeerConnection->SetRemoteDescription(DummySetSessionDescriptionObserver::Create(), session_description.release());
	return true;
}

bool Conductor::AddRenderTargetVideoTrack() 
{
	RTC_LOG(INFO) << "Conductor::AddRenderTargetVideoTrack() called.";

	if (!PeerConnection->GetSenders().empty())
		return true;  // Already added tracks.

	// create new source
	this->RendertargetTrackSource = RenderTargetTrackSource::Create();
    if (this->RendertargetTrackSource)
	{
		// create new trck from source
		rtc::scoped_refptr<webrtc::VideoTrackInterface> RTSourceTrack(this->PeerConnectionFactory->CreateVideoTrack("ClientCameraView", this->RendertargetTrackSource));

		auto TransceiverConfig = webrtc::RtpTransceiverInit();
		{
			TransceiverConfig.stream_ids = { "kRTStream" };
			TransceiverConfig.direction = webrtc::RtpTransceiverDirection::kSendRecv;
			TransceiverConfig.send_encodings = {};
		}

		auto result_or_error = this->PeerConnection->AddTransceiver(RTSourceTrack, TransceiverConfig);
		//auto result_or_error = this->PeerConnection->AddTrack(RTSourceTrack, {"kRTStream"});
		if (!result_or_error.ok()) 
		{
			RTC_LOG(INFO) << "Failed to add video track to PeerConnection: " << result_or_error.error().message();
			return false;
		}
		else
		{
			RTC_LOG(INFO) << "RT video track added: " << ((RTSourceTrack->state() == webrtc::MediaStreamTrackInterface::kLive) ? "live" : "not live");
		}
    }

	return true;
}

bool Conductor::SetRenderTargetFrameProperties(int width, int height)
{
	RTC_LOG(INFO) << "Conductor::SetRenderTargetFrameProperties(" << width << ", " << height << ") called.";

	this->SourceFrameWidth = width;
	this->SourceFrameHeight = height;
	this->SourceFrameBufferSize = this->SourceFrameWidth * this->SourceFrameHeight;
	this->HalfSourceFrameWidth = this->SourceFrameWidth >> 1;
	this->HalfSourceFrameHeight = this->SourceFrameHeight >> 1;
	this->HalfSourceFrameSize = this->HalfSourceFrameWidth * this->HalfSourceFrameHeight;

	// re-create frame buffer to store frame data
	this->FrameBuffer = webrtc::I420Buffer::Create(width, height);
	return true;
}

bool Conductor::SendRenderTargetData(const uint8_t* YData, const uint8_t* UData, const uint8_t* VData)
{
	static int64_t frameId { 0 };
	
	if(YData == nullptr || UData == nullptr || VData == nullptr)
		return false;

	memcpy(this->FrameBuffer->MutableDataY(), YData, this->SourceFrameBufferSize);
	memcpy(this->FrameBuffer->MutableDataU(), UData, this->HalfSourceFrameSize);
	memcpy(this->FrameBuffer->MutableDataV(), VData, this->HalfSourceFrameSize);

	// create frame
	webrtc::VideoFrame frame =
		webrtc::VideoFrame::Builder()
		.set_video_frame_buffer(this->FrameBuffer)
		.set_rotation(webrtc::VideoRotation::kVideoRotation_0)
		.set_timestamp_us(rtc::TimeMicros())
		.set_id(frameId++)
		.build();

	// send frame
	this->RendertargetTrackSource->SendFrame(frame);
	return true;
}

bool Conductor::CreateDataChannel() 
{
	RTC_LOG(INFO) << "Conductor::CreateDataChannel() called.";

	webrtc::DataChannelInit init;
	{
	  init.ordered = true;
	  init.reliable = true;
	}
	
	this->DataChannel = PeerConnection->CreateDataChannel("DataChannel", &init);
	if(this->DataChannel.get()) 
	{
		this->DataChannel->RegisterObserver(this);
		RTC_LOG(LS_INFO) << "Succeeds to create data channel";
		return true;
	} 
	else 
	{
	  RTC_LOG(LS_INFO) << "Fails to create data channel";
	  return false;
	}
}

void Conductor::CloseDataChannel() 
{
	RTC_LOG(INFO) << "Conductor::CloseDataChannel() called.";

	if (this->DataChannel.get()) 
	{
		this->DataChannel->UnregisterObserver();
		this->DataChannel->Close();
	}
	this->DataChannel = nullptr;
}

bool Conductor::SendData(const std::string& data) 
{
	if (!this->DataChannel.get()) 
	{
		RTC_LOG(LS_INFO) << "Data channel is not established";
		return false;
	}

	DataChannel->Send(webrtc::DataBuffer(data));
	return true;
}

bool Conductor::AddNewIceCandidate(const char* candidate, const int sdp_mlineindex, const char* sdp_mid) 
{	
	RTC_LOG(INFO) << "Conductor::AddNewIceCandidate(" << candidate << ", " << sdp_mlineindex << ", " << sdp_mid << ") called.";

	if (!this->PeerConnection)
		return false;

	webrtc::SdpParseError error;
	std::unique_ptr<webrtc::IceCandidateInterface> ice_candidate(webrtc::CreateIceCandidate(sdp_mid, sdp_mlineindex, candidate, &error));
	if (!ice_candidate.get()) 
	{
		RTC_LOG(WARNING) << "Can't parse received candidate message. " << "SdpParseError was: " << error.description;
		return false;
	}
	if (!this->PeerConnection->AddIceCandidate(ice_candidate.get())) 
	{
		RTC_LOG(WARNING) << "Failed to apply the received candidate";
		return false;
	}

	return true;
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// REGISTER CALLBACKS
///////////////////////////////////////////////////////////////////////////////////////////////////

void Conductor::RegisterOnLocalDataChannelReady(LOCALDATACHANNELREADY_CALLBACK callback) { OnLocalDataChannelReady = callback; }
void Conductor::RegisterOnDataFromDataChannelReady(DATAFROMEDATECHANNELREADY_CALLBACK callback) { OnDataFromDataChannelReady = callback; }
void Conductor::RegisterOnFailure(FAILURE_CALLBACK callback) { OnFailureMessage = callback; }
void Conductor::RegisterOnLocalSdpReadytoSend(LOCALSDPREADYTOSEND_CALLBACK callback) { OnLocalSdpReady = callback; }
void Conductor::RegisterLogCallback(LOG_CALLBACK callback) 
{ 
#ifdef USE_LOGGING
	if(callback != nullptr) { this->Logger = new LogStream(callback); } 
#endif // USE_LOGGING
}

///////////////////////////////////////////////////////////////////////////////////////////////////
// EVENT HANDLER
///////////////////////////////////////////////////////////////////////////////////////////////////

void Conductor::OnSuccess(webrtc::SessionDescriptionInterface* desc) 
{
	std::string sdp;
	desc->ToString(&sdp);

	RTC_LOG(INFO) << "Conductor::OnSuccess() called. sdp: " << sdp << ", type: " << desc->type();

	PeerConnection->SetLocalDescription(DummySetSessionDescriptionObserver::Create(), desc);

	if(OnLocalSdpReady)
	{
		OnLocalSdpReady(desc->type().c_str(), sdp.c_str());
	}
}

void Conductor::OnFailure(webrtc::RTCError error) 
{
  if (OnFailureMessage)
    OnFailureMessage(error.message());
}

void Conductor::OnIceCandidate(const webrtc::IceCandidateInterface* candidate) 
{
	RTC_LOG(INFO) << "Conductor::OnSuccess(" << candidate << ") called";
	PeerConnection->AddIceCandidate(candidate);
}

void Conductor::OnAddStream(rtc::scoped_refptr<webrtc::MediaStreamInterface> stream) 
{
	RTC_LOG(INFO) << "Conductor::OnAddStream() called. Stream-Id: " << stream->id();
}

// Peerconnection observer
void Conductor::OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel) 
{
  channel->RegisterObserver(this);
}

void Conductor::OnStateChange() 
{
	RTC_LOG(INFO) << "Conductor::OnStateChange() called.";
	if (DataChannel) 
	{
		webrtc::DataChannelInterface::DataState state = DataChannel->state();
		if (state == webrtc::DataChannelInterface::kOpen) 
		{
			RTC_LOG(LS_INFO) << "Data channel is open";
			if(OnLocalDataChannelReady)
				OnLocalDataChannelReady();
		}
	}
}

//  A data buffer was successfully received.
void Conductor::OnMessage(const webrtc::DataBuffer& buffer) 
{
	size_t size = buffer.data.size();

	// if incoming data is small enough use pre-created buffer
	if(size < DATA_BUFFER_SIZE - 1)
	{
		// get message
		memcpy(this->DataBuffer, buffer.data.data(), size);
		this->DataBuffer[size] = 0;

		// notify app 
		if (OnDataFromDataChannelReady)
			OnDataFromDataChannelReady(this->DataBuffer);
	}
	// otherwise create a new temporary buffer that can hold all data
	else
	{
		char* tempBigBuffer = new char[size + 1];
		{
			memcpy(tempBigBuffer, buffer.data.data(), size);
			tempBigBuffer[size] = 0;

			if(OnDataFromDataChannelReady)
				OnDataFromDataChannelReady(tempBigBuffer);
		}
		delete[] tempBigBuffer;
	}
}