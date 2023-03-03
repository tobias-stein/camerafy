using System;
using System.Net;

namespace CamerafyHttpServer.Web.Handler
{
    class AbstractRequestHandler : IRequestHandler
    {
        public void HandleRequest(HttpServer Server, HttpListenerContext context)
        {
            if (String.Equals(context.Request.HttpMethod, "GET")) { Get(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "POST")) { Post(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "PUT")) { Put(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "DELETE")) { Delete(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "OPTIONS")) { Options(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "HEAD")) { Head(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "CONNECT")) { Connect(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "TRACE")) { Trace(ref context); }
            else if (String.Equals(context.Request.HttpMethod, "PATCH")) { Patch(ref context); }
            else { throw new Exception(string.Format("Unkown HTTP method '{0}'.", context.Request.HttpMethod)); }

            // finalize response.
            context.Response.Close();
        }

        public virtual void Get(ref HttpListenerContext context) { throw new Exception("GET method is not supported!"); }
        public virtual void Post(ref HttpListenerContext context) { throw new Exception("POST method is not supported!"); }
        public virtual void Put(ref HttpListenerContext context) { throw new Exception("PUT method is not supported!"); }
        public virtual void Delete(ref HttpListenerContext context) { throw new Exception("DELETE method is not supported!"); }
        public virtual void Options(ref HttpListenerContext context) { throw new Exception("OPTIONS method is not supported!"); }
        public virtual void Connect(ref HttpListenerContext context) { throw new Exception("CONNECT method is not supported!"); }
        public virtual void Patch(ref HttpListenerContext context) { throw new Exception("PATCH method is not supported!"); }
        public virtual void Trace(ref HttpListenerContext context) { throw new Exception("TRACE method is not supported!"); }
        public virtual void Head(ref HttpListenerContext context) { throw new Exception("HEAD method is not supported!"); }
    }
}
