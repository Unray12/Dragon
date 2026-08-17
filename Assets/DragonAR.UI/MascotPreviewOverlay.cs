using UnityEngine;
using UnityEngine.UI;

namespace DragonAR.UI
{
    // Hien anh 2D tham chieu mascot (img-mascos-1.png) o GIUA MAN HINH, tu lac lu tai cho
    // (xoay qua lai nhe quanh truc Z, khong doi vi tri) - dung de xem thu thiet ke mascot
    // ngay trong app truoc khi co model 3D dung thiet ke nay (xem
    // context/OPEN_QUESTIONS.md). Day la Screen Space overlay don gian, KHONG neo vao
    // khong gian AR va KHONG can tuong tac cham - chi hien thi + tu lac lu.
    public static class MascotPreviewOverlay
    {
        private const string SpriteResourcePath = "MascotReference";
        private const float ImageSize = 420f;
        private const float SwayAmplitudeDegrees = 8f;
        private const float SwayPeriodSeconds = 2.2f;

        public static void Spawn()
        {
            var sprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (sprite == null)
            {
                Debug.LogError($"[MascotPreviewOverlay] Sprite not found at Resources/{SpriteResourcePath}");
                return;
            }

            var canvasGo = new GameObject("MascotPreviewCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var imageGo = new GameObject("MascotImage", typeof(RectTransform));
            imageGo.transform.SetParent(canvasGo.transform, false);

            var rect = imageGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ImageSize, ImageSize);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = imageGo.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            imageGo.AddComponent<SwayInPlace>();
        }

        // Xoay qua lai nhe quanh truc Z theo song sin - "lac lu tai cho", khong dich
        // chuyen vi tri.
        private sealed class SwayInPlace : MonoBehaviour
        {
            private float _angularFrequency;

            private void Awake()
            {
                _angularFrequency = Mathf.PI * 2f / SwayPeriodSeconds;
            }

            private void Update()
            {
                var angle = Mathf.Sin(Time.time * _angularFrequency) * SwayAmplitudeDegrees;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}
