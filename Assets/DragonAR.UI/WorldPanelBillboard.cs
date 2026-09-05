using UnityEngine;

namespace DragonAR.UI
{
    // Luon xoay panel ve phia camera (chi quanh truc dung) de doc duoc du nguoi dung dung o
    // goc nao quanh no - giong 1 man hinh AR lo lung thuc su.
    //
    // Dung chung cho MOI panel world-space (dashboard so lieu, khung web...). Truoc day day
    // la class private long trong EnvironmentDashboardOverlay; tach ra khi co panel thu hai
    // de khong phai chep lai cong thuc xoay - nhat la cong thuc nay tung sai dau mot lan.
    public sealed class WorldPanelBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            // World Space Canvas hien noi dung khi nguoi xem dung o phia -Z nhin ve +Z
            // (khong phai nguoc lai) - dung "huong ra xa camera" (tu camera toi panel, keo
            // dai them) lam forward, KHONG PHAI "huong ve camera", neu khong chu se bi lat
            // guong (da xac nhan qua anh chup thuc te: "ENVIRONMENT" hien thanh "TNEMNORIVNE").
            var directionAwayFromCamera = transform.position - camera.transform.position;
            directionAwayFromCamera.y = 0f;
            if (directionAwayFromCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(directionAwayFromCamera.normalized, Vector3.up);
        }
    }
}
