using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// TAM THOI - chi de test nhanh model rong vua tai ve, khong phai kien truc chinh thuc
// cua project (xem docs/ARCHITECTURE.md va .claude/skills/unity-clean-architecture).
// Cho den khi phat hien mat phang nam ngang (san/ban), spawn 2 con rong 2 ben (chua cho
// giua cho item chinh), moi con tu phoi hop 4 animation co san theo vong lap (lech pha
// nhau), va thinh thoang quay mat nhin ve phia con kia/tam giua de mo phong tuong tac
// (file goc khong co animation tuong tac rieng). Xoa file nay + Assets/Resources/Dragon/
// khi bat dau implement CreatureBehaviorController/OrbitMotion that (xem
// context/OPEN_QUESTIONS.md).
public static class DragonSmokeTestBootstrap
{
    private const string PrefabResourcePath = "Dragon/BGE_Dragon_2.7_Animation_Only";
    private const string ControllerResourcePath = "Dragon/BGE_Dragon_2.7_Animation_Only";

    // Kich thuoc/khoang cach gio tinh theo kich thuoc THAT cua mat phang phat hien duoc
    // (plane.size), khong con la hang so co dinh - mat phang nho (ban con) ra rong nho +
    // gan nhau, mat phang lon (san nha) ra rong to + xa nhau hon, nhung luon bi gioi han
    // trong khoang Min/Max de: (1) khong qua nho khi mat phang be, (2) khong qua to/xa
    // toi muc troi khoi khung hinh camera khi mat phang rat lon.
    private const float VisualSizeFractionOfPlane = 1.4f; // % canh nho hon cua mat phang dung lam kich thuoc rong
    private const float MinVisualSize = 0.8f; // met
    private const float MaxVisualSize = 8f; // met

    private const float SideOffsetFractionOfPlaneWidth = 0.16f; // % chieu rong mat phang dung lam khoang cach tam
    private const float MinSideOffsetMultiplierOfSize = 0.35f; // toi thieu = % kich thuoc rong, tranh 2 con de len nhau o giua
    private const float MaxSideOffset = 1.6f; // met

    // Ten state duy nhat da wire san trong Animator Controller di kem file (chi co 1 state
    // "Run"). Ta doi CLIP gan vao state nay (qua AnimatorOverrideController) de phat lan
    // luot ca 4 animation, thay vi phai dung nhieu state/transition that (khong the tao
    // bang tay vi file .controller la dinh dang nhi phan, khong sua truc tiep duoc).
    private const string BaseStateName = "Armature|Run_New";

    // Thu tu phoi hop 4 animation co san trong file, lap lai vo han.
    private static readonly string[] AnimationOrder =
    {
        "Armature|Fly_New",
        "Armature|Walk_New",
        "Armature|Run_New",
        "Armature|Idel_New",
    };

    private const float SecondsPerAnimation = 4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        HideTemplateUi();

        var planeManager = Object.FindAnyObjectByType<ARPlaneManager>();
        if (planeManager == null)
        {
            Debug.LogError("[DragonSmokeTest] Khong tim thay ARPlaneManager trong scene.");
            return;
        }

