using System;
using DragonAR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonAR.UI
{
    // Khung hien trang web trong khong gian AR. Neo vao 1 toa do THAT, tu xoay mat ve camera
    // (WorldPanelBillboard) va KEO THA duoc bang ngon tay y het dashboard
    // (WorldPanelDragHandler) - dung chung 2 component do, khong chep lai.
    //
    // Noi dung la ANH CHUP cua trang, lam moi theo chu ky (xem IWebSnapshotSource de biet
    // vi sao khong phai trang web song). Panel khong biet anh tu dau ra - doi sang Vuplex
    // sau nay chi la thay implementation cua IWebSnapshotSource.
    public static class WebPanelOverlay
    {
        // Ban dau lay theo ti le anh kiosk 720x1280 (doc). Chieu cao se duoc tinh lai theo
        // ti le that cua anh dau tien nhan duoc, nen doi do phan giai endpoint khong lam
        // meo hinh.
        private const float PanelWidth = 340f;
        private const float DefaultPanelHeight = 604f;

        // Cung ti le quy doi voi dashboard => 2 panel cung "co" trong khong gian.
        private const float WorldUnitsPerPixel = 0.0012f;

        private const float ImagePadding = 8f;
        private const float CaptionHeight = 22f;

        private static readonly Color FrameColor = new(0.06f, 0.09f, 0.11f, 0.55f);
        private static readonly Color CaptionInkColor = new(0.75f, 0.79f, 0.82f, 1f);

        public static void Spawn(Vector3 worldPosition, IWebSnapshotSource source)
        {
            if (source == null)
            {
                Debug.LogError("[WebPanel] Thieu nguon anh, khong spawn.");
                return;
            }

            var camera = Camera.main;

            var canvasGo = new GameObject("WebPanelCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(PanelWidth, DefaultPanelHeight);
            canvasGo.transform.localScale = Vector3.one * WorldUnitsPerPixel;

            canvasGo.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("WebPanelRoot", typeof(RectTransform));
            rootGo.transform.SetParent(canvasGo.transform, false);

            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Khung vien toi bao quanh anh: vua lam vien cho anh noi bat, vua la vung nhan
            // tia cham de keo tha (Image nay phai ton tai thi GraphicRaycaster moi bat duoc).
            var frame = rootGo.AddComponent<Image>();
            frame.sprite = RoundedRectSpriteFactory.GetShared();
            frame.type = Image.Type.Sliced;
            frame.color = FrameColor;

            var dragHandler = rootGo.AddComponent<WorldPanelDragHandler>();
            dragHandler.Init(canvasGo.transform, camera);

            // Dat tuyet doi thay vi dung LayoutGroup: bo cuc o day chi co 2 phan tu co dinh,
            // dung layout group chi them cho de sai (da tung dinh loi chong lan o dashboard).
            var imageGo = new GameObject("Snapshot", typeof(RectTransform));
            imageGo.transform.SetParent(rootGo.transform, false);
            var imageRect = imageGo.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(ImagePadding, ImagePadding + CaptionHeight);
            imageRect.offsetMax = new Vector2(-ImagePadding, -ImagePadding);

            var rawImage = imageGo.AddComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false; // de tia cham roi xuong frame -> keo tha muot

            var captionGo = new GameObject("Caption", typeof(RectTransform));
            captionGo.transform.SetParent(rootGo.transform, false);
            var captionRect = captionGo.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 0f);
            captionRect.pivot = new Vector2(0.5f, 0f);
            captionRect.offsetMin = new Vector2(ImagePadding + 4f, 4f);
            captionRect.offsetMax = new Vector2(-(ImagePadding + 4f), 4f + CaptionHeight);

            var caption = captionGo.AddComponent<TextMeshProUGUI>();
            caption.text = "Đang tải trang…";
            caption.fontSize = 12;
            caption.color = CaptionInkColor;
            caption.alignment = TextAlignmentOptions.Left;
            caption.raycastTarget = false;

            var driver = rootGo.AddComponent<WebPanelDriver>();
            driver.Init(source, rawImage, caption, canvasRect);

            canvasGo.transform.position = worldPosition;
            canvasGo.AddComponent<WorldPanelBillboard>();
        }

        // Nhan anh moi tu nguon, dap len RawImage va cap nhat dong chu thich. Tu chinh lai
        // chieu cao panel theo ti le anh that - endpoint tra 720x1280 hay 1080x1920 deu
        // khong bi meo.
        private sealed class WebPanelDriver : MonoBehaviour
        {
            private const float CaptionRefreshSeconds = 1f;

            private IWebSnapshotSource _source;
            private RawImage _image;
            private TextMeshProUGUI _caption;
            private RectTransform _canvasRect;

            private DateTime _lastSnapshotUtc = DateTime.MinValue;
            private string _lastError;
            private string _lastCaption;
            private float _timer;

            public void Init(IWebSnapshotSource source, RawImage image, TextMeshProUGUI caption, RectTransform canvasRect)
            {
                _source = source;
                _image = image;
                _caption = caption;
                _canvasRect = canvasRect;

                _source.SnapshotReceived += OnSnapshotReceived;
                _source.SnapshotFailed += OnSnapshotFailed;
            }

            private void OnDestroy()
            {
                if (_source != null)
                {
                    _source.SnapshotReceived -= OnSnapshotReceived;
                    _source.SnapshotFailed -= OnSnapshotFailed;
                }
            }

            private void OnSnapshotReceived(Texture2D texture)
            {
                _image.texture = texture;
                _lastSnapshotUtc = DateTime.UtcNow;
                _lastError = null;

                if (texture.width > 0)
                {
                    var aspect = (float)texture.height / texture.width;
                    var contentWidth = PanelWidth - ImagePadding * 2f;
                    _canvasRect.sizeDelta = new Vector2(
                        PanelWidth,
                        contentWidth * aspect + ImagePadding * 2f + CaptionHeight);
                }

                RefreshCaption();
            }

            private void OnSnapshotFailed(string message)
            {
                _lastError = message;
                RefreshCaption();
            }

            private void Update()
            {
                _timer += Time.deltaTime;
                if (_timer < CaptionRefreshSeconds)
                {
                    return;
                }

                _timer = 0f;
                RefreshCaption();
            }

            private void RefreshCaption()
            {
                string next;
                if (_lastError != null)
                {
                    next = "⚠ " + _lastError;
                }
                else if (_lastSnapshotUtc == DateTime.MinValue)
                {
                    next = "Đang tải trang…";
                }
                else
                {
                    next = $"dragoneden-portal · {(int)(DateTime.UtcNow - _lastSnapshotUtc).TotalSeconds}s trước";
                }

                // Chi gan khi chuoi doi - gan text cho TMP lam no dung lai ca mesh.
                if (_lastCaption == next)
                {
                    return;
                }

                _lastCaption = next;
                _caption.text = next;
            }
        }
    }
}
