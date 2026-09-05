using UnityEngine;
using UnityEngine.EventSystems;

namespace DragonAR.UI
{
    // Keo tha bang ngon tay -> doi VI TRI THE GIOI THAT (transform.position cua canvas),
    // khong phai vi tri tren man hinh. Cach lam: ban 1 tia tu camera qua diem dang cham,
    // giao voi 1 mat phang huong ve camera di qua vi tri HIEN TAI cua panel - lay diem giao
    // do lam vi tri moi. Sau khi tha tay, panel dung yen (fix) dung tai toa do 3D moi do,
    // khong quay lai vi tri cu.
    //
    // Dung chung cho moi panel world-space. Phai gan len 1 GameObject CO Graphic (Image /
    // RawImage) thi moi nhan duoc tia cham tu GraphicRaycaster.
    public sealed class WorldPanelDragHandler : MonoBehaviour, IDragHandler
    {
        private Transform _worldTransform;
        private Camera _fallbackCamera;

        public void Init(Transform worldTransform, Camera fallbackCamera)
        {
            _worldTransform = worldTransform;
            _fallbackCamera = fallbackCamera;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : _fallbackCamera;
            if (cam == null || _worldTransform == null)
            {
                return;
            }

            var ray = cam.ScreenPointToRay(eventData.position);
            var plane = new Plane(-cam.transform.forward, _worldTransform.position);
            if (plane.Raycast(ray, out var distanceAlongRay))
            {
                _worldTransform.position = ray.GetPoint(distanceAlongRay);
            }
        }
    }
}
