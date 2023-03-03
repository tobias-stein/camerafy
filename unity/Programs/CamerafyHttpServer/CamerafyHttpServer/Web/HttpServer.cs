using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CamerafyHttpServer.Web
{
    public class HttpServer
    {
        /// <summary>
        /// Root path for web content.
        /// </summary>
        public string WebContentHomePath { get; private set; } = "WebContent/home";

        /// <summary>
        /// The port the server is listening to.
        /// </summary>
        public int Port { get; private set; } = 1988;

        /// <summary>
        /// USe secure connection.
        /// </summary>
        public bool Secure { get; private set; } = false;

        /// <summary>
        /// If true a user must perform an authentication through an login mask
        /// </summary>
        public bool Login { get; private set; } = false;

        public bool IsListening { get { return this.Listener != null && this.Listener.IsListening; } }

        /// <summary>
        /// Access the Camerafy (parent process) service.
        /// </summary>
        public WCFServiceContractClient CamerafyService { get; private set; }

        /// <summary>
        /// The http listener object.
        /// </summary>
        private HttpListener Listener = null;

        private Dictionary<string, IRequestHandler> RequestHandler = null;

        private string CamerafyServiceAddress = "";

        // Start is called before the first frame update
        public HttpServer(string WebContentHome, int CamerafyHttpServerPort, string CamerafyServiceAddress, bool Secure, bool Login)
        {
            // override HttpServer properties by config
            {
                this.WebContentHomePath = WebContentHome;
                this.Port = CamerafyHttpServerPort;
                this.CamerafyServiceAddress = CamerafyServiceAddress;
                this.Secure = Secure;
                this.Login = Login;
            }

            InitializeRequestHandler();
            ConnectCamerafyService();
            StartServer();
        }

        /// <summary>
        /// Gathers all http request handler from this assembly and initializes on
        /// handler instance each.
        /// </summary>
        private void InitializeRequestHandler()
        {
            System.Console.Out.WriteLine("===== BEGIN: Register Request-Handler =====");

            // create handler map
            this.RequestHandler = new Dictionary<string, IRequestHandler>();

            // IRequestHandler class type
            Type type = typeof(IRequestHandler);

            // Check entire app domain. 
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> HandlerClasses = assemblies.SelectMany(t => t.GetTypes()).Where(t => type.IsAssignableFrom(t));

            // instantiate handler class
            foreach (Type THandler in HandlerClasses)
            {
                // ignore the 'IRequestHandler' interface itself.
                if (THandler.IsInterface)
                    continue;

                List<EndpointAttribute> Endpoints = THandler.GetCustomAttributes<EndpointAttribute>().ToList();

                // skip this handler if not at least one endpoint has been specified
                if (Endpoints.Count == 0)
                {
                    System.Console.Out.WriteLine("Request handler '{0}' has no endpoint specification. Please use 'EndpointAttribute' on class to set one or many endpoints", THandler.Name);
                    continue;
                }

                try
                {
                    // create new instance of handler
                   IRequestHandler requestHandler = (IRequestHandler)Activator.CreateInstance(THandler);

                    foreach (EndpointAttribute endpoint in Endpoints)
                    {
                        if (this.RequestHandler.ContainsKey(endpoint.Endpoint))
                        {
                            System.Console.Out.WriteLine("Request handler '{0}' is trying to handle enpoint '{1}', but the endpoint is already handled by another handler. Only one handler can handle an endpoint.", THandler.Name, endpoint.Endpoint);
                            continue;
                        }

                        // add handler for this endpoint
                        this.RequestHandler.Add(endpoint.Endpoint, requestHandler);
                        System.Console.Out.WriteLine("'{0}' request handler serves endpoint '{1}'", THandler.Name, endpoint.Endpoint);
                    }
                }
                catch (Exception e)
                {
                    System.Console.Out.WriteLine(e);
                }
            }

            System.Console.Out.WriteLine("===== END: Register Request-Handler =====");
        }

        private void ConnectCamerafyService()
        {
            this.CamerafyService = new WCFServiceContractClient(new System.ServiceModel.BasicHttpBinding(), new System.ServiceModel.EndpointAddress(this.CamerafyServiceAddress));
        }

        /// <summary>
        /// Starts the http server.
        /// </summary>
        private void StartServer()
        {
            if (this.Listener != null && this.Listener.IsListening)
                // server is already running.
                return;

            // create a listener
            this.Listener = new HttpListener();

            string Endpoint = string.Format("{0}://*:{1}/", this.Secure ? "https" : "http", this.Port);
            System.Console.Out.WriteLine("Camerafy HttpServer listening at: {0}", Endpoint);
            this.Listener.Prefixes.Add(Endpoint);
            this.Listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            // start listening
            this.Listener.Start();

            // create background worker thread
            new Thread(new ThreadStart(delegate
            {
                System.Console.Out.WriteLine("HttpServer is running and listening on port '{0}' ...", this.Port);

                // process incoming requests async
                while (this.Listener.IsListening)
                {
                    try
                    {
                        // wait for request ...
                        HttpListenerContext context = this.Listener.GetContext();

                        // process request async
                        Task.Run(() => { HandleRequest(context); });
                    }
                    catch (HttpListenerException e)
                    {
                        // Error: 'Listener closed.'
                        // note: This will always happen, when HttpServer is terminated.
                        if (e.ErrorCode == 500)
                            return;
                    }
                    catch (Exception e)
                    {
                        System.Console.Error.WriteLine(e);
                    }
                }

            })).Start();
        }

        /// <summary>
        /// Terminates the http server.
        /// </summary>
        public void TerminateServer()
        {
            // terminate listener
            if (this.Listener.IsListening)
                this.Listener.Stop();

            this.Listener = null;

            System.Console.Out.WriteLine("HttpServer stopped.");

            // clear request handler map
            if(this.RequestHandler != null)
                this.RequestHandler.Clear();
            this.RequestHandler = null;
        }

        /// <summary>
        /// HttpServers request handler entry point.
        /// </summary>
        /// <param name="context"></param>
        private void HandleRequest(HttpListenerContext context)
        {
            // check if there is a handler registered for this request
            IRequestHandler handler = null;
            if (this.RequestHandler.TryGetValue(context.Request.RawUrl, out handler))
            {
                handler.HandleRequest(this, context);
            }
            // if no handler could be determined, try to serve a resource instead
            else
            {
                try
                {
                    System.Console.Out.WriteLine("Serving resource '{0}' ...", context.Request.RawUrl);
                    string resourcePath = string.Format("{0}{1}", this.WebContentHomePath, context.Request.RawUrl);

                    using (FileStream fs = new FileStream(resourcePath, FileMode.Open))
                        fs.CopyTo(context.Response.OutputStream);
                }
                catch (Exception e)
                {
                    System.Console.Error.WriteLine(e);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                finally
                {
                    // finalize response
                    context.Response.Close();
                }
            }
        }
    }
}