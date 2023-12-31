using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using NativeWebSocket;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AsyncWebSocket
{
    /// <summary>
    /// jp: WebSocketの非同期処理を行うクラス
    /// en: Class that performs asynchronous processing of WebSocket
    /// </summary>
    public class WebSocketAsyncTrigger : IDisposable
    {
        private WebSocket _webSocket;
        private Channel<AsyncUnit> _onOpenedHandler;
        private Channel<byte[]> _onReceivedHandler;
        private Channel<string> _onErrorHandler;
        private Channel<WebSocketCloseCode> _onClosedHandler;

        #region static member

        private static CancellationTokenSource _updateCts;
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
        public static WebSocketAsyncTrigger GetOrCreate(string uri, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_updateCts == null)
            {
                _updateCts = new CancellationTokenSource();
                _webSocketMap ??= new Dictionary<string, WebSocketAsyncTrigger>();

                var go = new GameObject(nameof(WebSocketAsyncTrigger));
                go.GetAsyncUpdateTrigger().Subscribe(_ =>  OnUpdate());
                Object.DontDestroyOnLoad(go);
            }


            if (_webSocketMap.TryGetValue(uri, out var websocket))
            {
                return websocket;
            }

            WebSocketAsyncTrigger genWs = new WebSocketAsyncTrigger();
            _webSocketMap[uri] = genWs;

            genWs.Initialize(uri, ct);

            return genWs;
        }

        private static void OnUpdate()
        {
            if (_webSocketMap != null)
            {
                foreach (var (_, ws) in _webSocketMap)
                {
                    ws._webSocket.DispatchMessageQueue();
                }
            }
        }

        #endregion

        private WebSocketAsyncTrigger()
        {
        }

        private void Initialize(string uri, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _webSocket = new WebSocket(uri);
            _onOpenedHandler = Channel.CreateSingleConsumerUnbounded<AsyncUnit>();
            _onReceivedHandler = Channel.CreateSingleConsumerUnbounded<byte[]>();
            _onErrorHandler = Channel.CreateSingleConsumerUnbounded<string>();
            _onClosedHandler = Channel.CreateSingleConsumerUnbounded<WebSocketCloseCode>();

            _webSocket.OnMessage += OnMessaged;
            _webSocket.OnError += OnErrored;
            _webSocket.OnClose += OnClosed;
            _webSocket.OnOpen += OnOpened;

            _webSocket.Connect().AsUniTask().AttachExternalCancellation(ct).Forget();
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
            _onOpenedHandler.Writer.TryWrite(AsyncUnit.Default);
        }

        private void OnMessaged(byte[] message)
        {
            _onReceivedHandler.Writer.TryWrite(message);
        }

        private void OnErrored(string error)
        {
            _onErrorHandler.Writer.TryWrite(error);
        }

        private void OnClosed(WebSocketCloseCode closeCode)
        {
            _onClosedHandler.Writer.TryWrite(closeCode);
        }

        #endregion

        #region publish property

        /// <summary>
        /// jp: WebSocketの接続が開いたときに発行されるイベント
        /// en: Event issued when the WebSocket connection is opened
        /// </summary>
        public IUniTaskAsyncEnumerable<AsyncUnit> OnOpenedAsyncEnumerable(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return _onOpenedHandler.Reader.ReadAllAsync(ct);
        }

        /// <summary>
        /// jp: WebSocketからメッセージを受信したときに発行されるイベント
        /// en: Event issued when a message is received from WebSocket
        /// </summary>
        public IUniTaskAsyncEnumerable<byte[]> OnReceivedAsyncEnumerable(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return _onReceivedHandler.Reader.ReadAllAsync(ct);
        }

        /// <summary>
        /// jp: WebSocketからエラーが発生したときに発行されるイベント
        /// en: Event issued when an error occurs from WebSocket
        /// </summary>
        public IUniTaskAsyncEnumerable<string> OnErrorAsyncEnumerable(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return _onErrorHandler.Reader.ReadAllAsync(ct);
        }

        /// <summary>
        /// jp: WebSocketの接続が閉じたときに発行されるイベント
        /// en: Event issued when the WebSocket connection is closed
        /// </summary>
        public IUniTaskAsyncEnumerable<WebSocketCloseCode> OnClosedAsyncEnumerable(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return _onClosedHandler.Reader.ReadAllAsync(ct);
        }

        #endregion


        /// <summary>
        /// jp: WebSocketのインスタンスを破棄する
        /// en: Destroy WebSocket instance
        /// </summary>
        public void Dispose()
        {
            _webSocket.CancelConnection();
            _webSocket.Close();
            _onOpenedHandler.Writer.TryComplete();
            _onReceivedHandler.Writer.TryComplete();
            _onErrorHandler.Writer.TryComplete();
            _onClosedHandler.Writer.TryComplete();
        }
    }
}