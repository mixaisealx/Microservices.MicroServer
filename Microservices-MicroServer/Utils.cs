using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Microservices_MicroServer {
    static class Utils {
        public class ThreadManagerGET {
            bool ready_to_count = false;
            ManualResetEventSlim getEvent = new(false);
            Dictionary<Guid, ushort> pendings = new();

            Mutex pending_mtx = new();

            public void WaitForEvent() {
                getEvent.Wait();
            }

            public void SetEvent() {
                ready_to_count = true;
                getEvent.Set();
            }

            /// <summary>
            /// </summary>
            /// <param name="identifer"></param>
            /// <returns>GET-request thread manager identifing token</returns>
            public void RegisterPending(Guid identifer) {
                pending_mtx.WaitOne();
                pendings.Add(identifer, 0);
                pending_mtx.ReleaseMutex();
            }

            public void UnregisterPending(Guid identifer) {
                pending_mtx.WaitOne();
                pendings.Remove(identifer);
                pending_mtx.ReleaseMutex();
            }

            public void RequiredExecuted(Guid identifer) {
                pending_mtx.WaitOne();
                if (ready_to_count) {
                    ++pendings[identifer];
                    if (pendings.All(x => x.Value != 0)) {
                        getEvent.Reset();
                        ready_to_count = false;
                        foreach (var item in pendings.Keys) {
                            pendings[item] = 0;
                        }
                    }
                }
                pending_mtx.ReleaseMutex();
            }
        }

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
