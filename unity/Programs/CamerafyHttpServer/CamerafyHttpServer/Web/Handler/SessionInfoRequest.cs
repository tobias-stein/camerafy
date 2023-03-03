using System.Net;
using System.Text;
using System;

namespace CamerafyHttpServer.Web
{
    [Endpoint("/sessioninfo")]
    public class SessionInfoRequest : IRequestHandler
    {
        public void HandleRequest(HttpServer Server, HttpListenerContext context)
        {
            if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                string stats = Server.CamerafyService.GetActiveSessionStats();
                string ResponseMessage = string.Format("<html><body>{0}</body></html>", stats.Replace("\n", "<br/>"));
                byte[] data = Encoding.ASCII.GetBytes(ResponseMessage);
                context.Response.OutputStream.Write(data, 0, data.Length);
            }
            else
            {
                // only post and get requests are served
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            // finalize response
            context.Response.Close();
        }
    }
}
