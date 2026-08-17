using System;
using DragonAR.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DragonAR.AR
{
    // Quet mat phang nam ngang (san/ban) trong _scanSeconds giay ke tu luc thay mat phang
    // DAU TIEN, sau do chon mat phang co DIEN TICH LON NHAT trong tat ca da thay - khong
    // phai mat phang thay dau tien - vi ARCore/ARKit hay tach 1 cai ban/san thanh nhieu manh
    // nho luc dau roi gop lai sau; neu khoa cung vao mat dau tien co the dinh phai 1 manh
    // chua gop xong. Raise SurfaceReady dung 1 lan voi thong tin be mat da chuan hoa
    // (TrackableSurfaceInfo) de cac module khac (Creature/UI) khong bao gio phai dung truc
    // tiep kieu ARPlane cua AR Foundation.
    [RequireComponent(typeof(ARPlaneManager))]
    public sealed class SurfaceTrackingManager : MonoBehaviour
    {
        [SerializeField] private float _scanSeconds = 5f;

        public event Action<TrackableSurfaceInfo> SurfaceReady;

        private ARPlaneManager _planeManager;
        private bool _hasSeenAnyHorizontalPlane;
        private float _scanTimer;
        private bool _hasFired;

        private void Awake()
        {
            _planeManager = GetComponent<ARPlaneManager>();
        }

        // Tim ARPlaneManager co san trong scene (thuong nam tren XR Origin) va gan
        // SurfaceTrackingManager vao dung GameObject do. Dat o day (khong phai o
        // DragonAR.App) de App khong bao gio phai dung truc tiep kieu ARPlaneManager cua
        // AR Foundation - giu dung huong phu thuoc mot chieu App -> AR -> Core.
        public static bool TryCreate(out SurfaceTrackingManager tracker)
        {
            var planeManager = UnityEngine.Object.FindAnyObjectByType<ARPlaneManager>();
            if (planeManager == null)
            {
                tracker = null;
                return false;
            }

            tracker = planeManager.gameObject.AddComponent<SurfaceTrackingManager>();
            return true;
        }

        private void OnEnable()
        {
            _planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnDisable()
        {
            _planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (_hasSeenAnyHorizontalPlane || _hasFired)
            {
                return;
            }

            foreach (var plane in args.added)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    continue; // bo qua tuong/mat huong xuong, chi lay san/ban
                }

                _hasSeenAnyHorizontalPlane = true;
                _scanTimer = 0f;
                return;
            }
        }

        private void Update()
        {
            if (!_hasSeenAnyHorizontalPlane || _hasFired)
            {
                return;
            }

            _scanTimer += Time.deltaTime;
            if (_scanTimer < _scanSeconds)
            {
                return;
            }

            var bestPlane = FindLargestHorizontalPlane();
            if (bestPlane == null)
            {
                Debug.LogWarning("[SurfaceTrackingManager] Khong con mat phang nao hop le sau khi quet xong.");
                return;
            }

            _hasFired = true;
            SurfaceReady?.Invoke(BuildSurfaceInfo(bestPlane));
        }

        // Duyet tat ca mat phang dang duoc theo doi (khong chi mat phang thay dau tien),
        // chon mat phang nam ngang huong len co dien tich lon nhat.
        private ARPlane FindLargestHorizontalPlane()
        {
            ARPlane best = null;
            var bestArea = -1f;

            foreach (var plane in _planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    continue;
                }

                var area = plane.size.x * plane.size.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = plane;
                }
            }

            return best;
        }

        private static TrackableSurfaceInfo BuildSurfaceInfo(ARPlane plane)
        {
            return new TrackableSurfaceInfo(
                plane.center,
                plane.normal,
                ComputeUserRelativeRight(plane),
                plane.size);
        }

        // Truc "phai" lay theo huong camera thuc te (khong phai plane.transform.right - he
        // truc noi bo AR Foundation tu gan cho mat phang, co the vo tinh chi theo chieu sau
        // thay vi ngang, gay ra loi 2 vat dung truoc-sau thay vi ngang hang).
        private static Vector3 ComputeUserRelativeRight(ARPlane plane)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return plane.transform.right;
            }

            var projectedRight = Vector3.ProjectOnPlane(camera.transform.right, plane.normal);
            return projectedRight.sqrMagnitude > 0.0001f ? projectedRight.normalized : plane.transform.right;
        }

        // Goi sau khi da dung noi dung len be mat - dung detect them mat phang moi va an het
        // mesh/luoi highlight cua cac mat phang da phat hien, de UI sach hon.
        public void StopDetectionAndHideVisuals()
        {
            foreach (var plane in _planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }

            _planeManager.enabled = false;
        }
    }
}
