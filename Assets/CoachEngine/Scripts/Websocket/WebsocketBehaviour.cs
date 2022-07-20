using System;
using WebSocketSharp.Server;
using UnityEngine;
using System.Collections.Concurrent;
using System.Net;
using UnityEngine.Events;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;

namespace CoachAiEngine {

    /// <summary>
    /// Enables listening for <see cref="CoachEvent"/>s on a websocket.
    /// </summary>
    /// <remarks>Only plain ws:// is supported</remarks>
    public class WebsocketBehaviour : MonoBehaviour {
        [Tooltip("The ip address to bind to")]
        [SerializeField] private string ip = IPAddress.Any.ToString();
        [Tooltip("The port to listen on")]
        [SerializeField] private int port = 8080;
        [Tooltip("The path to mount the socket handler on")]
        [SerializeField] private string path = "/coach-ai/";
        [Tooltip("Subscribe to public events received via the socket")]
        [SerializeField] private UnityEvent<PublicEvent> publicEventHandler;
        [Tooltip("Subscribe to world object events received via the socket")]
        [SerializeField] private UnityEvent<WorldObjectEvent> worldObjectEventHandler;
        [Tooltip("Subscribe to metrics events received via the socket")]
        [SerializeField] private UnityEvent<MetricsUpdateEvent> metricsEventHandler;
        [Tooltip("Start listening for events as soon as the server is created")]
        [SerializeField] private bool autoStart;
        [Tooltip("Keep the server (and websocket connection) alive during scene changes.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private ConcurrentQueue<CoachEvent> _msgQueue = new ConcurrentQueue<CoachEvent>();
        private WebSocketServer _websocket;
        private bool _suspended;

        private void Awake() {
            var ipAddress = IPAddress.Parse(ip);
            _websocket = new WebSocketServer(ipAddress, port, false);
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start() {
            if (autoStart) StartServer();
        }

        private void Update() => ReceiveMessages();

        private void OnDestroy() => StopServer();

        /// <summary>
        /// Starts the websocket server and begins delivering the received events to subscribers.
        /// </summary>
        public void StartServer() {
            var absolutePath = path.StartsWith("/") ? path : $"/{path}";
            _websocket.AddWebSocketService<WebsocketHandler>(absolutePath,
                    handler => handler.onMessage = msg => EnqueueEvent(msg, handler.Context));
            Debug.Log($"Websocket server started at ws://{ip}:{port}{absolutePath}");
            _websocket.Start();
        }

        /// <summary>
        /// Stops the websocket server. Any events received but not yet
        /// delivered to subscribers will be discarded.
        /// </summary>
        public void StopServer() {
            _websocket.Stop();
            _msgQueue = new ConcurrentQueue<CoachEvent>();
        }

        /// <summary>
        /// Stop delivering events to subscribers. Any events received
        /// from the socket while suspended will be discarded.
        /// </summary>
        public void SuspendServer() => _suspended = true;

        /// <summary>
        /// Resume delivering events to subscribers.
        /// </summary>
        public void ResumeServer() => _suspended = false;

        private void EnqueueEvent(string json, WebSocketContext context) {
            if (_suspended) return;

            try {
                var @event = CoachEvent.FromJson(json);
                _msgQueue.Enqueue(@event);
            } catch (Exception ex) {
                Debug.LogError($"Received an invalid message from {context.Origin}", this);
                Debug.LogException(ex, this);
            }
        }

        private void ReceiveMessages() {
            while (!_msgQueue.IsEmpty) {
                _msgQueue.TryDequeue(out var @event);
                switch (@event) {
                    case PublicEvent e:
                        publicEventHandler.Invoke(e);
                        break;
                    case WorldObjectEvent e:
                        worldObjectEventHandler.Invoke(e);
                        break;
                    case MetricsUpdateEvent e:
                        metricsEventHandler.Invoke(e);
                        break;
                }
            }
        }

        private class WebsocketHandler : WebSocketBehavior {
            internal Action<string> onMessage = ErrorNoHandlerFunction;

            protected override void OnOpen() {
                Debug.Log($"Establishing connection with {Context.Origin}");
                Send(@"{""connect"": true}");
            }

            protected override void OnMessage(MessageEventArgs e) => onMessage.Invoke(e.Data);

            private static void ErrorNoHandlerFunction(string msg) =>
                Debug.LogError("Error, did you forget to provide a message handler??");
        }
    }
}
