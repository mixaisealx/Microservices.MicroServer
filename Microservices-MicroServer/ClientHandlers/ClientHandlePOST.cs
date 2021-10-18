using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;


namespace Microservices_MicroServer {
    static partial class ClientHandler {

        static void HandlePOST(HttpListenerContext context) {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse resp = context.Response;

            if (request.ContentType != null && request.ContentType.ToLower().Contains("application/json")) {
                string content;
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8)) {
                    content = reader.ReadToEnd();
                }

                try {
                    BasicContent bcontent = JsonSerializer.Deserialize<BasicContent>(content, Constants.JSON_SERIALIZER_OPTIONS);

                    Utils.FixNullInBasicContent(ref bcontent);

                    if (bcontent.type != "null" || bcontent.visibleId) {
                        if (!Constants.SPECIAL_TYPES.Any(x => x == bcontent.type)) {
                            ContentStorage.PushContent(bcontent);
                            ThreadManagerGET.SetEvent();
                            resp.StatusCode = 201;
                        } else if (bcontent.type == "MicroServer.25367be645.CompensateUnderflow") {
                            try {
                                BasicContent[] compensation = JsonSerializer.Deserialize<BasicContent[]>(((JsonElement)bcontent.content).GetRawText(), Constants.JSON_SERIALIZER_OPTIONS);
                                if (compensation.Length != 0 && Constants.SPECIAL_TYPES.All(x => x != compensation[0].type)) {
                                    resp.StatusCode = ContentStorage.СompensateUnderflow(compensation.Select(x => Utils.FixNullInBasicContent(x))) ? 200 : 409;
                                    ThreadManagerGET.SetEvent();
                                } else {
                                    resp.StatusCode = 400;
                                }
                            } catch {
                                resp.StatusCode = 415;
                            }
                        } else {
                            resp.StatusCode = 405;
                        }
                    } else {
                        resp.StatusCode = 409;
                    }
                } catch {
                    resp.StatusCode = 400;
                }
            } else {
                resp.StatusCode = 412;
            }
            resp.Close();
        }

    }
}
