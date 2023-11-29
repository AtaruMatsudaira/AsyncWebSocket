using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NativeWebSocket;

namespace AsyncWebSocket
{
    public class WebSocketAsyncTrigger : IDisposable
    {
        private WebSocket _webSocket;
        private AsyncReactiveProperty<AsyncUnit> _onOpenedHandler;
        private AsyncReactiveProperty<byte[]> _onReceivedHandler;
        private AsyncReactiveProperty<string> _onErrorHandler;
        private AsyncReactiveProperty<WebSocketCloseCode> _onClosedHandler;

        #region publish property

        public IUniTaskAsyncEnumerable<AsyncUnit> OnOpenedAsyncEnumerable => _onOpenedHandler;
        public IUniTaskAsyncEnumerable<byte[]> OnReceivedAsyncEnumerable => _onReceivedHandler;
        public IUniTaskAsyncEnumerable<string> OnErrorAsyncEnumerable => _onErrorHandler;
        public IUniTaskAsyncEnumerable<WebSocketCloseCode> OnClosedAsyncEnumerable => _onClosedHandler;

        #endregion

        #region static member

        private static Dictionary<string, WebSocketAsyncTrigger> _webSocketMap;

        public static async UniTask<WebSocketAsyncTrigger> GetOrCreate(string uri, CancellationToken ct)
        {
            if (_webSocketMap.TryGetValue(uri, out var websocket))
            {
                return websocket;
            }

            WebSocketAsyncTrigger genWs = new WebSocketAsyncTrigger();
            _webSocketMap[uri] = genWs;

            await genWs.StartAsync(uri, ct);

            return genWs;
        }

        #endregion

        private WebSocketAsyncTrigger()
        {
        }


        private async UniTask StartAsync(string uri, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _webSocket = new WebSocket(uri);
            _onOpenedHandler = new(default);
            _onReceivedHandler = new(default);
            _onErrorHandler = new(default);
            _onClosedHandler = new(default);

            _webSocket.OnMessage += OnMessaged;
            _webSocket.OnError += OnErrored;
            _webSocket.OnClose += OnClosed;
            _webSocket.OnOpen += OnOpened;

            await _webSocket.Connect().AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask PublishAsync(string message, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _webSocket.SendText(message).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask PublishAsync(byte[] message, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _webSocket.Send(message).AsUniTask().AttachExternalCancellation(ct);
        }

        private void OnOpened()
        {
            _onOpenedHandler.Value = default;
        }

        private void OnMessaged(byte[] message)
        {
            _onReceivedHandler.Value = message;
        }

        private void OnErrored(string error)
        {
            _onErrorHandler.Value = error;
        }

        private void OnClosed(WebSocketCloseCode closeCode)
        {
            _onClosedHandler.Value = closeCode;
        }

        public void Dispose()
        {
            _webSocket.Close();
            _onOpenedHandler.Dispose();
            _onReceivedHandler.Dispose();
            _onErrorHandler.Dispose();
            _onClosedHandler.Dispose();
        }
    }
}