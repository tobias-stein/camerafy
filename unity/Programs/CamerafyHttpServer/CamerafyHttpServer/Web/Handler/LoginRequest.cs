using System.Net;
using System.Text;
using System;
using System.IO;
using System.Collections.Specialized;

namespace CamerafyHttpServer.Web
{
    [Endpoint("/login")]
    public class LoginRequest : IRequestHandler
    {
        private const string LoginHtmlPage = "login.html";

        public void HandleRequest(HttpServer Server, HttpListenerContext context)
        {
            if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Server.Login)
                {
                    ServeLoginPage(Server, context);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                    context.Response.Redirect( "/live");
                }
            }
            else if (String.Equals(context.Request.HttpMethod, "POST", StringComparison.CurrentCultureIgnoreCase))
            {
                AuthenticateUser(Server, context);
            }
            else
            {
                // only post and get requests are served
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            // finalize response
            context.Response.Close();
        }

        private void ServeLoginPage(HttpServer Server, HttpListenerContext context)
        {
            try
            {
                context.Response.ContentType = "text/html";
                context.Response.ContentEncoding = Encoding.UTF8;

                // transfer Login.html file
                using (FileStream fs = new FileStream(Path.Combine(Server.WebContentHomePath, LoginHtmlPage), FileMode.Open))
                    fs.CopyTo(context.Response.OutputStream);
            }
            catch (Exception e)
            {
                System.Console.Out.WriteLine(e);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }

        private void AuthenticateUser(HttpServer Server, HttpListenerContext context)
        {
            if (!Server.CamerafyService.IsNewUserConnectionsAllowed())
            {
                context.Response.OutputStream.WriteByte((byte)('2'));
                context.Response.Close();
                return;
            }

            try
            {
                // get request content
                string content = null;
                using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    content = reader.ReadToEnd();

                // parse post data
                NameValueCollection postData = System.Web.HttpUtility.ParseQueryString(content);

                // get username and password
                string username = postData.Get("username");
                string password = postData.Get("password");

                // do authentication stuff
                bool HasValidCredentials = true;//password == "123"; // todo: implement authorization interface

                if (HasValidCredentials)
                {
                    // give the user an authentication token to access further content
                    Cookie ActiveUserTokenCookie = new Cookie
                    {
                        Name = "ActiveUserToken",
                        Value = Server.CamerafyService.GenerateNewUserToken(),
                        Secure = Server.Secure,
                    };
                    context.Response.Headers.Add(HttpResponseHeader.SetCookie, ActiveUserTokenCookie.ToString());
                }

                // send response
                context.Response.OutputStream.WriteByte(HasValidCredentials ? (byte)('1') : (byte)('0'));
            }
            catch (Exception e)
            {
                System.Console.Error.WriteLine(e);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }
}
