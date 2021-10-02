using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Microservices_MicroServer {
    static class ContentStorage {
        static Dictionary<string, Queue<BasicContent>> storage = new();
        static HashSet<string> external_storaged = new();
        static Mutex mtx = new(); //Syncronization object (for saving storage integrity)

        public static void PushContent(BasicContent content) {
            mtx.WaitOne();

            if (storage.TryGetValue(content.type, out Queue<BasicContent> que)) {
                que.Enqueue(content);
                
                if (que.Count > 128 && !external_storaged.Contains(content.type)) {
                    external_storaged.Add(content.type);
                }
            } else {
                storage.Add(content.type, new Queue<BasicContent>(
                    new BasicContent[] { content }
                    ));
            }

#if MicroServer_DebugEdition
            if (debug_posthistory.Count > 512) debug_posthistory.RemoveRange(0, 128);
            debug_posthistory.Add(new PostHistory(DateTime.Now, content));
#endif
            mtx.ReleaseMutex();
        }

        /// <summary>
        /// Remove first occurence of element in queue
        /// </summary>
        /// <param name="que">Must be not empty</param>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        static Queue<BasicContent> RemoveFirstOccurence(Queue<BasicContent> que, BasicContent toRemove) {
            Queue<BasicContent> result = new(que.Count - 1);

            Queue<BasicContent>.Enumerator en = que.GetEnumerator();

            while (en.MoveNext() && en.Current != toRemove) {
                result.Enqueue(en.Current);
            }

            while (en.MoveNext()) {
                result.Enqueue(en.Current);
            }

            return result;
        }

        public static BasicContent PopContent(string type, string id) {
            BasicContent result = null;

            mtx.WaitOne();

            if (type == "null") {
                result = storage.SelectMany(t => t.Value.Where(c => c.visibleId && c.id == id)).FirstOrDefault(); //Gets element from storage with given id (if such exists)
                if (result != null) {
                    Queue<BasicContent> que = storage[result.type] = RemoveFirstOccurence(storage[result.type], result);

                    if (que.Count == 0)
                        storage.Remove(result.type);

#if MicroServer_DebugEdition
                    if (debug_gethistory.Count > 512) debug_gethistory.RemoveRange(0, 128);
                    debug_gethistory.Add(new GetHistory(DateTime.Now, result, "null", id));
#endif
                }
            } else if (storage.TryGetValue(type, out Queue<BasicContent> que)) {
                if (id == "null") {
                    result = que.Dequeue();
                    
                    if (que.Count == 0)
                        storage.Remove(type);
#if MicroServer_DebugEdition
                    if (debug_gethistory.Count > 512) debug_gethistory.RemoveRange(0, 128);
                    debug_gethistory.Add(new GetHistory(DateTime.Now, result, type, id));
#endif
                } else {
                    result = que.Where(c => (id == "null" || c.visibleId && c.id == id)).FirstOrDefault();

                    if (result != null) {
                        Queue<BasicContent> quet = storage[type] = RemoveFirstOccurence(que, result);

                        if (quet.Count == 0)
                            storage.Remove(type);
#if MicroServer_DebugEdition
                    if (debug_gethistory.Count > 512) debug_gethistory.RemoveRange(0, 128);
                    debug_gethistory.Add(new GetHistory(DateTime.Now, result, type, id));
#endif
                    }
                }
            }

            mtx.ReleaseMutex();
            return result;
        }

        internal struct ExternalStatus {
            public string type;
            public bool underflow, overflow;
            public ExternalStatus(string type, bool underflow, bool overflow) {
                this.type = type; this.underflow = underflow; this.overflow = overflow;
            }
        }
        internal static IEnumerable<ExternalStatus> GetExternalStatus() {
            List<ExternalStatus> stat = new();

            mtx.WaitOne();

            foreach (var item in external_storaged) {
                if (storage.TryGetValue(item, out Queue<BasicContent> que)) {
                    stat.Add(new ExternalStatus(item, que.Count < 64, que.Count > 128));
                } else {
                    stat.Add(new ExternalStatus(item, true, false));
                }
            }

            mtx.ReleaseMutex();
            return stat;
        }

        internal static IEnumerable<BasicContent> FetchOverflow(string type) {
            IEnumerable<BasicContent> elms = null;

            mtx.WaitOne();

            if (storage.TryGetValue(type, out Queue<BasicContent> que) && que.Count > 128) {
                int scnt = que.Count - 96;
                elms = que.Take(scnt);
                storage[type] = new Queue<BasicContent>(que.Skip(scnt));
            }
            mtx.ReleaseMutex();
            return elms;
        }

        internal static bool СompensateUnderflow(IEnumerable<BasicContent> content) {
            string type = content.First().type;
            if (!external_storaged.Contains(type) || content.Any(x => x.type != type || x.type == "null" && !x.visibleId))
                return false;

            mtx.WaitOne();

            if (storage.TryGetValue(type, out Queue<BasicContent> que)) {
                if (que.Count < 64 && content.Count() + que.Count < 128) {
                    foreach (var item in content) {
                        que.Enqueue(item);
                    }
                } else {
                    mtx.ReleaseMutex();
                    return false;
                }
            } else {
                storage.Add(type, new Queue<BasicContent>(content));
            }

            mtx.ReleaseMutex();
            return true;
        }

#if MicroServer_DebugEdition
        internal struct PostHistory {
            public DateTime datetime;
            public BasicContent content;
            public PostHistory(DateTime datetime, BasicContent content) {
                this.datetime = datetime;
                this.content = content;
            }
        }
        internal struct GetHistory {
            public DateTime datetime;
            public string requestedType, requestedId;
            public BasicContent content;
            public GetHistory(DateTime datetime, BasicContent content, string requestedType, string requestedId) {
                this.datetime = datetime;
                this.content = content;
                this.requestedType = requestedType;
                this.requestedId = requestedId;
            }
        }

        static List<PostHistory> debug_posthistory = new();
        static List<GetHistory> debug_gethistory = new();

        internal static IEnumerable<GetHistory> debug_RetriveGetHistory() {
            mtx.WaitOne();
            var result = new List<GetHistory>(debug_gethistory);
            debug_gethistory.Clear();
            mtx.ReleaseMutex();
            return result;
        }
        internal static IEnumerable<PostHistory> debug_RetrivePostHistory() {
            mtx.WaitOne();
            var result = new List<PostHistory>(debug_posthistory);
            debug_posthistory.Clear();
            mtx.ReleaseMutex();
            return result;
        }

        internal static IEnumerable<BasicContent> debug_InternalStorageSnapshot() {
            mtx.WaitOne();
            var result = storage.SelectMany(i => i.Value);
            mtx.ReleaseMutex();
            return result;
        }

        internal static IEnumerable<string> debug_GetLocallyStoredTypes() {
            mtx.WaitOne();
            var result = storage.Keys;
            mtx.ReleaseMutex();
            return result;
        }

        internal static IEnumerable<KeyValuePair<string, uint>> debug_GetTypeStatistic() {
            mtx.WaitOne();
            var resultemp = storage.Select(i => new KeyValuePair<string, uint>(i.Key, (uint)i.Value.Count));
            var result = resultemp.Concat(external_storaged.Where(i => resultemp.All(p => p.Key != i)).Select(i => new KeyValuePair<string, uint>(i, 0)));
            mtx.ReleaseMutex();
            return result;
        }

        static Mutex debug_pendings_mxt = new();
        internal struct Pending {
            public string type, id;
            public Pending(string type, string id) {
                this.type = type; this.id = id;
            }
        }
        static List<Pending> debug_pendings = new();
        internal static Pending debug_AddPending(string type, string id) {
            debug_pendings_mxt.WaitOne();
            Pending pnd = new(type, id);
            debug_pendings.Add(pnd);
            debug_pendings_mxt.ReleaseMutex();
            return pnd;
        }

        internal static void debug_RemovePending(Pending pending) {
            debug_pendings_mxt.WaitOne();
            debug_pendings.Remove(pending);
            debug_pendings_mxt.ReleaseMutex();
        }

        internal static IEnumerable<Pending> debug_GetPendings() {
            debug_pendings_mxt.WaitOne();
            var result = new List<Pending>(debug_pendings);
            debug_pendings_mxt.ReleaseMutex();
            return result;
        }
#endif

    }
}
