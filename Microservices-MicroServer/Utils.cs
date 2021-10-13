using System;
using System.Linq;


namespace Microservices_MicroServer {
    static class Utils {

        public static bool IsJobGet(string[] segments) {
            var url_fragments = segments.Skip(1).Select(s => s.Replace("/", ""));
            var get_job = Constants.PREFIX_GETJOB.Split('/').Skip(3).SkipLast(1);

            if (url_fragments.SequenceEqual(get_job))
                return true;

            return false;
        }

        public static bool IsJobPost(string[] segments) {
            var url_fragments = segments.Skip(1).Select(s => s.Replace("/", ""));
            var post_job = Constants.PREFIX_POSTJOB.Split('/').Skip(3).SkipLast(1);

            if (url_fragments.SequenceEqual(post_job))
                return true;

            return false;
        }

        public static void FixNullInBasicContent(ref BasicContent content) {
            if (content.type == null) 
                content.type = "null";

            if (content.id == null)
                content.id = "null";
        }

        public static BasicContent FixNullInBasicContent(BasicContent content) {
            if (content.type == null)
                content.type = "null";

            if (content.id == null)
                content.id = "null";

            return content;
        }
    }
}
