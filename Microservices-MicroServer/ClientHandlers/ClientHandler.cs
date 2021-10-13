using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;


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
                ThreadManagerGET.SetEvent();
            }
        }

        static class ThreadManagerGET {
            static bool ready_to_count = false;
            static ManualResetEventSlim event_get = new(false);
            static Dictionary<Guid, ushort> pendings = new();

            static Mutex pending_mtx = new(); //ReadWriteLock is not need because of there only write access is needed (in RWlock behaviour of WriteLock is same as the behaviour of common mutex)

            public static void WaitForEvent() {
                event_get.Wait();
            }

            public static void SetEvent() {
                ready_to_count = true;
                event_get.Set();
            }

            /// <summary>
            /// </summary>
            /// <param name="identifer"></param>
            /// <returns>GET-request thread manager identifing token</returns>
            public static void RegisterPending(Guid identifer) {
                pending_mtx.WaitOne();
                pendings.Add(identifer, 0);
                pending_mtx.ReleaseMutex();
            }

            public static void UnregisterPending(Guid identifer) {
                pending_mtx.WaitOne();
                pendings.Remove(identifer);
                pending_mtx.ReleaseMutex();
            }

            public static void RequiredExecuted(Guid identifer) {
                pending_mtx.WaitOne();
                if (ready_to_count) {
                    ++pendings[identifer];
                    if (pendings.All(x => x.Value != 0)) {
                        event_get.Reset();
                        ready_to_count = false;
                        foreach (var item in pendings.Keys) {
                            pendings[item] = 0;
                        }
                    }
                }
                pending_mtx.ReleaseMutex();
            }
        }

    }
}
