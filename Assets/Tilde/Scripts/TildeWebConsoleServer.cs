using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Web;
using UnityEngine.Networking;

namespace Tilde {
    public static class HttpListenerResponseExtensions {
        public static void WriteString(this HttpListenerResponse response, string input, string type = "text/plain") {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";

            if (string.IsNullOrEmpty(input)) {
                return;
            }
            
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(input);
            response.ContentLength64 = buffer.Length;
            response.ContentType = type;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteBytes(this HttpListenerResponse response, byte[] bytes) {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteFile(this HttpListenerResponse response, string path, string type = "application/octet-stream", bool download = false) {
            using var fileStream = File.OpenRead(path);
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentLength64 = fileStream.Length;
            response.ContentType = type;
            
            if (download) {
                response.AddHeader("Content-disposition", $"attachment; filename={Path.GetFileName(path)}");
            }

            byte[] buffer = new byte[64 * 1024];
            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0) {
                response.OutputStream.Write(buffer, 0, bytesRead);
            }
        }
    }

    public class Route {
        public readonly Regex Pattern;
        public readonly Regex Methods = new Regex(@"(GET|HEAD)");
        public bool RunOnMainThread = true;
        public readonly Func<RequestContext, bool> Callback;
        
        public Route(string pattern, Func<RequestContext, bool> handler) {
            Pattern = new Regex(pattern, RegexOptions.IgnoreCase);
            Callback = handler;
        }
    }

    public class RequestContext {
        public Match RouteMatch;
        public readonly string Path;

        readonly HttpListenerContext context;

        public HttpListenerRequest Request => context.Request;
        public HttpListenerResponse Response => context.Response;

        public RequestContext(HttpListenerContext ctx) {
            context = ctx;
            RouteMatch = null;
            Path = HttpUtility.HtmlDecode(context.Request.Url.AbsolutePath);
            if (Path == "/") {
                Path = "/index.html";
            }
        }
    }

    public sealed class TildeWebConsoleServer : MonoBehaviour {
        [SerializeField] TildeConsole Console;
        [SerializeField] int Port = 55055;

        static Thread mainThread;
        static string fileRoot;
        static HttpListener listener = new();
        static readonly List<Route> routes = new();
        static readonly Queue<RequestContext> mainThreadRequests = new();
        static readonly Dictionary<string, string> supportedFileTypes = new() {
            { "js", "application/javascript" },
            { "json", "application/json" },
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" },
            { "gif", "image/gif" },
            { "png", "image/png" },
            { "css", "text/css" },
            { "htm", "text/html" },
            { "html", "text/html" },
            { "ico", "image/x-icon" },
        };

        static string GetRequestedFilePath(RequestContext context) {
            return Path.Combine(fileRoot, context.RouteMatch.Groups[1].Value);
        }

        static string GetFileResponseType(string path) {
            string ext = Path.GetExtension(path).ToLower().TrimStart(new[] { '.' });
            return supportedFileTypes.TryGetValue(ext, out string type) ? type : "application/octet-stream";
        }

        static bool WWWFileHandler(RequestContext context, bool download) {
            string path = GetRequestedFilePath(context);
            string type = download ? "application/octet-stream" : GetFileResponseType(path);

            using var request = UnityWebRequest.Get(path);
            
            var requestOperation = request.SendWebRequest();
            while (!requestOperation.isDone) {
                Thread.Sleep(0);
            }

            if (!string.IsNullOrEmpty(request.error)) {
                if (request.error.StartsWith("Couldn't open file")) {
                    return false;
                }
                
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = $"Fatal error:\n{request.error}";
                return true;
            }
            
            context.Response.ContentType = type;
            if (download) {
                context.Response.AddHeader("Content-disposition", $"attachment; filename={Path.GetFileName(path)}");
            }
            context.Response.WriteBytes(request.downloadHandler.data);
            return true;
        }

        static bool LocalFileHandler(RequestContext context, bool download) {
            string path = GetRequestedFilePath(context);
            string type = download ? "application/octet-stream" : GetFileResponseType(path);

            if (!File.Exists(path)) {
                return false;
            }
            
            context.Response.WriteFile(path, type, download);
            return true;
        }
        
