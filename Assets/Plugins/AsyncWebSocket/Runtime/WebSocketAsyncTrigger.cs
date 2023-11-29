using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NativeWebSocket;

namespace AsyncWebSocket
{
    /// <summary>
    /// jp: WebSocketの非同期処理を行うクラス
    /// en: Class that performs asynchronous processing of WebSocket
    /// </summary>
    public class WebSocketAsyncTrigger : IDisposable
    {
        private WebSocket _webSocket;
        private AsyncReactiveProperty<AsyncUnit> _onOpenedHandler;
        private AsyncReactiveProperty<byte[]> _onReceivedHandler;
        private AsyncReactiveProperty<string> _onErrorHandler;
        private AsyncReactiveProperty<WebSocketCloseCode> _onClosedHandler;

        #region publish property

        /// <summary>
        /// jp: WebSocketの接続が開いたときに発行されるイベント
        /// en: Event issued when the WebSocket connection is opened
        /// </summary>
        public IUniTaskAsyncEnumerable<AsyncUnit> OnOpenedAsyncEnumerable => _onOpenedHandler;

        /// <summary>
        /// jp: WebSocketからメッセージを受信したときに発行されるイベント
        /// en: Event issued when a message is received from WebSocket
        /// </summary>
        public IUniTaskAsyncEnumerable<byte[]> OnReceivedAsyncEnumerable => _onReceivedHandler;

        /// <summary>
        /// jp: WebSocketからエラーが発生したときに発行されるイベント
        /// en: Event issued when an error occurs from WebSocket
        /// </summary>
        public IUniTaskAsyncEnumerable<string> OnErrorAsyncEnumerable => _onErrorHandler;

        /// <summary>
        /// jp: WebSocketの接続が閉じたときに発行されるイベント
        /// en: Event issued when the WebSocket connection is closed
        /// </summary>
        public IUniTaskAsyncEnumerable<WebSocketCloseCode> OnClosedAsyncEnumerable => _onClosedHandler;

        #endregion

        #region static member

        private static Dictionary<string, WebSocketAsyncTrigger> _webSocketMap;

        /// <summary>
        /// jp: WebSocketのインスタンスを取得する。存在しない場合は作成
        /// en: Get WebSocket instance or create if not exist
        /// </summary>
        /// <param name="uri">
        /// jp: WebSocketのURI
        /// en: WebSocket URI
        /// </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async UniTask<WebSocketAsyncTrigger> GetOrCreate(string uri, CancellationToken ct = default)
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

        /// <summary>
        /// jp: WebSocketでメッセージを送信する
        /// en: Send message with WebSocket
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ct"></param>
        public async UniTask PublishAsync(string message, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _webSocket.SendText(message).AsUniTask().AttachExternalCancellation(ct);
        }

        /// <summary>
        /// jp: WebSocketでメッセージを送信する
        /// en: Send message with WebSocket
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ct"></param>
        public async UniTask PublishAsync(byte[] message, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _webSocket.Send(message).AsUniTask().AttachExternalCancellation(ct);
        }

        #region events

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

        #endregion

        /// <summary>
        /// jp: WebSocketのインスタンスを破棄する
        /// en: Destroy WebSocket instance
        /// </summary>
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