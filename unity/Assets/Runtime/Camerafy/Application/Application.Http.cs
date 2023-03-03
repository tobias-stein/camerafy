using System;
using System.Net;
using System.Threading;

namespace Camerafy.Application
{
    public partial class Application
    {
        private HttpListener HttpListener = null;

        private Thread HttpListenerThread = null;

        private void StartHttpListener()
        {
            // check if feature is supported
            if (!HttpListener.IsSupported)
            {
                Logger.Error("HttpListener not supported on this system.");
                return;
            }

            // create the listener
            this.HttpListener = new HttpListener();
            this.HttpListener.AuthenticationSchemes = AuthenticationSchemes.None;
            this.HttpListener.Prefixes.Add($"http://*:{this.Config.CamerafyHttpListenerPort}/");
            
            // start listener thread
            this.HttpListenerThread = new Thread(UpdateHttpListener);
            this.HttpListenerThread.Start();
        }

        private void StopHttpListener()
        {
            if (this.HttpListener != null)
            {
                this.HttpListener.Stop();
            }

            if (this.HttpListenerThread != null)
            {
                this.HttpListenerThread.Abort();
            }
        }

        private void UpdateHttpListener()
        {
            // start listening...
            Logger.Debug($"Start http listener thread on port {this.Config.CamerafyHttpListenerPort}...");
            this.HttpListener.Start();
           
            try
            {
                // limit listener to only serve a maximum of concurrent requests.
                const int MaxConcurrentRequests = 4;
                var Sem = new Semaphore(MaxConcurrentRequests, MaxConcurrentRequests);

                while (this.HttpListener.IsListening)
                {
                    Sem.WaitOne();
                    this.HttpListener.GetContextAsync().ContinueWith(async Ctx =>
                    {
                        try
                        {
                            this.ProcessHttpRequest(await Ctx);
                            Sem.Release();
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    });
                }
            }
            catch (ThreadAbortException e)
            {
                // thread was terminated.
                Logger.Debug("Terminate http-listener thread...");
            }
        }

        private void ProcessHttpRequest(HttpListenerContext Ctx)
        {
            var REQ = Ctx.Request;
            var RES = Ctx.Response;

            RES.StatusCode = (int)HttpStatusCode.OK;

            // send response.
            RES.Close();
        }
    }
}
