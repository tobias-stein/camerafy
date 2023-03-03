using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Camerafy.Web
{
    public enum MessagingProtocol
    {
        stomp
    }

    /// <summary>
    /// Web transfer protocols.
    /// </summary>
    public enum TransferProtocol
    {
        /// <summary>
        /// Hypertext transfer protocol.
        /// </summary>
        http,

        /// <summary>
        /// Hypertext transfer protocol secure.
        /// </summary>
        https,

        /// <summary>
        /// Transmission control protocol.
        /// </summary>
        tcp,

        /// <summary>
        /// Web socket.
        /// </summary>
        ws
    }

    /// <summary>
    /// A list of all valid session commands, that can be send over to peers.
    /// </summary>
    public enum SessionCommandType
    {
        /// <summary>
        /// Default for unknown commands.
        /// </summary>
        UnknownCmd,

        /// <summary>
        /// A command send to client to signal readiness to start webrtc call.
        /// </summary>
        ServerRTCReady,

        /// <summary>
        /// Command received from client to signal server readiness to start webrtc call.
        /// </summary>
        ClientRTCReady,

        /// <summary>
        /// 
        /// </summary>
        OfferCmd,

        /// <summary>
        ///
        /// </summary>
        AnswerCmd,

        /// <summary>
        /// A command send by session before termination.
        /// </summary>
        SessionTerminatedCmd,

        /// <summary>
        /// A command received from clients when they are disconnecting from session.
        /// </summary>
        DisconnectCmd,

        /// <summary>
        /// A command received from clients when they send their discovered ice candidates.
        /// </summary>
        NewIceCandidateCmd,
    }

    /// <summary>
    /// Wrapper class to define the overall structure of the json string, which is going
    /// to be serialized by the json parser.
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// The command.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SessionCommandType command { get; set; } = SessionCommandType.UnknownCmd;

        /// <summary>
        /// The actual data.
        /// </summary>
        public object data { get; set; } = null;

        /// <summary>
        /// This method constructs a proxy object with a certain command and data and serializes
        /// it to a json string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string AsJson<T>(SessionCommandType command, T data = null) where T : class
        {
            return JsonConvert.SerializeObject(new SessionData { command = command, data = data });
        }

        /// <summary>
        /// Same as the generic counterpart, but for string data.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string AsJson(SessionCommandType command, string data = null)
        {
            return AsJson<string>(command, data);
        }
    }
}
