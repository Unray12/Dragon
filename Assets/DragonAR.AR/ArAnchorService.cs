using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DragonAR.AR
{
    // Neo 1 GameObject vao BAN DO KHONG GIAN cua ARCore/ARKit.
    //
    // Vi sao khong chi dat transform.position roi thoi: he toa do cua phien AR khong dung
    // yen. Khi thiet bi di chuyen, ARCore/ARKit lien tuc tinh lai ban do phong (loop closure,
    // re-localisation) va DICH CHUYEN goc toa do. Vat the chi co toa do se troi dan khoi
    // cho da dat. ARAnchor la cach bao he thong "giu gium diem nay", moi lan ban do duoc
    // hieu chinh thi pose cua anchor cung duoc hieu chinh theo.
    //
    // Nam trong DragonAR.AR vi day la thu duy nhat biet AR Foundation. DragonAR.App goi
    // ham nay, khong tu dung kieu cua AR Foundation.
    public static class ArAnchorService
    {
        public static bool TryAnchor(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (Object.FindAnyObjectByType<ARAnchorManager>() == null)
            {
                Debug.LogWarning("[ArAnchor] Scene khong co ARAnchorManager - vat the se dung " +
                                 "toa do co dinh va CO THE TROI khi AR hieu chinh lai ban do.");
                return false;
            }

            // AR Foundation 6: gan component ARAnchor la du, manager tu nhan.
            target.AddComponent<ARAnchor>();
            return true;
        }
    }
}
