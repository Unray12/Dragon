using System;
using System.Collections;
using DragonAR.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonAR.Telemetry
{
    // Tai anh chup cua 1 trang web ve theo chu ky va ban ra qua IWebSnapshotSource.
    //
    // Nam trong DragonAR.Telemetry vi day cung la "lay du lieu tu xa qua mang" - dung chung
    // module voi ThingsBoard thay vi de rieng 1 asmdef cho dung 1 class.
    //
    // Ben server can co 1 endpoint tra ve PNG/JPG cua trang. Cach don gian nhat: them 1
    // route vao chinh portal Next.js dung Playwright, cache lai vai giay:
    //
    //   GET /api/kiosk-shot.png  ->  chup https://.../kiosk o 720x1280, cache 5s
    //
    // Hoac chay ngoai bang Chrome headless roi ghi ra file tinh:
    //   chrome --headless=new --window-size=720,1280 --virtual-time-budget=12000 \
    //          --screenshot=kiosk.png https://dragoneden-portal.dsolution.net/kiosk
    public sealed class WebSnapshotSource : MonoBehaviour, IWebSnapshotSource
    {
        public event Action<Texture2D> SnapshotReceived;
        public event Action<string> SnapshotFailed;

        private string _url;
        private float _refreshSeconds;
        private Texture2D _current;
        private int _consecutiveFailures;

        public void Configure(string url, float refreshSeconds)
        {
            _url = url;
            _refreshSeconds = Mathf.Max(1f, refreshSeconds);
        }

        private void OnEnable()
        {
            StartCoroutine(RefreshLoop());
        }

        private void OnDestroy()
        {
            // Texture tai bang UnityWebRequestTexture khong thuoc GC quan ly - khong huy tay
            // thi moi lan lam moi lai bo lai 1 texture 720x1280 trong bo nho.
            if (_current != null)
            {
                Destroy(_current);
            }
        }

        private IEnumerator RefreshLoop()
        {
            // AddComponent<T>() chay OnEnable NGAY, truoc khi nguoi goi kip Configure().
            // Cho toi khi co URL thay vi tu tat component - neu tu tat thi Configure() goi
            // sau cung vo nghia, coroutine khong bao gio chay (loi da gap that).
            while (string.IsNullOrWhiteSpace(_url))
            {
                yield return null;
            }

            while (true)
            {
                yield return FetchOnce();
                yield return new WaitForSeconds(_refreshSeconds);
            }
        }

        private IEnumerator FetchOnce()
        {
            // Them tham so chong cache: CDN (Cloudflare) hoac cache cua UnityWebRequest co
            // the tra lai dung anh cu, khung se dung im du server da co anh moi.
            var url = _url + (_url.Contains("?") ? "&" : "?") + "t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures == 1)
                {
                    Debug.LogWarning($"[WebSnapshot] Tai anh that bai ({request.responseCode}): {request.error}");
                }

                SnapshotFailed?.Invoke($"Không tải được ảnh trang ({request.responseCode})");
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                SnapshotFailed?.Invoke("Phản hồi không phải ảnh hợp lệ");
                yield break;
            }

            if (_consecutiveFailures > 0)
            {
                Debug.Log("[WebSnapshot] Da tai lai duoc anh trang.");
                _consecutiveFailures = 0;
            }

            var previous = _current;
            _current = texture;
            SnapshotReceived?.Invoke(texture);

            if (previous != null)
            {
                Destroy(previous);
            }
        }
    }
}
