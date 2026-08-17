using UnityEngine;

namespace DragonAR.Core
{
    // Tien ich hinh hoc thuan tuy: scale 1 GameObject (theo tat ca Renderer con) ve 1 kich
    // thuoc muc tieu (canh lon nhat cua bounding box), roi can chinh vi tri sao cho DAY cua
    // bounding box (khong phai tam) trung voi goc toa do hien tai cua no - dung khi can dat
    // 1 vat the dung len tren mat phang thay vi lo lung nua chim nua noi.
    public static class BoundsScaler
    {
        public static void ScaleToFitAndAlignBottom(GameObject target, float targetLargestDimension)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[BoundsScaler] '{target.name}' khong co Renderer nao, bo qua scale.");
                return;
            }

            var bounds = CombinedBounds(renderers);
            var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestDimension <= 0f)
            {
                return;
            }

            var scaleFactor = targetLargestDimension / largestDimension;
            target.transform.localScale *= scaleFactor;

            var scaledBounds = CombinedBounds(target.GetComponentsInChildren<Renderer>());
            var bottomCenter = new Vector3(scaledBounds.center.x, scaledBounds.min.y, scaledBounds.center.z);
            var worldOffset = target.transform.position - bottomCenter;
            target.transform.position += worldOffset;
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
    }
}
