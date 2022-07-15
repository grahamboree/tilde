using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Web;

namespace Tilde {
    public static class HttpListenerResponseExtensions {
        public static void WriteString(this HttpListenerResponse response, string input) {
            response.WriteBytes(System.Text.Encoding.UTF8.GetBytes(input), "text/plain");
        }

        public static void WriteBytes(this HttpListenerResponse response, byte[] bytes, string type) {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            response.ContentType = type;
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }
    }

    public class RequestContext {
        public readonly string Path;
        
        readonly HttpListenerContext context;

        public HttpListenerRequest Request => context.Request;
        public HttpListenerResponse Response => context.Response;

        public RequestContext(HttpListenerContext ctx) {
            context = ctx;
            Path = HttpUtility.HtmlDecode(context.Request.Url.AbsolutePath);
            if (Path == "/") {
                Path = "/index.html";
            }
        }
    }

    public sealed class TildeWebConsoleServer : MonoBehaviour {
        [SerializeField] TildeConsole Console;
        [SerializeField] int Port = 55055;
        
        [SerializeField] TextAsset IndexHTML;
        [SerializeField] TextAsset LogoPNG;
        
        static Thread mainThread;
        static HttpListener listener = new();
        static Dictionary<string, Action<RequestContext>> routes;
        static readonly Queue<RequestContext> mainThreadRequests = new();

        static void ListenerCallback(IAsyncResult result) {
            FulfillRequest(new RequestContext(listener.EndGetContext(result)));
            
            if (listener.IsListening) {
                listener.BeginGetContext(ListenerCallback, null);
            }
        }
        
        static void FulfillRequest(RequestContext context) {
            try {
                if (context.Request.HttpMethod is "GET" or "HEAD" && routes.TryGetValue(context.Path, out var handler)) {
                    // Upgrade to main thread if necessary
                    if (Thread.CurrentThread != mainThread) {
                        lock (mainThreadRequests) {
                            mainThreadRequests.Enqueue(context);
                        }
                        return;
                    }

                    handler(context);
                    return;
                }
                
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = "Not Found";
            } catch (Exception exception) {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = $"Fatal error:\n{exception}";
                Debug.LogException(exception);
            }
        }
        
        public void Awake() {
            mainThread = Thread.CurrentThread;
            
            // Register Routes
            routes = new Dictionary<string, Action<RequestContext>> {
                ["/index.html"] = context => context.Response.WriteBytes(IndexHTML.bytes, "text/html"),
                ["/TildeLogo.png"] = context => context.Response.WriteBytes(LogoPNG.bytes, "image/png"),
                ["/console/out"] = context => context.Response.WriteString(HttpUtility.HtmlEncode(Console.RemoteContent)),
                ["/console/run"] = context => {
                    Console.RunCommand(Uri.UnescapeDataString(context.Request.QueryString.Get("command")));
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.StatusDescription = "OK";
                },
                ["/console/history"] = context => {
                    string index = context.Request.QueryString.Get("index");
                    string previous = null;
                    if (!string.IsNullOrEmpty(index)) {
                        previous = Console.History[int.Parse(index)];
                    }
                    context.Response.WriteString(previous);
                },
                ["/console/complete"] = context => {
                    string partialCommand = context.Request.QueryString.Get("command");
                    string found = null;
                    if (partialCommand != null) {
                        found = Console.Completer.Complete(partialCommand);
                    }
                    context.Response.WriteString(found);
                }
            };

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
            if (Console == null) {
                Console = FindObjectOfType<TildeConsole>();
            }
        }
    }
}