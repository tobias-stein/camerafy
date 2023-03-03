
using System;

namespace CamerafyHttpServer.Web
{
    /// <summary>
    /// This attribute will be used in conjunction with the IRequestHandler interface
    /// and provides an endpoint specification.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class EndpointAttribute : Attribute
    {
        public string Endpoint { get; set; }

        public EndpointAttribute(string endpoint)
        {
            this.Endpoint = endpoint;
        }
    }
}
