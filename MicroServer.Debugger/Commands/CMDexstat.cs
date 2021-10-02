using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MicroServer.Debugger {
    static partial class CMDs {

#pragma warning disable 0649
        struct ExternalStatus {
            public string type;
            public bool underflow, overflow;
        }
#pragma warning restore 0649

        internal static void exstat() {


            HttpResponseMessage response;
            {
                var task = Program.clientHttp.GetAsync(Program.url_get + "?type=MicroServer.25367be645.ExternalStatus");
                try {
                    task.Wait();
                    response = task.Result;
                } catch {
                    Console.WriteLine("ERROR: throw " + Program.url_get + "?type=MicroServer.25367be645.ExternalStatus");
                    return;
                }
            }

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine($"ERROR: {response.StatusCode} " + Program.url_get + "?type=MicroServer.25367be645.ExternalStatus");
                return;
            }

            string content;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                content = reader.ReadToEnd();
            }

            try {
                ExternalStatus[] received = JsonSerializer.Deserialize<ExternalStatus[]>(content, Constants.JSON_SERIZLIZER_OPTIONS);
                if (received.Length == 0) {
                    Console.WriteLine("<nothing to show>");
                } else {
                    foreach (var item in received) {
                        Console.WriteLine($"Type" + Utils.GetPrintableString(item.type) + $"; Overflow: {item.overflow}; Underflow: {item.underflow}");
                    }
                }
                StreamWriter file = new("exstat.log", true, Encoding.UTF8);
                file.WriteLine(content);
                file.WriteLine("\\____/");
                file.Close();
                ++flush.exstat;
            } catch (Exception) {
                Console.WriteLine("ERROR: throw " + content);
            }
        }
    }
}
