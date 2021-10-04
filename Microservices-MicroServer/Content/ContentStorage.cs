using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Microservices_MicroServer {
    static class ContentStorage {

        static Dictionary<string, Queue<BasicContent>> storage = new();
        static ReaderWriterLockSlim lockStorage = new(); //Syncronization object (for saving storage integrity)

        static HashSet<string> external_storaged = new();
        static ReaderWriterLockSlim lockExternals = new(); //Syncronization object (for saving external_storaged integrity)

        public static void PushContent(BasicContent content) {
            Queue<BasicContent> que = null;

            lockStorage.EnterUpgradeableReadLock();
            if (storage.TryGetValue(content.type, out que)) {
                lockStorage.EnterWriteLock();
                que.Enqueue(content);
                lockStorage.ExitWriteLock();
            } else {
                lockStorage.EnterWriteLock();
                storage.Add(content.type, new Queue<BasicContent>(
                    new BasicContent[] { content }
                    ));
                lockStorage.ExitWriteLock();
            }
            lockStorage.ExitUpgradeableReadLock();

            if (que != null) {
                lockExternals.EnterUpgradeableReadLock();
                if (que.Count > 128 && !external_storaged.Contains(content.type)) {

                    lockExternals.EnterWriteLock();
                    external_storaged.Add(content.type);
                    lockExternals.ExitWriteLock();

                }
                lockExternals.ExitUpgradeableReadLock();
            }

#if MicroServer_DebugEdition
            debug_lockPost.WaitOne();
            if (debug_posthistory.Count > 512) debug_posthistory.RemoveRange(0, 128);
            debug_posthistory.Add(new PostHistory(DateTime.Now, content));
            debug_lockPost.ReleaseMutex();
#endif
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

            if (type == "null") {
                lockStorage.EnterReadLock(); //Start 1
                result = storage.SelectMany(t => t.Value.Where(c => c.visibleId && c.id == id)).FirstOrDefault(); //Gets element from storage with given id (if such exists)

                if (result == null) {
                    lockStorage.ExitReadLock(); //End 1
                } else {
                    Queue<BasicContent> que_new = RemoveFirstOccurence(storage[result.type], result);
                    lockStorage.ExitReadLock(); //End 1
                    //Totally UNLOCKED zone! Anything can happen!
                    lockStorage.EnterWriteLock(); //Start 2
                    try {
                        storage[result.type] = que_new;

                        if (que_new.Count == 0)
                            storage.Remove(result.type);
                    } catch {
                        result = null;
                    } finally {
                        lockStorage.ExitWriteLock(); //End 2
                    }
                }
            } else if (id == "null") {
                lockStorage.EnterUpgradeableReadLock();
                if (storage.TryGetValue(type, out Queue<BasicContent> que)) {
                    lockStorage.EnterWriteLock();

                    result = que.Dequeue();
                    if (que.Count == 0)
                        storage.Remove(type);

                    lockStorage.ExitWriteLock();
                }
                lockStorage.ExitUpgradeableReadLock();
            } else {
                lockStorage.EnterReadLock(); //Start 3
                if (storage.TryGetValue(type, out Queue<BasicContent> que)) {
                    result = que.Where(c => (id == "null" || c.visibleId && c.id == id)).FirstOrDefault();

                    if (result == null) {
                        lockStorage.ExitReadLock(); //End 3
                    } else {
                        Queue<BasicContent> que_new = RemoveFirstOccurence(que, result);
                        lockStorage.ExitReadLock(); //End 3
                        //Totally UNLOCKED zone! Anything can happen!
                        lockStorage.EnterWriteLock(); //Start 4
                        try {
                            storage[type] = que_new;

                            if (que_new.Count == 0)
                                storage.Remove(type);
                        } catch {
                            result = null;
                        } finally {
                            lockStorage.ExitWriteLock(); //End 4
                        }
                    }
                } else
                    lockStorage.ExitReadLock(); //End 3
            }

#if MicroServer_DebugEdition
            if (result != null) {
                debug_lockGet.WaitOne();
                if (debug_gethistory.Count > 512) debug_gethistory.RemoveRange(0, 128);
                debug_gethistory.Add(new GetHistory(DateTime.Now, result, type, id));
                debug_lockGet.ReleaseMutex();
            }
#endif
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
            lockStorage.EnterReadLock();

            foreach (var item in external_storaged) {
                if (storage.TryGetValue(item, out Queue<BasicContent> que)) {
                    stat.Add(new ExternalStatus(item, que.Count < 64, que.Count > 128));
                } else {
                    stat.Add(new ExternalStatus(item, true, false));
                }
            }

            lockStorage.ExitReadLock();
            return stat;
        }

        internal static IEnumerable<BasicContent> FetchOverflow(string type) {
            IEnumerable<BasicContent> elms = null;
            lockStorage.EnterUpgradeableReadLock();

            if (storage.TryGetValue(type, out Queue<BasicContent> que) && que.Count > 128) {
                int scnt = que.Count - 96;
                elms = que.Take(scnt);
                var que_new = new Queue<BasicContent>(que.Skip(scnt));

                lockStorage.EnterWriteLock();
                storage[type] = que_new;
                lockStorage.ExitWriteLock();
            }

            lockStorage.ExitUpgradeableReadLock();
            return elms;
        }

        internal static bool СompensateUnderflow(IEnumerable<BasicContent> content) {
            string type = content.First().type;

            lockExternals.EnterReadLock();
            if (external_storaged.Contains(type)) {
                lockExternals.ExitReadLock();
            } else { 
                lockExternals.ExitReadLock();
                return false;
            }
                
            if (content.Any(x => x.type != type || x.type == "null" && !x.visibleId))
                return false;

            lockStorage.EnterUpgradeableReadLock(); //Start 1

            if (storage.TryGetValue(type, out Queue<BasicContent> que)) {
                if (que.Count < 64 && content.Count() + que.Count < 128) {

                    lockStorage.EnterWriteLock();
                    foreach (var item in content) {
                        que.Enqueue(item);
                    }
                    lockStorage.ExitWriteLock();

                } else {
                    lockStorage.ExitUpgradeableReadLock(); //Exit 1
                    return false;
                }
            } else {
                lockStorage.EnterWriteLock();
                storage.Add(type, new Queue<BasicContent>(content));
                lockStorage.ExitWriteLock();
            }

            lockStorage.ExitUpgradeableReadLock(); //Exit 1
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
        static Mutex debug_lockGet = new(); 
        static Mutex debug_lockPost = new(); 

        internal static IEnumerable<GetHistory> debug_RetriveGetHistory() {
            debug_lockGet.WaitOne();
            var result = new List<GetHistory>(debug_gethistory);
            debug_gethistory.Clear();
            debug_lockGet.ReleaseMutex();
            return result;
        }
        internal static IEnumerable<PostHistory> debug_RetrivePostHistory() {
            debug_lockPost.WaitOne();
            var result = new List<PostHistory>(debug_posthistory);
            debug_posthistory.Clear();
            debug_lockPost.ReleaseMutex();
            return result;
        }

        internal static IEnumerable<BasicContent> debug_InternalStorageSnapshot() {
            lockStorage.EnterReadLock();
            var result = storage.SelectMany(i => i.Value);
            lockStorage.ExitReadLock();
            return result;
        }

        internal static IEnumerable<string> debug_GetLocallyStoredTypes() {
            lockStorage.EnterReadLock();
            var result = storage.Keys;
            lockStorage.ExitReadLock();
            return result;
        }

        internal static IEnumerable<KeyValuePair<string, uint>> debug_GetTypeStatistic() {
            lockStorage.EnterReadLock();
            var resultemp = storage.Select(i => new KeyValuePair<string, uint>(i.Key, (uint)i.Value.Count));
            lockStorage.ExitReadLock();

            lockExternals.EnterReadLock();
            var result = resultemp.Concat(external_storaged.Where(i => resultemp.All(p => p.Key != i)).Select(i => new KeyValuePair<string, uint>(i, 0)));
            lockExternals.ExitReadLock();
            return result;
        }

        static ReaderWriterLockSlim debug_lock_pendings = new();
        internal struct Pending {
            public string type, id;
            public Pending(string type, string id) {
                this.type = type; this.id = id;
            }
        }
        static List<Pending> debug_pendings = new();
        internal static Pending debug_AddPending(string type, string id) {
            debug_lock_pendings.EnterWriteLock();
            Pending pnd = new(type, id);
            debug_pendings.Add(pnd);
            debug_lock_pendings.ExitWriteLock();
            return pnd;
        }

        internal static void debug_RemovePending(Pending pending) {
            debug_lock_pendings.EnterWriteLock();
            debug_pendings.Remove(pending);
            debug_lock_pendings.ExitWriteLock();
        }

        internal static IEnumerable<Pending> debug_GetPendings() {
            debug_lock_pendings.EnterReadLock();
            var result = new List<Pending>(debug_pendings);
            debug_lock_pendings.ExitReadLock();
            return result;
        }
#endif

    }
}
