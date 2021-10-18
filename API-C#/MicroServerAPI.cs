using System;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Reflection;
using System.Net.Http.Headers;


namespace MicroServerAPI {

    public class MicroService : IDisposable {
        private string url_get, url_post;
        protected bool persistentMode;

        private readonly MediaTypeWithQualityHeaderValue CONTENT_TYPE = new MediaTypeWithQualityHeaderValue("application/json");

        private HttpClient client = new() {
            Timeout = TimeSpan.FromSeconds(60)
        };

        public MicroService(string get_url, string post_url, bool persistentConnections = false) {
            url_get = get_url;
            url_post = post_url;
            persistentMode = persistentConnections;

            if (!Uri.TryCreate(url_get, UriKind.Absolute, out Uri result) || result.Scheme != Uri.UriSchemeHttp)
                throw new ArgumentException("Uncorect GET url");

            if (!Uri.TryCreate(url_post, UriKind.Absolute, out result) || result.Scheme != Uri.UriSchemeHttp)
                throw new ArgumentException("Uncorect POST url");

            client.DefaultRequestHeaders.Accept.Add(CONTENT_TYPE);
        }

        /// <summary>
        /// If set to <see langword="true"/>, functions will try to connect to the server over and over again, even if it is not available. The default value is <see langword="false"/>.
        /// </summary>
        public bool PersistentConnections { get { return persistentMode; } set { persistentMode = value; } }

        public T GetJob<T>(string job_type, out ResponseAddress address) {
            if (job_type == null) throw new ArgumentNullException(nameof(job_type));

            T content = RequestJob<T>("?type=" + HttpUtility.UrlEncode(job_type), out BasicContent recv);
            address = new ResponseAddress(recv.id, recv.visibleId);
            return content;
        }

        public T GetJob<T>(string job_type, string job_id) {
            if (job_type == null) throw new ArgumentNullException(nameof(job_type));
            if (job_id == null) throw new ArgumentNullException(nameof(job_id));

            return RequestJob<T>("?type=" + HttpUtility.UrlEncode(job_type) + "&id=" + HttpUtility.UrlEncode(job_id), out _);
        }

        /// <returns>"job_type" of received content</returns>
        public string GetJob<T>(string job_id, out T content) {
            if (job_id == null) throw new ArgumentNullException(nameof(job_id));

            content = RequestJob<T>("?id=" + HttpUtility.UrlEncode(job_id), out BasicContent recv);
            return recv.type;
        }

        private T RequestJob<T>(string request_str, out BasicContent received) {
            HttpResponseMessage response;
            Task<HttpResponseMessage> tresponse;
            while (true) {
                tresponse = client.GetAsync(url_get + request_str);
                try {
                    tresponse.Wait();
                    response = tresponse.Result;
                } catch {
                    if (tresponse.IsCanceled || persistentMode)
                        continue;
                    else
                        throw;
                }
                if (response.StatusCode != System.Net.HttpStatusCode.RequestTimeout)
                    break;
            }

            if (!response.IsSuccessStatusCode) {
                throw new AggregateException("Job can not be received: " + response.StatusCode);
            }

            string rawcontent;
            using (var reader = new StreamReader(response.Content.ReadAsStream(), Encoding.UTF8)) {
                rawcontent = reader.ReadToEnd();
            }

            received = JsonSerializer.Deserialize<BasicContent>(rawcontent, JSON_SERIZLIZER_OPTIONS);
            T content = JsonSerializer.Deserialize<T>(((JsonElement)received.content).GetRawText(), JSON_SERIZLIZER_OPTIONS);

            return content;
        }

        public void PostFinalResult<T>(string result_type, ResponseAddress targetAddress, T content) {
            PostJob<T>(result_type, targetAddress.id, true, content);
        }

        public void PostIntermediateResult<T>(string result_type, ResponseAddress targetAddress, T content) {
            if (result_type == null) throw new ArgumentNullException(nameof(result_type));

            PostJob<T>(result_type, targetAddress.id, targetAddress.visibleId, content);
        }

        public void PostJob<T>(string job_type, T content) {
            PostJob<T>(job_type, "null", true, content);
        }

#nullable enable
        public void PostJob<T>(string? job_type, string job_id, bool visibleId, T content) {
#nullable disable
            if (job_id == null) throw new ArgumentNullException(nameof(job_id));
            if (job_type == null) job_type = "null";
            
            if (job_type == "null" && !visibleId) throw new ArgumentNullException(nameof(job_type), "\"type\" is null while \"visibleId\" is false");

            BasicContent bcontent = new(job_type, job_id, visibleId, content);

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(bcontent, JSON_SERIZLIZER_OPTIONS);

            HttpContent hcontent = new ByteArrayContent(json);
            hcontent.Headers.ContentType = CONTENT_TYPE;

            Task<HttpResponseMessage> tresponse = null;

            while (true) {
                try {
                    tresponse = client.PostAsync(url_post, hcontent);
                    tresponse.Wait();
                    break;
                } catch {
                    if (!persistentMode)
                        throw;
                }
            }

            if (!tresponse.Result.IsSuccessStatusCode) {
                throw new AggregateException("Job can not be posted: " + tresponse.Result.StatusCode);
            }
        }

        public O ProcessAsFunction<O, I>(string requested_function, I content) {
            string cid = Assembly.GetExecutingAssembly().Location + "." + DateTime.UtcNow.Ticks.ToString() + "." + Thread.CurrentThread.ManagedThreadId.ToString();

            PostJob<I>(requested_function, cid, false, content);
            GetJob<O>(cid, out O result);
            return result;
        }

        public O ProcessAsFunction<O, I>(string requested_function, I content, string target_content_type) {
            string cid = Assembly.GetExecutingAssembly().Location + "." + DateTime.UtcNow.Ticks.ToString() + "." + Thread.CurrentThread.ManagedThreadId.ToString();

            PostJob<I>(requested_function, cid, false, content);
            return GetJob<O>(target_content_type, cid);
        }

        protected virtual void DisposeClass() {
            if (client != null) {
                client.Dispose();
                client = null;
            }
        }

        public void Dispose() {
            DisposeClass();
            GC.SuppressFinalize(this);
        }

        ~MicroService() { }

        private static readonly JsonSerializerOptions JSON_SERIZLIZER_OPTIONS = new(JsonSerializerDefaults.Web) {
            IncludeFields = true
        };
        
        private class BasicContent {
            public BasicContent(string type, string id, bool visibleId, object content) {
                this.type = type;
                this.id = id;
                this.content = content;
                this.visibleId = visibleId;
            }

            public string id { get; set; }

            public bool visibleId { get; set; }

            public string type { get; set; }

            public object content { get; set; }
        }
    }

    public struct ResponseAddress {
        public string id;
        public bool visibleId;
        public ResponseAddress(string id, bool visibleId) {
            this.id = id; this.visibleId = visibleId;
        }
    }
}
