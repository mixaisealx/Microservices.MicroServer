using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net;


namespace MicroServer.Debugger {
    static partial class CMDs {

#pragma warning disable 0649
        struct PostHistory {
            public DateTime datetime;
            public BasicContent content;
        }
#pragma warning restore 0649

        internal static void posthist() {
            HttpResponseMessage response;
            {
                var task = Program.clientHttp.GetAsync(Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retrivePostHistory");
                try {
                    task.Wait();
                    response = task.Result;
                } catch {
                    Console.WriteLine("ERROR: throw " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retrivePostHistory");
                    return;
                }
            }

            if (!response.IsSuccessStatusCode) {
                if (response.StatusCode == HttpStatusCode.MethodNotAllowed) {
                    Console.WriteLine($"ERROR: You are using a server build with communication debugging features disabled. To use these commands, you need to use the \"Debug Edition\" build.");
                } else {
                    Console.WriteLine($"ERROR: {response.StatusCode} " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.retrivePostHistory");
                }
                return;
            }

            string content;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                content = reader.ReadToEnd();
            }

            try {
                PostHistory[] received = JsonSerializer.Deserialize<PostHistory[]>(content, Constants.JSON_SERIZLIZER_OPTIONS);
                if (received.Length == 0) {
                    Console.WriteLine("<nothing to show>");
                } else {
                    foreach (var item in received) {
                        Console.WriteLine(item.datetime.ToString("HH:mm:ss.f") + 
                            " Type" + Utils.GetPrintableString(item.content.type) + 
                            "; " + (item.content.visibleId ? "visible" : "hidden") + "-id" + Utils.GetPrintableString(item.content.id) + 
                            "; Content: " + Utils.GetPrintableContent(item.content.content) + '\n');
                    }
                }
                StreamWriter file = new("posthist.log", true, Encoding.UTF8);
                file.WriteLine(content);
                file.WriteLine("\\____/");
                file.Close();
                ++flush.posthist;
            } catch (Exception) {
                Console.WriteLine("ERROR: throw " + content);
            }
        }
    }
}
