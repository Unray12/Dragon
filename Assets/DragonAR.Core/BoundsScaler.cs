using UnityEngine;

namespace DragonAR.Core
{
    // Tien ich hinh hoc thuan tuy: scale 1 GameObject (theo tat ca Renderer con) ve 1 kich
    // thuoc muc tieu (canh lon nhat cua bounding box), roi can chinh vi tri theo 1 trong 2
    // cach: can DAY (dung tren mat phang) hoac can TAM (lo lung truoc camera, khong dung
    // tren be mat nao).
    public static class BoundsScaler
    {
        // Can day cua bounding box (khong phai tam) trung voi goc toa do hien tai cua no -
        // dung khi can dat 1 vat the dung len tren mat phang thay vi lo lung nua chim nua
        // noi.
        public static void ScaleToFitAndAlignBottom(GameObject target, float targetLargestDimension)
        {
            if (!TryScale(target, targetLargestDimension, out var scaledBounds))
            {
                return;
            }

            var bottomCenter = new Vector3(scaledBounds.center.x, scaledBounds.min.y, scaledBounds.center.z);
            var worldOffset = target.transform.position - bottomCenter;
            target.transform.position += worldOffset;
        }

        // Can TAM cua bounding box trung voi goc toa do hien tai - dung khi vat the lo
        // lung (vd gan theo camera), khong dung tren be mat nao.
        public static void ScaleToFitAndCenter(GameObject target, float targetLargestDimension)
        {
            if (!TryScale(target, targetLargestDimension, out var scaledBounds))
            {
                return;
            }

            var worldOffset = target.transform.position - scaledBounds.center;
            target.transform.position += worldOffset;
        }

        private static bool TryScale(GameObject target, float targetLargestDimension, out Bounds scaledBounds)
        {
            scaledBounds = default;

            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[BoundsScaler] '{target.name}' has no Renderer, skipping scale.");
                return false;
            }

            var bounds = CombinedBounds(renderers);
            var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestDimension <= 0f)
            {
                return false;
            }

            var scaleFactor = targetLargestDimension / largestDimension;
            target.transform.localScale *= scaleFactor;

            scaledBounds = CombinedBounds(target.GetComponentsInChildren<Renderer>());
            return true;
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            var bounds = WorldBounds(renderers[0]);
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(WorldBounds(renderers[i]));
            }
            return bounds;
        }

        // SkinnedMeshRenderer.bounds la bounds DA DUOC NOI RONG de phu moi tu the animation,
        // khong phai kich thuoc that cua model. Voi con rong: 1.19m noi rong so voi 0.92m
        // that -> dung thang no de scale se lam model nho di ~29%. sharedMesh.bounds moi la
        // kich thuoc that cua bind pose.
        private static Bounds WorldBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                return TransformBounds(skinned.sharedMesh.bounds, renderer.transform.localToWorldMatrix);
            }

            return renderer.bounds;
        }

        // Bien doi bounds cuc bo sang the gioi bang cach doi 8 dinh hop roi bao lai - khong
        // the nhan thang center/extents vi phep xoay lam hop khong con thang truc.
        private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
        {
            var c = local.center;
            var e = local.extents;
            var result = new Bounds(matrix.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);

            for (var i = 1; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                result.Encapsulate(matrix.MultiplyPoint3x4(c + corner));
            }

            return result;
        }
    }
}
