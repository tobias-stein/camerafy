using System.Net;
using System.Text;
using System;
using System.IO;

namespace CamerafyHttpServer.Web
{
    [Endpoint("/live")]
    public class LiveRequest : IRequestHandler
    {
        private const string LiveHtmlPage = "live.html";

        public void HandleRequest(HttpServer Server, HttpListenerContext context)
        {
            if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Server.Login)
                {
                    string userAccessToken = null;
                    int privilegeLevel = 400; // todo: determine users privelege level

                    // this cookie is granted by the LoginRequest on successfull authentication.
                    try
                    {
                        userAccessToken = context.Request.Cookies["ActiveUserToken"].Value;
                    }
                    catch (Exception e)
                    {
                        System.Console.Out.WriteLine(e);
                    }

                    if (Server.CamerafyService.ClaimUserToken(userAccessToken))
                    {
                        // create a new session user
                        Server.CamerafyService.CreateNewSessionUser(userAccessToken, privilegeLevel);

                        // serve the page
                        ServeLivePage(Server, context);
                    }
                    // if request was made with invalid or expired token, do not authorize user to see live page
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                }
                // No login required
                else
                {
                    if (!Server.CamerafyService.IsNewUserConnectionsAllowed())
                    {
                        byte[] ResponseMessage = UTF8Encoding.UTF8.GetBytes("<body>Session full. Try again later.</body>");
                        context.Response.OutputStream.Write(ResponseMessage, 0, ResponseMessage.Length);
                        context.Response.Close();
                        return;
                    }
                    else
                    {
                        // simply claim new user token and create new user
                        string token = Server.CamerafyService.GenerateNewUserToken();
                        Server.CamerafyService.ClaimUserToken(token);
                        Server.CamerafyService.CreateNewSessionUser(token, 400);

                        // give the user an authentication token to access further content
                        Cookie ActiveUserTokenCookie = new Cookie
                        {
                            Name = "ActiveUserToken",
                            Value = token,
                            Secure = Server.Secure,
                        };
                        context.Response.Headers.Add(HttpResponseHeader.SetCookie, ActiveUserTokenCookie.ToString());

                        ServeLivePage(Server, context);
                    }
                }
            }
            else
            {
                // only get requests are served
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            // finalize response
            context.Response.Close();
        }

        private void ServeLivePage(HttpServer Server, HttpListenerContext context)
        {
            try
            {
                context.Response.ContentType = "text/html";
                context.Response.ContentEncoding = Encoding.UTF8;

                // give the user the active session cookie
                Cookie ActiveSessionTokenCookie = new Cookie
                {
                    Name = "ActiveSessionToken",
                    Value = Server.CamerafyService.GetActiveSessionId(),
                    Secure = Server.Secure
                };

                // give the user the url to signaling server
                Cookie SignalingServerUrlCookie = new Cookie
                {
                    Name = "SignalingServerUrl",
                    Value = Server.CamerafyService.GetSignalingServerUrl(),
                    Secure = Server.Secure
                };

                // give the user the list of ice server candidates
                Cookie IceServersCookie = new Cookie
                {
                    Name = "IceServers",
                    Value = Server.CamerafyService.GetIceServers(),
                    Secure = Server.Secure
                };

                context.Response.Headers.Add(HttpResponseHeader.SetCookie, SignalingServerUrlCookie.ToString());
                context.Response.Headers.Add(HttpResponseHeader.SetCookie, ActiveSessionTokenCookie.ToString());
                context.Response.Headers.Add(HttpResponseHeader.SetCookie, IceServersCookie.ToString());

                // transfer Login.html file
                using (FileStream fs = new FileStream(Path.Combine(Server.WebContentHomePath, LiveHtmlPage), FileMode.Open))
                    fs.CopyTo(context.Response.OutputStream);
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine(e);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }
}
