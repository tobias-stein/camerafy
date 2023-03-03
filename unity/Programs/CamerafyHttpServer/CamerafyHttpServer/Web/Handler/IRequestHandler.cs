using System.Net;

namespace CamerafyHttpServer.Web
{
    /// <summary>
    /// Implemented by a specific request handler.
    /// </summary>
    public interface IRequestHandler
    {
        void HandleRequest(HttpServer Server, HttpListenerContext context);
    }
}
