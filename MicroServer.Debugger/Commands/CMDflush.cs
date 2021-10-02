using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;


namespace MicroServer.Debugger {
    static partial class CMDs {
        static class flush {
            internal static ushort exstat = 0, pend = 0, loctp = 0, tystat = 0, isnap = 0, gethist = 0, posthist = 0;
        }

        internal static void Flush() {
            uint count = 0;
            count += flush_file("exstat.log", flush.exstat);
            count += flush_file("pend.log", flush.pend);
            count += flush_file("loctp.log", flush.loctp);
            count += flush_file("tystat.log", flush.tystat);
            count += flush_file("isnap.log", flush.isnap);
            count += flush_file("gethist.log", flush.gethist);
            count += flush_file("posthist.log", flush.posthist);
            Console.WriteLine("Flushing done: " + count + " entries was removed");
        }

        static uint flush_file(string filename, ushort count) {
            string fullfile;
            {
                StreamReader reader;
                try {
                    reader = new StreamReader(filename, Encoding.UTF8);
                } catch (Exception) {
                    return 0;
                }
                fullfile = reader.ReadToEnd();
                reader.Close();
            }

            IEnumerable<string> requests = fullfile.Split("\n\\____/").Select(s => s.Trim());
            int req_count = requests.Count();
            if (req_count > count + 1) {
                requests = requests.SkipLast(1).TakeLast(count);
                StreamWriter file = new(filename, false, Encoding.UTF8);
                foreach (var item in requests) {
                    file.WriteLine(item);
                    file.WriteLine("\\____/");
                }
                file.Close();

                return (uint)(req_count - count - 1);
            }
            return 0;
        }
    }
}