        static void ListenerCallback(IAsyncResult result) {
            FulfillRequest(new RequestContext(listener.EndGetContext(result)));
            
            if (listener.IsListening) {
                listener.BeginGetContext(ListenerCallback, null);
            }
        }
        
        static void FulfillRequest(RequestContext context) {
            try {
                bool wasHandled = false;

                foreach (var route in routes) {
                    var match = route.Pattern.Match(context.Path);
                    
                    // Check if this route matches the request
                    if (!match.Success || !route.Methods.IsMatch(context.Request.HttpMethod)) {
                        continue;
                    }

                    // Upgrade to main thread if necessary
                    if (route.RunOnMainThread && Thread.CurrentThread != mainThread) {
                        lock (mainThreadRequests) {
                            mainThreadRequests.Enqueue(context);
                        }
                        return;
                    }
                    
                    context.RouteMatch = match;
                    wasHandled = route.Callback(context);
                    if (wasHandled) {
                        break;
                    }
                }

                if (!wasHandled) {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusDescription = "Not Found";
                }
            } catch (Exception exception) {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = $"Fatal error:\n{exception}";
                Debug.LogException(exception);
            }

            context.Response.OutputStream.Close();
        }
        
        public void Awake() {
            mainThread = Thread.CurrentThread;
            fileRoot = Path.Combine(Application.streamingAssetsPath, "Tilde");
            
            routes.Add(new Route(@"^/test$", context => {
                context.Response.WriteString("it works!");
                return true;
            }));
            
            // Register Routes
            routes.Add(new Route(@"^/console/out$", context => {
                context.Response.WriteString(HttpUtility.HtmlEncode(Console.Content));
                return true;
            }));
            
            routes.Add(new Route(@"^/console/run$", context => {
                Console.RunCommand(Uri.UnescapeDataString(context.Request.QueryString.Get("command")));
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.StatusDescription = "OK";
                return true;
            }));
            
            routes.Add(new Route(@"^/console/commandHistory$", context => {
                string index = context.Request.QueryString.Get("index");
                string previous = null;
                if (!string.IsNullOrEmpty(index)) {
                    previous = Console.History[int.Parse(index)];
                }
                context.Response.WriteString(previous);
                return true;
            }));

            routes.Add(new Route(@"^/console/complete$", context => {
                string partialCommand = context.Request.QueryString.Get("command");
                string found = null;
                if (partialCommand != null) {
                    found = Console.Completer.Complete(partialCommand);
                }
                context.Response.WriteString(found);
                return true;
            }));
            
            string fileExtensionsPattern = $"({string.Join("|", supportedFileTypes.Keys.ToArray())})";
            bool needsWWW = fileRoot.Contains("://");
            Func<RequestContext, bool, bool> callback = needsWWW ? WWWFileHandler : LocalFileHandler;
            
            // download route
            routes.Add(new Route($@"^/download/(.*\.{fileExtensionsPattern})$", context => {
                callback(context, true);
                return true;
            }) {
                RunOnMainThread = needsWWW
            });
            
            // file route
            routes.Add(new Route($@"^/(.*\.{fileExtensionsPattern})$", context => {
                callback(context, false);
                return true;
            }) {
                RunOnMainThread = needsWWW
            });

            // Start the server
            Debug.Log("Starting Tilde Server on port : " + Port);
            listener.Prefixes.Add("http://*:" + Port + "/");
            listener.Start();
            listener.BeginGetContext(ListenerCallback, null);
        }

        public void OnApplicationPause(bool paused) {
            if (paused) {
                listener.Stop();
            } else {
                listener.Start();
                listener.BeginGetContext(ListenerCallback, null);
            }
        }
        
        void LateUpdate() {
            RequestContext context;
            lock (mainThreadRequests) {
                if (mainThreadRequests.Count == 0) {
                    return;
                }
                context = mainThreadRequests.Dequeue();
            }
            FulfillRequest(context);
        }
        
        void OnDestroy() {
            listener.Close();
            listener = null;
        }

        void Reset() {
            if (Console != null) {
                return;
            }
            
            var existingConsole = FindObjectOfType<TildeConsole>();
            if (existingConsole != null) {
                Console = existingConsole;
            }
        }
    }
}