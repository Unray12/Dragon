using UnityEngine;

namespace DragonAR.UI
{
    // Tu tao 1 Sprite bo goc tron bang code (khong phu thuoc builtin resource nao cua
    // Unity) - Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd") KHONG ton
    // tai trong Unity 6000.5.8f1 (bao loi runtime "resource could not be loaded", phat
    // hien qua Unity MCP doc Console). Dung chung 1 sprite (cache static), tinted mau khac
    // nhau qua Image.color, 9-slice (border = ban kinh bo goc) de scale dung voi bat ky
    // kich thuoc panel/card nao ma khong bi meo goc.
    internal static class RoundedRectSpriteFactory
    {
        private const int TextureSize = 64;
        private const float CornerRadius = 18f;

        private static Sprite _cachedSprite;

        public static Sprite GetShared()
        {
            if (_cachedSprite != null)
            {
                return _cachedSprite;
            }

            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var half = TextureSize * 0.5f;

            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var alpha = RoundedRectCoverage(x + 0.5f - half, y + 0.5f - half, half, CornerRadius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            var border = CornerRadius + 2f;
            _cachedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));

            return _cachedSprite;
        }

        // Cong thuc SDF hinh chu nhat bo goc chuan (Inigo Quilez) - alpha=1 ben trong, mo
        // dan (anti-alias ~1px) o vien, alpha=0 ben ngoai.
        private static float RoundedRectCoverage(float px, float py, float halfExtent, float radius)
        {
            var qx = Mathf.Abs(px) - (halfExtent - radius);
            var qy = Mathf.Abs(py) - (halfExtent - radius);

            var outsideDistance = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            var insideDistance = Mathf.Min(Mathf.Max(qx, qy), 0f);
            var distance = outsideDistance + insideDistance - radius;

            return Mathf.Clamp01(0.5f - distance);
        }
    }
}
