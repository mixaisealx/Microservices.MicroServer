using System;
using System.Net;
using System.Threading.Tasks;


namespace Microservices_MicroServer {
    class Program {

        static void Main(string[] args) {
            if (!HttpListener.IsSupported) {
                Console.WriteLine("Something went wrong.\nYour platform does not support the System.Net.HttpListener class.\nServer can't work.");
                return;
            }

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(Constants.PREFIX_GETJOB);
            listener.Prefixes.Add(Constants.PREFIX_POSTJOB);
            listener.Start();

            Task.Factory.StartNew(ClientHandler.WakeUpStuckGETs);

#if MicroServer_DebugEdition
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [INFO] Server started");
            Console.WriteLine("[INFO] GET-JOB URL: " + Constants.PREFIX_GETJOB);
            Console.WriteLine("[INFO] POST-JOB URL: " + Constants.PREFIX_POSTJOB);
#endif
            while (true) {
                HttpListenerContext context = listener.GetContext();
                Task.Factory.StartNew(ClientHandler.HandleClient, context);
            }
        }

    }
}
