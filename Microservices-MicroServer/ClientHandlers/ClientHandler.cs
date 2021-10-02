using System;
using System.Net;
using System.Threading;

namespace Microservices_MicroServer {
    static partial class ClientHandler {

        public static void HandleClient(object contextObj) {
            HttpListenerContext context = (HttpListenerContext)contextObj;

            HttpListenerRequest request = context.Request;

            if (request.HttpMethod == "GET" && Utils.IsJobGet(request.Url.Segments)) {
#if MicroServer_DebugEdition
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [INFO] New GET request: " + request.RequestTraceIdentifier.ToString());
#endif
                HandleGET(context);
            } else if (request.HttpMethod == "POST" && Utils.IsJobPost(request.Url.Segments)) {
#if MicroServer_DebugEdition
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [INFO] New POST request");
#endif
                HandlePOST(context);
            } else {
#if MicroServer_DebugEdition
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [ERROR] New UNKNOWN request (will be rejected): " + request.RequestTraceIdentifier.ToString());
#endif
                HttpListenerResponse resp = context.Response;
                resp.StatusCode = 417;
                resp.Close();
            }
            
        }

        public static void WakeUpStuckGETs() {
            while (true) {
                Thread.Sleep(27000);
                Program.threadManagerGET.SetEvent();
            }
        }

    }
}
