using System.Text.Json;

namespace MicroServer.Debugger {
    static class Constants {

        public static readonly JsonSerializerOptions JSON_SERIZLIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
            IncludeFields = true
        };

    }
}
