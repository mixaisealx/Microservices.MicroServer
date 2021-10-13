

namespace Microservices_MicroServer {
    public class BasicContent {
        public BasicContent(string type) {
            this.type = type;
        }

        public string id { get; set; }

        public bool visibleId { get; set; }

        public string type { get; set; }

        public object content { get; set; }
    }
}
