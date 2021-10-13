using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Globalization;


namespace MicroServer.Debugger {
    static class Utils {

        static readonly UnicodeCategory[] nonRenderingCategories = new UnicodeCategory[] { UnicodeCategory.Control, UnicodeCategory.OtherNotAssigned, UnicodeCategory.Surrogate };

        public static bool IsNonPrintable(string str) {
            return str.Any(c => c == '\n' || !char.IsWhiteSpace(c) && nonRenderingCategories.Contains(char.GetUnicodeCategory(c)));
        }

        public enum ServerValidationResult {
            OK, Timeouted, RefusedPOST, RefusedGET, UnexpectedPOSTBehaviour, UnexpectedGETBehaviour, UnknownCommunicationFormat, InvalidDataReceived
        }
        public static ServerValidationResult IsServerValid(string get, string post) {
            var data = new StringContent("{\"id\":\"id725367be645\",\"visibleId\":false,\"type\":\"MicroServer.DebugEdition.bc18e76bd30a0d773deb201cd66bd725367be645.TEST\",\"content\":\"bc18e76bd30a0d773deb201cd66bd725367be645\"}", Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            {
                try {
                    var task = Program.clientHttp.PostAsync(post, data);
                    task.Wait();
                    response = task.Result;
                } catch {
                    return ServerValidationResult.RefusedPOST;
                }
            }

            if (!response.IsSuccessStatusCode) {
                return ServerValidationResult.UnexpectedPOSTBehaviour;
            }

            {
                var task = Program.clientHttp.GetAsync(get + "?type=MicroServer.DebugEdition.bc18e76bd30a0d773deb201cd66bd725367be645.TEST");
                try {
                    task.Wait();
                    response = task.Result;
                } catch {
                    if (task.IsCanceled) {
                        return ServerValidationResult.Timeouted;
                    } else {
                        return ServerValidationResult.RefusedGET;
                    }
                }
            }

            if (!response.IsSuccessStatusCode) {
                return ServerValidationResult.UnexpectedGETBehaviour;
            }

            string content;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                content = reader.ReadToEnd();
            }

            try {
                BasicContent received = JsonSerializer.Deserialize<BasicContent>(content, Constants.JSON_SERIZLIZER_OPTIONS);
                if (received.type == "MicroServer.DebugEdition.bc18e76bd30a0d773deb201cd66bd725367be645.TEST" && received.id == "id725367be645" && ((JsonElement)received.content).GetString() == "bc18e76bd30a0d773deb201cd66bd725367be645") {
                    return ServerValidationResult.OK;
                } else {
                    return ServerValidationResult.InvalidDataReceived;
                }
            } catch (Exception) {
                return ServerValidationResult.UnknownCommunicationFormat;
            }
        }

        public static bool RunServerTest(string get, string post) {
            ServerValidationResult vres = IsServerValid(get, post);
            if (vres == ServerValidationResult.OK) {
                Console.WriteLine("== SERVER VALIDATED SUCCESSFULLY ==");
                return true;
            } else {
                Console.WriteLine("== SERVER VALIDATING ERROR ==");
                Console.WriteLine("To check the correctness of the entered addresses, the program performs the most basic testing of the server functionality. " +
                    "This helps determine if a URL is entered incorrectly, but it does NOT help you determine if your own server implementation is functionally equivalent to the original one.");
                Console.WriteLine();
                Console.WriteLine("Error name: " + vres);
                Console.WriteLine("Error description:");
                switch (vres) {
                    case ServerValidationResult.Timeouted:
                        Console.WriteLine("The timeout for a response to a GET request has expired on the side of this program. " +
                            "This should not happen if the server timeout is set correctly (about 25 seconds).");
                        break;
                    case ServerValidationResult.RefusedPOST:
                        Console.WriteLine("The POST request was rejected. " +
                            "You probably have incorrectly specified the URL for POST requests.");
                        break;
                    case ServerValidationResult.RefusedGET:
                        Console.WriteLine("The GET request was rejected. " +
                            "You probably have incorrectly specified the URL for GET requests.");
                        break;
                    case ServerValidationResult.UnexpectedPOSTBehaviour:
                        Console.WriteLine("The server behaves incorrectly for POST requests. " +
                            "You probably confused the GET and POST addresses, or you made a mistake in some part of the address after specifying the host and port. " +
                            "\nPerhaps you are using an non-original server. " +
                            "Tip: When developing your server, rely on the MicroServer's documentation, not the test from this program. ");
                        break;
                    case ServerValidationResult.UnexpectedGETBehaviour:
                        Console.WriteLine("The server behaves incorrectly for GET requests. " +
                            "You probably confused the GET and POST addresses, or you made a mistake in some part of the address after specifying the host and port. " +
                            "\nPerhaps you are using an non-original server. " +
                            "Tip: When developing your server, rely on the MicroServer's documentation, not the test from this program. ");
                        break;
                    case ServerValidationResult.UnknownCommunicationFormat:
                        Console.WriteLine("You are definitely developing your own microservices server. " +
                            "Note that communication between the client and the server on jobs occurs strictly within the framework of the \"BasicContent\" data structure (see the code of the original server).");
                        break;
                    case ServerValidationResult.InvalidDataReceived:
                        Console.WriteLine("You are definitely developing your own microservices server. " +
                            "There seems to be a bug in your implementation of the task store (the received data does not match the sent one). " +
                            "It might have something to do with multithreading (are you missed some mutex?).");
                        break;
                }
                return false;
            }
        }

        public static string GetPrintableString(string unk_str) {
            if (IsNonPrintable(unk_str)) {
                return "(hex) " + BitConverter.ToString(Encoding.UTF8.GetBytes(unk_str)).Replace("-", "");
            } else {
                return "(text) " + unk_str;
            }
        }

        public static string GetPrintableContent(object obj) {
            JsonElement jsel = (JsonElement)obj;
            switch (jsel.ValueKind) {
                case JsonValueKind.Object:
                    return "JSON.Object";
                case JsonValueKind.Array:
                    return "[...].Length=" + jsel.GetArrayLength();
                case JsonValueKind.String:
                    return GetPrintableString(jsel.GetString());
                case JsonValueKind.Number:
                    return jsel.GetInt64().ToString();
                case JsonValueKind.True:
                    return "[boolean] true";
                case JsonValueKind.False:
                    return "[boolean] false";
                case JsonValueKind.Null:
                    return "[NULL]";
                default:
                    return typeof(JsonElement).FullName;
            }
        }
    }
}
