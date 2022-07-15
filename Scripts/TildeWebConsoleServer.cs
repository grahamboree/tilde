using UnityEngine;
using System;
using System.Net;
using System.Collections.Generic;
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
        
        static HttpListener listener = new();
        static Dictionary<string, Action<RequestContext>> routes;
        static readonly Queue<Action> responseQueue = new();

        /// HttpListener callback.  This happens on a background thread, so we need to queue response generation for the main thread.
        static void ListenerCallback(IAsyncResult result) {
            var context = new RequestContext(listener.EndGetContext(result));
            
            try {
                if (context.Request.HttpMethod is "GET" or "HEAD" && routes.TryGetValue(context.Path, out var handler)) {
                    // Queue a closure for main thread execution
                    lock (responseQueue) {
                        responseQueue.Enqueue(() => {
                            handler(context);
                            context.Response.Close();
                        });
                    }
                } else {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusDescription = "Not Found";
                    context.Response.Close();
                }
            } catch (Exception exception) {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = $"Fatal error:\n{exception}";
                context.Response.Close();
                Debug.LogException(exception);
            }
            
            if (listener.IsListening) {
                listener.BeginGetContext(ListenerCallback, null);
            }
        }

        void Reset() {
            if (Console == null) {
                Console = FindObjectOfType<TildeConsole>();
            }
        }

        public void Awake() {
            if (Console == null) {
                Console = FindObjectOfType<TildeConsole>();
            }
            
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
            lock (responseQueue) {
                while (responseQueue.Count != 0) {
                    responseQueue.Dequeue()();
                }
            }
        }
        
        void OnDestroy() {
            listener.Close();
            listener = null;
        }
    }
}