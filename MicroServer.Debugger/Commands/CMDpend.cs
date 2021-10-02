using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net;


namespace MicroServer.Debugger {
    static partial class CMDs {

#pragma warning disable 0649
        struct Pending {
            public string type, id;
        }
#pragma warning restore 0649

        internal static void pend() {
            HttpResponseMessage response;
            {
                var task = Program.clientHttp.GetAsync(Program.url_get + "?type=MicroServer.25367be645.DebugEdition.getPendings");
                try {
                    task.Wait();
                    response = task.Result;
                } catch {
                    Console.WriteLine("ERROR: throw " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.getPendings");
                    return;
                }
            }

            if (!response.IsSuccessStatusCode) {
                if (response.StatusCode == HttpStatusCode.MethodNotAllowed) {
                    Console.WriteLine($"ERROR: You are using a server build with communication debugging features disabled. To use these commands, you need to use the \"Debug Edition\" build.");
                } else {
                    Console.WriteLine($"ERROR: {response.StatusCode} " + Program.url_get + "?type=MicroServer.25367be645.DebugEdition.getPendings");
                }
                return;
            }

            string content;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                content = reader.ReadToEnd();
            }

            try {
                Pending[] received = JsonSerializer.Deserialize<Pending[]>(content, Constants.JSON_SERIZLIZER_OPTIONS);
                if (received.Length == 0) {
                    Console.WriteLine("<nothing to show>");
                } else {
                    foreach (var item in received) {
                        Console.WriteLine("Type" + Utils.GetPrintableString(item.type) + "; Id" + Utils.GetPrintableString(item.id));
                    }
                }
                StreamWriter file = new("pend.log", true, Encoding.UTF8);
                file.WriteLine(content);
                file.WriteLine("\\____/");
                file.Close();
                ++flush.pend;
            } catch (Exception) {
                Console.WriteLine("ERROR: throw " + content);
            }
        }
    }
}