        var waiter = new GameObject("TEMP_DragonPlaneWaiter").AddComponent<PlaneWaiter>();
        waiter.Init(planeManager);
    }

    private static void HideTemplateUi()
    {
        var templateUiRoot = GameObject.Find("UI");
        if (templateUiRoot != null)
        {
            templateUiRoot.SetActive(false);
        }
    }

    // Cho toi khi co it nhat 1 mat phang nam ngang huong len (san/ban, khong phai tuong)
    // duoc phat hien, roi spawn 2 con rong 2 ben - chi lam 1 lan cho lan phat hien dau tien.
    private sealed class PlaneWaiter : MonoBehaviour
    {
        // Cho nguoi dung lia camera quet xung quanh trong khoang thoi gian nay TRUOC KHI
        // chon mat phang - ARCore/ARKit co the tach 1 mat ban/san thanh nhieu mat phang nho
        // luc dau roi gop lai sau, va plane.size luc moi phat hien luon rat nho. Sau khi het
        // gio, chon mat phang NGANG LON NHAT trong tat ca mat phang da thay (khong phai mat
        // phang dau tien tinh co thay) - dung tinh than "mat phang cuoi cung sau khi quet
        // xong", vi AR Foundation khong co khai niem "mat phang cuoi" ro rang, chi co the
        // suy ra bang cach chon mat day du nhat sau ca khoang thoi gian quet.
        private const float ScanSeconds = 5f;

        private ARPlaneManager _planeManager;
        private bool _hasSeenAnyHorizontalPlane;
        private float _scanTimer;

        public void Init(ARPlaneManager planeManager)
        {
            _planeManager = planeManager;
            _planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            Debug.Log("[DragonSmokeTest] Dang cho phat hien mat phang - di chuyen may quet quanh san/ban...");
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (_hasSeenAnyHorizontalPlane)
            {
                return; // da bat dau dem gio quet roi, khong can xu ly gi them o day
            }

            foreach (var plane in args.added)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    continue; // bo qua tuong/mat huong xuong, chi lay san/ban
                }

                _hasSeenAnyHorizontalPlane = true;
                _scanTimer = 0f;
                Debug.Log($"[DragonSmokeTest] Da thay mat phang dau tien, tiep tuc quet them {ScanSeconds}s...");
                return;
            }
        }

        private void Update()
        {
            if (!_hasSeenAnyHorizontalPlane)
            {
                return;
            }

            _scanTimer += Time.deltaTime;
            if (_scanTimer < ScanSeconds)
            {
                return;
            }

            _planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);

            var bestPlane = FindLargestHorizontalPlane();
            if (bestPlane == null)
            {
                Debug.LogWarning("[DragonSmokeTest] Khong con mat phang nao hop le sau khi quet xong, huy.");
                Destroy(gameObject);
                return;
            }

            SpawnTwoDragons(bestPlane);
            StopPlaneDetectionAndHideVisuals(_planeManager);
            Destroy(gameObject);
        }

        // Sau khi da spawn xong 2 con rong, khong can detect them mat phang moi nua, va an
        // luon cac mesh/luoi highlight danh dau mat phang da phat hien (de UI sach hon,
        // khong con luoi/vien mau che len camera). An tung GameObject cua plane truoc (thay
        // vi xoa) roi moi tat ARPlaneManager - tat manager truoc co the khong don sach cac
        // plane da instantiate tuy version, nen chu dong an het cho chac.
        private static void StopPlaneDetectionAndHideVisuals(ARPlaneManager planeManager)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }

            planeManager.enabled = false;
        }

        // Duyet tat ca mat phang dang duoc theo doi (khong chi mat phang thay dau tien),
        // chon mat phang nam ngang huong len co dien tich lon nhat - day la mat phang "day
        // du nhat" sau ca qua trinh quet, gan dung voi y "mat phang cuoi cung".
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

        private static void SpawnTwoDragons(ARPlane plane)
        {
            ComputeResponsiveSizing(plane, out var visualSize, out var sideOffset);

            var right = ComputeUserRelativeRight(plane);

            var leftPivot = SpawnOneDragon(plane, -right * sideOffset, visualSize, startAnimationIndex: 0);
            var rightPivot = SpawnOneDragon(plane, right * sideOffset, visualSize, startAnimationIndex: 2);

            if (leftPivot == null || rightPivot == null)
            {
                return;
            }

            var leftSeq = leftPivot.GetComponent<DragonAnimationSequencer>();
            var rightSeq = rightPivot.GetComponent<DragonAnimationSequencer>();
            leftSeq.SetInteractionPartner(rightPivot.transform, plane.center);
            rightSeq.SetInteractionPartner(leftPivot.transform, plane.center);

            Debug.Log($"[DragonSmokeTest] Da spawn 2 con rong 2 ben mat phang tai {plane.center} " +
                      $"(mat phang {plane.size.x:F2}x{plane.size.y:F2}m -> rong {visualSize:F2}m, cach tam {sideOffset:F2}m), chua cho item chinh o giua.");
        }

        // Tinh kich thuoc rong va khoang cach tam theo kich thuoc THAT cua mat phang vua
        // phat hien (plane.size, don vi met) - xem giai thich cac hang so o dau file.
        private static void ComputeResponsiveSizing(ARPlane plane, out float visualSize, out float sideOffset)
        {
            var planeSpan = Mathf.Min(plane.size.x, plane.size.y);
            visualSize = Mathf.Clamp(planeSpan * VisualSizeFractionOfPlane, MinVisualSize, MaxVisualSize);

            var rawOffset = plane.size.x * SideOffsetFractionOfPlaneWidth;
            var minOffset = visualSize * MinSideOffsetMultiplierOfSize;
            sideOffset = Mathf.Clamp(rawOffset, minOffset, MaxSideOffset);
        }

        // Truc "trai-phai" de xep 2 con rong phai la trai-phai THEO GOC NHIN CUA USER luc
        // spawn, khong phai truc noi bo cua mat phang (plane.transform.right) - truc do do
        // ARCore/ARKit tu gan cho mat phang, co the vo tinh chi theo chieu sau (xa/gan
        // camera) thay vi ngang, gay ra loi rong dung truoc-sau thay vi ngang hang. Cach
        // dung: lay vector "right" cua camera, chieu xuong mat phang (bo phan vuong goc voi
        // normal) de ra 1 truc ngang thuc su nam tren mat phang va dung huong voi nguoi dung.
        private static Vector3 ComputeUserRelativeRight(ARPlane plane)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return plane.transform.right; // fallback neu khong co camera
            }

            var projectedRight = Vector3.ProjectOnPlane(camera.transform.right, plane.normal);
            return projectedRight.sqrMagnitude > 0.0001f ? projectedRight.normalized : plane.transform.right;
        }

        private static GameObject SpawnOneDragon(ARPlane plane, Vector3 worldOffset, float visualSize, int startAnimationIndex)
        {
            var dragonAsset = Resources.Load<GameObject>(PrefabResourcePath);
            if (dragonAsset == null)
            {
                Debug.LogError($"[DragonSmokeTest] Khong tim thay model tai Resources/{PrefabResourcePath}.fbx");
                return null;
            }

            var pivot = new GameObject($"TEMP_DragonSmokeTest_Pivot_{startAnimationIndex}");
            var dragon = Object.Instantiate(dragonAsset, pivot.transform);
            dragon.name = "Dragon";

            ScaleAndCenterWithinPivot(dragon, visualSize);

            pivot.transform.position = plane.center + worldOffset;
            pivot.transform.rotation = FaceTowardUser(pivot.transform.position, plane.normal);

            var sequencer = pivot.AddComponent<DragonAnimationSequencer>();
            sequencer.Init(dragon, AnimationOrder, SecondsPerAnimation, startAnimationIndex);

            return pivot;
        }

        // Quay mat rong ve phia camera (nguoi dung) tai thoi diem spawn - dung Camera.main
        // lam vi tri "user", chieu huong xuong mat phang (bo phan Y theo normal cua mat
        // phang) de rong khong bi nga/nghieng, chi xoay quanh truc dung.
        private static Quaternion FaceTowardUser(Vector3 dragonPosition, Vector3 planeNormal)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return Quaternion.identity;
            }

            var directionToUser = camera.transform.position - dragonPosition;
            directionToUser = Vector3.ProjectOnPlane(directionToUser, planeNormal);

            if (directionToUser.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(directionToUser.normalized, planeNormal);
        }

        // Scale deu de canh lon nhat cua bounding box = targetSize (khong phu thuoc scale
        // goc cua file FBX), roi dich model (local, ben trong pivot) sao cho DAY cua
        // bounding box (khong phai tam) trung voi goc pivot - de rong dung tren mat phang
        // thay vi lo lung nua chim nua noi qua mat phang.
        private static void ScaleAndCenterWithinPivot(GameObject dragon, float targetSize)
        {
            var renderers = dragon.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[DragonSmokeTest] Model khong co Renderer nao, bo qua auto-scale.");
                return;
            }

            var bounds = CombinedBounds(renderers);
            var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestDimension <= 0f)
            {
                return;
            }

            var scaleFactor = targetSize / largestDimension;
            dragon.transform.localScale *= scaleFactor;

            var scaledBounds = CombinedBounds(dragon.GetComponentsInChildren<Renderer>());
            var bottomCenter = new Vector3(scaledBounds.center.x, scaledBounds.min.y, scaledBounds.center.z);
            var worldOffset = dragon.transform.position - bottomCenter;
            dragon.transform.position += worldOffset;
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

    // Phat 4 animation co san theo thu tu, lap vong lien tuc (bat dau tu startIndex khac
    // nhau moi con de lech pha, khong dong bo).
    //
    // QUAN TRONG: dung Animator + AnimatorOverrideController (doi clip gan vao state
    // "Run_New" co san) chu KHONG dung Animation (legacy) component + clip.legacy=true
    // nhu ban dau - cach cu chi chay dung trong Unity Editor nhung mat animation khi build
    // that ra APK, vi model duoc import kieu Generic (danh cho Animator/Mecanim), va viec
    // ep clip.legacy=true luc runtime khong duoc IL2CPP/trinh build xu ly dong nhat voi
    // Editor. AnimatorOverrideController la co che chinh thuc, hoat dong dung ca trong
    // Editor lan build that.
    private sealed class DragonAnimationSequencer : MonoBehaviour
    {
        private const float InteractionIntervalSeconds = 15f;
        private const float InteractionHoldSeconds = 2.5f;
        private const float TurnDegreesPerSecond = 90f;

        private Animator _animator;
        private AnimatorOverrideController _overrideController;
        private readonly Dictionary<string, AnimationClip> _clipsByName = new();

        private string[] _order;
        private float _secondsPerClip;
        private int _currentIndex;
        private float _clipTimer;

        private Transform _partner;
        private Vector3 _centerPoint;
        private bool _isInteracting;
        private float _interactionTimer;

        public void Init(GameObject dragon, string[] order, float secondsPerClip, int startIndex)
        {
            _order = order;
            _secondsPerClip = secondsPerClip;
            _currentIndex = order.Length == 0 ? 0 : startIndex % order.Length;

            _animator = dragon.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogWarning("[DragonSmokeTest] Model khong co Animator, khong the phat animation.");
                return;
            }

            var baseController = Resources.Load<RuntimeAnimatorController>(ControllerResourcePath);
            if (baseController == null)
            {
                Debug.LogWarning($"[DragonSmokeTest] Khong tim thay Animator Controller tai Resources/{ControllerResourcePath}.");
                return;
            }

            foreach (var clip in Resources.LoadAll<AnimationClip>(PrefabResourcePath))
            {
                _clipsByName[clip.name] = clip;
            }

            _overrideController = new AnimatorOverrideController(baseController);
            _animator.runtimeAnimatorController = _overrideController;

            PlayCurrent();
        }

        public void SetInteractionPartner(Transform partner, Vector3 centerPoint)
        {
            _partner = partner;
            _centerPoint = centerPoint;
        }

        private void Update()
        {
            if (_overrideController == null || _isInteracting)
            {
                return;
            }

            _clipTimer += Time.deltaTime;
            if (_clipTimer >= _secondsPerClip)
            {
                _clipTimer = 0f;
                _currentIndex = (_currentIndex + 1) % _order.Length;
                PlayCurrent();
            }

            if (_partner != null)
            {
                _interactionTimer += Time.deltaTime;
                if (_interactionTimer >= InteractionIntervalSeconds)
                {
                    _interactionTimer = 0f;
                    StartCoroutine(InteractionBeat());
                }
            }
        }

        private void PlayCurrent()
        {
            SetOverrideClip(_order[_currentIndex]);
        }

        // Doi clip dang gan vao state goc, roi Play lai tu dau (0f) - vi day la thay clip
        // ben duoi 1 state duy nhat (khong phai chuyen state that) nen can restart de tranh
        // loi thoi gian phat (vi du dang o giay thu 3 cua Run ma doi sang Fly, khong restart
        // se nhay vao giua clip Fly thay vi tu dau).
        private void SetOverrideClip(string clipName)
        {
            if (!_clipsByName.TryGetValue(clipName, out var clip))
            {
                Debug.LogWarning($"[DragonSmokeTest] Khong tim thay clip '{clipName}' trong model.");
                return;
            }

            _overrideController[BaseStateName] = clip;
            _animator.Play(BaseStateName, 0, 0f);
        }

        // Mo phong "tuong tac": tam dung chuoi animation binh thuong, quay mat huong ve
        // phia con kia (hoac tam giua neu khong co partner), phat Idle trong luc do, roi
        // quay lai huong cu va tiep tuc chuoi animation. Khong phai animation tuong tac
        // rieng (file goc khong co) - chi la hieu ung xoay + Idle dung dip.
        private IEnumerator InteractionBeat()
        {
            _isInteracting = true;
            var normalRotation = transform.rotation;

            var lookAtPoint = _partner != null ? _partner.position : _centerPoint;
            var lookDirection = lookAtPoint - transform.position;
            lookDirection.y = 0f;
            var targetRotation = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection.normalized, transform.up)
                : transform.rotation;

            SetOverrideClip(FindIdleClipName());

            yield return RotateTowards(targetRotation);
            yield return new WaitForSeconds(InteractionHoldSeconds);
            yield return RotateTowards(normalRotation);

            _isInteracting = false;
            PlayCurrent();
        }

        private IEnumerator RotateTowards(Quaternion target)
        {
            while (Quaternion.Angle(transform.rotation, target) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnDegreesPerSecond * Time.deltaTime);
                yield return null;
            }
            transform.rotation = target;
        }

        private string FindIdleClipName()
        {
            foreach (var name in _order)
            {
                if (name.Contains("Idel") || name.Contains("Idle"))
                {
                    return name;
                }
            }
            return _order[_currentIndex];
        }
    }
}
