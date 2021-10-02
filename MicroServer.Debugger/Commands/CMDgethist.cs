using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net;


namespace MicroServer.Debugger {
    static partial class CMDs {

#pragma warning disable 0649
        struct GetHistory {
            public DateTime datetime;
            public string requestedType, requestedId;
            public BasicContent content;
        }
#pragma warning restore 0649

        internal static void gethist() {
            HttpResponseMessage response;
            {
                var task = Program.clientHttp.GetAsync(Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retriveGetHistory");
                try {
                    task.Wait();
                    response = task.Result;
                } catch {
                    Console.WriteLine("ERROR: throw " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retriveGetHistory");
                    return;
                }
            }

            if (!response.IsSuccessStatusCode) {
                if (response.StatusCode == HttpStatusCode.MethodNotAllowed) {
                    Console.WriteLine($"ERROR: You are using a server build with communication debugging features disabled. To use these commands, you need to use the \"Debug Edition\" build.");
                } else {
                    Console.WriteLine($"ERROR: {response.StatusCode} " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retriveGetHistory");
                }
                return;
            }

            string content;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                content = reader.ReadToEnd();
            }

            try {
                GetHistory[] received = JsonSerializer.Deserialize<GetHistory[]>(content, Constants.JSON_SERIZLIZER_OPTIONS);
                if (received.Length == 0) {
                    Console.WriteLine("<nothing to show>");
                } else {
                    foreach (var item in received) {
                        Console.WriteLine(item.datetime.ToString("HH:mm:ss.f") + 
                            " Requested type" + Utils.GetPrintableString(item.requestedType) + 
                            "; Requested id" + Utils.GetPrintableString(item.requestedId) + 
                            "; Found type" + (item.requestedType == item.content.type ? " [SAME]" : Utils.GetPrintableString(item.content.type)) + 
                            "; Found " + (item.content.visibleId ? "visible-" : "hidden-") + "id" + (item.requestedId == item.content.id ? " [SAME]" : Utils.GetPrintableString(item.content.id)) + 
                            "; Content: " + Utils.GetPrintableContent(item.content.content) + '\n');
                    }
                }
                StreamWriter file = new("gethist.log", true, Encoding.UTF8);
                file.WriteLine(content);
                file.WriteLine("\\____/");
                file.Close();
                ++flush.gethist;
            } catch (Exception) {
                Console.WriteLine("ERROR: throw " + content);
            }
        }
    }
}
