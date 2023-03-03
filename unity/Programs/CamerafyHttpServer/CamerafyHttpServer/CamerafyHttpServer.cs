
using System.Collections.Generic;

namespace CamerafyHttpServer
{
    /// <summary>
    /// Runs
    /// </summary>
    class CamerafyHttpServer
    {
        static void Main(string[] args)
        {
            System.Console.Out.WriteLine("CamerafyHttpServer.exe run with args: \"{0}\"", string.Join(" ", args));

            if (args.Length < 3)
            {
                System.Console.Out.WriteLine("CamerafyHttpServer run with insufficient arguments. Run: CamerafyHttpServer.exe <AbsoluteWebContentPath> <Port> <CamerafyServiceAddress> [-Secure]");
                return;
            }

            string WebContent = args[0];
            int Port = int.Parse(args[1]);
            string CamerafyServiceAddress = args[2];

            List<string> CmdLine = new List<string>(args);
            
            bool Secure = CmdLine.Contains("-secure");
            bool Login = CmdLine.Contains("-login");

            // create new server
            Web.HttpServer Server = new Web.HttpServer(WebContent, Port, CamerafyServiceAddress, Secure, Login);

            // terminate server if process will be terminated.
            System.AppDomain.CurrentDomain.ProcessExit += (sender, e) => Server.TerminateServer();

            // run until parent process kills us
            while (Server.IsListening) { System.Threading.Thread.Sleep(100); }

            System.Console.Out.WriteLine("CamerafyHttpServer.exe is quiting.");
        }
    }
}
