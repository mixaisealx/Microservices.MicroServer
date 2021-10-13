using System.Text.Json;


namespace Microservices_MicroServer {
    static class Constants {
        public const string PREFIX_GETJOB = "http://*:8080/microserver/get-job/";
        public const string PREFIX_POSTJOB = "http://*:8080/microserver/post-job/";
        
        public static readonly string[] SPECIAL_TYPES = new string[] { "MicroServer.25367be645.ExternalStatus",
        "MicroServer.25367be645.FetchOverflow",
        "MicroServer.25367be645.CompensateUnderflow",
        "MicroServer.25367be645.DebugEdition.getInternalStorageSnapshot",
        "MicroServer.25367be645.DebugEdition.getLocallyAvailibleTypes",
        "MicroServer.25367be645.DebugEdition.getTypesStatistic",
        "MicroServer.25367be645.DebugEdition.retrivePostHistory",
        "MicroServer.25367be645.DebugEdition.retriveGetHistory",
        "MicroServer.25367be645.DebugEdition.getPendings",
        "MicroServer.25367be645.GET_TIMEOUT_25"};

        public static readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
            IncludeFields = true
        };
    }
}
