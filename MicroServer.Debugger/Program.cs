using System;
using System.IO;
using System.Net.Http;


namespace MicroServer.Debugger {
    class Program {
        internal static string url_get, url_post;

        internal static HttpClient clientHttp = new() {
            Timeout = TimeSpan.FromSeconds(60)
        };

        static void Main(string[] args) {
            Console.WriteLine("--== MiscoServer services communication debugger ==--");

            {
                if (File.Exists("GET.txt")) {
                    url_get = File.ReadAllText("GET.txt");
                    if (Uri.TryCreate(url_get, UriKind.Absolute, out Uri result) && result.Scheme == Uri.UriSchemeHttp) {
                        Console.WriteLine("<> GET url was loaded from GET.txt");
                        goto goto_skip_get;
                    }
                }
                do {
                    Console.WriteLine("Enter the GET url bellow (example: http://localhost:8080/microserver/get-job)");
                    Console.Write(">");
                    url_get = Console.ReadLine();
                } while (!Uri.TryCreate(url_get, UriKind.Absolute, out Uri result) || result.Scheme != Uri.UriSchemeHttp);
                File.WriteAllText("GET.txt", url_get);
                goto_skip_get:;
            }

            {
                if (File.Exists("POST.txt")) {
                    url_post = File.ReadAllText("POST.txt");
                    if (Uri.TryCreate(url_post, UriKind.Absolute, out Uri result) && result.Scheme == Uri.UriSchemeHttp) {
                        Console.WriteLine("<> POST url was loaded from POST.txt");
                        goto goto_skip_post;
                    }
                }
                do {
                    Console.WriteLine("Enter the POST url bellow (example: http://localhost:8080/microserver/post-job)");
                    Console.Write(">");
                    url_post = Console.ReadLine();
                } while (!Uri.TryCreate(url_post, UriKind.Absolute, out Uri result) || result.Scheme != Uri.UriSchemeHttp);
                File.WriteAllText("POST.txt", url_post);
                goto_skip_post:;
            }
            

            if (!Utils.RunServerTest(url_get, url_post)) {
                Console.ReadKey();
                return;
            }

            string cmd;
            do {
                Console.Write("\n<> Enter the debugger command: ");
                cmd = Console.ReadLine().Trim().ToLower();
                switch (cmd) {
                    case "help":
                        CMDs.DisplayHelp();
                        break;
                    case "exstat":
                        CMDs.exstat();
                        break;
                    case "pend":
                        CMDs.pend();
                        break;
                    case "loctp":
                        CMDs.loctp();
                        break;
                    case "tystat":
                        CMDs.tystat();
                        break;
                    case "isnap":
                        CMDs.isnap();
                        break;
                    case "gethist":
                        CMDs.gethist();
                        break;
                    case "posthist":
                        CMDs.posthist();
                        break;
                    case "flush":
                        CMDs.Flush();
                        break;
                    case "validate":
                        Utils.RunServerTest(url_get, url_post);
                        break;
                    case "exit":
                        break;
                    default:
                        Console.WriteLine("The command is not recognized. Enter \"help\" for help.");
                        break;
                }
            } while (cmd != "exit");
        }
    }
}
