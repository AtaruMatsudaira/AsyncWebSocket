using System.Threading;
using AsyncWebSocket;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SampleSocket : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string uri;

    WebSocketAsyncTrigger _webSocketAsyncTrigger;

    private void Start()
    {
        RunAsync().Forget();
    }

    private async UniTask RunAsync()
    {
        using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken))
        {
            _webSocketAsyncTrigger = WebSocketAsyncTrigger.GetOrCreate(uri, cts.Token);

            Debug.Log("init");
            
            _webSocketAsyncTrigger.OnReceivedAsyncEnumerable(cts.Token).Subscribe(data =>
            {
                string msg = System.Text.Encoding.UTF8.GetString(data);
                Debug.Log(msg);
            }).AddTo(cts.Token);

            _webSocketAsyncTrigger.OnErrorAsyncEnumerable(cts.Token)
                .Subscribe(Debug.LogError)
                .AddTo(cts.Token);
            Debug.Log("start");
            await button.onClick.OnInvokeAsAsyncEnumerable(cts.Token).Take(10).ForEachAwaitAsync(async _ =>
            {
                await _webSocketAsyncTrigger.PublishAsync("test", cts.Token);
            }, cts.Token);
            Debug.Log("end");
        }
    }

    private void OnDestroy()
    {
        _webSocketAsyncTrigger?.Dispose();
    }
}