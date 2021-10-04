using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.Json;

namespace Microservices_MicroServer {
    static partial class ClientHandler {

        static void HandleGET(HttpListenerContext context) {
            DateTime start_time = DateTime.UtcNow;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            var headers = request.QueryString;
            if (headers.Count != 0 && headers.AllKeys.All(x => x == "id" || x == "type")) {
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "application/json; charset=utf-8";

                string type = headers.Get("type") ?? "null";
                string id = headers.Get("id") ?? "null";
                
                if (Constants.SPECIAL_TYPES.All(x => x != type)) { //If standart request (not from specials)
                    BasicContent content = ContentStorage.PopContent(type, id);

                    if (content == null) {
#if MicroServer_DebugEdition
                        var ptoken = ContentStorage.debug_AddPending(type, id);
#endif
                        ThreadManagerGET.RegisterPending(request.RequestTraceIdentifier);
                        do {
                            Thread.Yield(); //Ask OS for executing another thread (not this)
                            Thread.Sleep(0);

                            ThreadManagerGET.WaitForEvent();
                            if ((DateTime.UtcNow - start_time).TotalSeconds < 25) {
                                content = ContentStorage.PopContent(type, id);
                            } else {
                                content = new BasicContent("MicroServer.25367be645.GET_TIMEOUT_25"); //Needed for timeout processing
                            }

                            ThreadManagerGET.RequiredExecuted(request.RequestTraceIdentifier);
                        } while (content == null);
                        ThreadManagerGET.UnregisterPending(request.RequestTraceIdentifier);
#if MicroServer_DebugEdition
                        ContentStorage.debug_RemovePending(ptoken);
#endif
                    }

                    if (content.type != "MicroServer.25367be645.GET_TIMEOUT_25") {
                        byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(content, Constants.JSON_SERIZLIZER_OPTIONS);
                        response.ContentLength64 = buffer.Length;

                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();

                        response.StatusCode = 200;
                    } else {
#if MicroServer_DebugEdition
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [WARNING] GET timeout after 25 seconds: " + request.RequestTraceIdentifier.ToString());
#endif
                        try {
                            response.StatusCode = 408;
                            response.Close();
                        } catch { }

                        return; //Exclusive ending
                    }
                } else { //Request from specials
                    byte[] buffer = null;
                    switch (type) {
                        case "MicroServer.25367be645.ExternalStatus": {
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.GetExternalStatus(), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                        }
                        break;
                        case "MicroServer.25367be645.FetchOverflow": {
                            IEnumerable<BasicContent> arrOvf = ContentStorage.FetchOverflow(id); //ID like a type
                            if (arrOvf != null) {
                                buffer = JsonSerializer.SerializeToUtf8Bytes(arrOvf, Constants.JSON_SERIZLIZER_OPTIONS);
                                response.StatusCode = 200;
                            } else {
                                response.StatusCode = 404;
                            }
                        }
                        break;
#if MicroServer_DebugEdition
                        case "MicroServer.25367be645.DebugEdition.getInternalStorageSnapshot":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.debug_InternalStorageSnapshot(), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                            break;
                        case "MicroServer.25367be645.DebugEdition.getLocallyAvailibleTypes":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.debug_GetLocallyStoredTypes(), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                            break;
                        case "MicroServer.25367be645.DebugEdition.getTypesStatistic":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, uint>(ContentStorage.debug_GetTypeStatistic()), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                            break;
                        case "MicroServer.25367be645.DebugEdition.retrivePostHistory":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.debug_RetrivePostHistory(), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                            break;
                        case "MicroServer.25367be645.DebugEdition.retriveGetHistory":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.debug_RetriveGetHistory(), Constants.JSON_SERIZLIZER_OPTIONS);
                            response.StatusCode = 200;
                            break;
                        case "MicroServer.25367be645.DebugEdition.getPendings":
                            buffer = JsonSerializer.SerializeToUtf8Bytes(ContentStorage.debug_GetPendings(), Constants.JSON_SERIZLIZER_OPTIONS);

                            response.StatusCode = 200;
                            break;
#endif
                        default:
#if MicroServer_DebugEdition
                            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " [ERROR] Not allowed special GET command");
#endif
                            response.StatusCode = 405;
                            break;
                    }
                    if (response.StatusCode == 200) {
                        Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);
                        output.Close();
                    }
                }
            } else {
                response.StatusCode = 400;
            }
            response.Close();
        }

    }
}