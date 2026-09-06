using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DragonAR.AR
{
    // Nhan dien NHAN PHANG dan tren tram quan trac (Augmented Image cua ARCore / image
    // detection cua ARKit, goi chung qua ARTrackedImageManager).
    //
    // KHONG CO AI/training o day: AR Foundation nhan 1 asset XRReferenceImageLibrary chua
    // anh nhan + kich thuoc in that, ARCore/ARKit bien dich thanh database luc build roi
    // so khop bang dac trung anh co dien. Khong dataset, khong gan nhan, khong server.
    //
    // Lop nay la ADAPTER: no la noi duy nhat trong app biet AR Foundation ton tai cho viec
    // nhan dien nhan. DragonAR.Creature / DragonAR.UI chi nhan duoc 1 Pose qua su kien
    // (unity-clean-architecture §3) - doi sang Vuforia Model Target sau nay chi la viet 1
    // adapter khac, khong dung toi logic con rong.
    public sealed class ImageTargetTrackingManager : MonoBehaviour
    {
        // Ban CHI phat 1 lan, luc nhan dien duoc nhan lan dau tien. Con rong duoc NEO CO
        // DINH tai toa do do va khong bao gio doi cho nua - ke ca khi nguoi dung soi lai
        // nhan tu goc khac, hay khi nhan bi che.
        public event Action<Pose> TargetAcquiredOnce;

        // Nhan co dang nam trong tam nhin khong - chi dung cho UI huong dan
        // ("Dang tim tram quan trac..."). Da debounce, khong nhap nhay.
        public event Action<bool> TargetVisibilityChanged;

        // Che mat nhan duoi nguong nay thi coi nhu VAN THAY. AR tracking hay chop nhay 1-2
        // frame khi nguoi dung rung tay hoac loa nang; khong debounce thi UI se nhap lien
        // tuc (unity-clean-architecture §5).
        private const float LostGraceSeconds = 1.2f;

        private ARTrackedImageManager _manager;
        private bool _acquired;
        private bool _reportedVisible;
        private float _lastSeenTime = -999f;

        public bool HasAcquired => _acquired;

        // Scene khong co AR (vd scene test trong Editor) thi tra false, khong nem loi.
        public static bool TryCreate(out ImageTargetTrackingManager manager)
        {
            manager = null;

            var tracked = FindAnyObjectByType<ARTrackedImageManager>();
            if (tracked == null)
            {
                Debug.LogWarning("[ImageTarget] Scene khong co ARTrackedImageManager - bo qua nhan dien nhan.");
                return false;
            }

            // AddComponent<T>() chay Awake/OnEnable NGAY LAP TUC (dong bo), TRUOC KHI dong
            // ke tiep kip gan _manager - da gap dung loi nay 2 lan truoc (WebSnapshotSource,
            // ThingsBoardTelemetrySource) nhung lai mac lai o day: neu OnEnable() dang ky
            // listener dua vao field vua duoc AddComponent gan, field do VAN CON NULL luc
            // OnEnable() chay, nen listener khong bao gio duoc dang ky - component im lang
            // vinh vien du ARCore/ARKit co nhan dien duoc anh that su. Day chinh la nguyen
            // nhan khien khong bao gio thay "Da neo con rong" du nhan co bam duoc hay khong.
            //
            // Sua bang cach dang ky trong Init() - goi TUONG MINH sau AddComponent, khong
            // phu thuoc OnEnable() chay dung luc.
            manager = tracked.gameObject.AddComponent<ImageTargetTrackingManager>();
            manager.Init(tracked);
            return true;
        }

        private void Init(ARTrackedImageManager manager)
        {
            _manager = manager;
            _manager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnEnable()
        {
            // Lan dau tien component duoc tao (qua TryCreate/AddComponent), _manager con
            // null tai day - Init() o tren se dang ky listener ngay sau. Nhanh nay chi xu ly
            // truong hop component bi OnDisable() roi enable lai SAU KHI da Init xong.
            if (_manager != null)
            {
                _manager.trackablesChanged.AddListener(OnTrackablesChanged);
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        private void OnTrackablesChanged(
            ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var image in args.added)
            {
                Consider(image);
            }

            foreach (var image in args.updated)
            {
                Consider(image);
            }
        }

        private void Consider(ARTrackedImage image)
        {
            // Chi chap nhan trang thai Tracking. Limited nghia la he thong doan vi tri tu
            // lan thay cuoi - neo con rong vao 1 pose doan mo se lech han so voi tram that.
            if (image.trackingState != TrackingState.Tracking)
            {
                return;
            }

            _lastSeenTime = Time.time;

            if (!_reportedVisible)
            {
                _reportedVisible = true;
                TargetVisibilityChanged?.Invoke(true);
            }

            if (_acquired)
            {
                return;
            }

            _acquired = true;
            TargetAcquiredOnce?.Invoke(new Pose(image.transform.position, image.transform.rotation));
        }

        private void Update()
        {
            if (!_reportedVisible || Time.time - _lastSeenTime < LostGraceSeconds)
            {
                return;
            }

            _reportedVisible = false;
            TargetVisibilityChanged?.Invoke(false);
        }
    }
}
