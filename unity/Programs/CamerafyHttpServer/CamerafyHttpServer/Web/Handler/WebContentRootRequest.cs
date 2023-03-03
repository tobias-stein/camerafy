using System.Net;
using System.Text;
using System;
using System.IO;

namespace CamerafyHttpServer.Web
{
    [Endpoint("/")]
    public class WebContentRootRequest : IRequestHandler
    {

        public void HandleRequest(HttpServer Server, HttpListenerContext context)
        {
            if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                context.Response.Redirect(Server.Login ? "/login" : "/live");
            }
            else
            {
                // only get requests are served
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            // finalize response
            context.Response.Close();
        }
    }
}
