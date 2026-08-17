using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Creature
{
    // Spawn 2 con rong 2 ben be mat da phat hien, chua khoang trong o giua cho item chinh.
    // Kich thuoc/khoang cach tu dieu chinh theo kich thuoc THAT cua be mat (responsive),
    // gioi han trong khoang Min/Max de khong qua nho/qua to so voi khung hinh camera.
    public static class DragonSpawner
    {
        private const string ModelResourcePath = "BGE_Dragon_2.7_Animation_Only";
        private const string ControllerResourcePath = "BGE_Dragon_2.7_Animation_Only";
        private const string BaseAnimatorStateName = "Armature|Run_New";

        // Thu tu phoi hop 4 animation co san trong file, lap lai vo han.
        private static readonly string[] AnimationOrder =
        {
            "Armature|Fly_New",
            "Armature|Walk_New",
            "Armature|Run_New",
            "Armature|Idel_New",
        };

        private const float SecondsPerAnimation = 4f;

        private const float VisualSizeFractionOfPlane = 1.4f; // % canh nho hon cua be mat dung lam kich thuoc rong
        private const float MinVisualSize = 0.8f; // met
        private const float MaxVisualSize = 8f; // met

        private const float SideOffsetFractionOfPlaneWidth = 0.16f; // % chieu rong be mat dung lam khoang cach tam
        private const float MinSideOffsetMultiplierOfSize = 0.35f; // toi thieu = % kich thuoc rong, tranh 2 con de len nhau
        private const float MaxSideOffset = 1.6f; // met

        public static DragonSpawnResult SpawnTwoDragons(TrackableSurfaceInfo surface)
        {
            ComputeResponsiveSizing(surface.Size, out var visualSize, out var sideOffset);

            var leftPivot = SpawnOneDragon(surface, -surface.RightAxis * sideOffset, visualSize, startAnimationIndex: 0);
            var rightPivot = SpawnOneDragon(surface, surface.RightAxis * sideOffset, visualSize, startAnimationIndex: 2);

            if (leftPivot != null && rightPivot != null)
            {
                var leftController = leftPivot.GetComponent<CreatureAnimationController>();
                var rightController = rightPivot.GetComponent<CreatureAnimationController>();
                leftController.SetInteractionPartner(rightPivot.transform, surface.Center);
                rightController.SetInteractionPartner(leftPivot.transform, surface.Center);
            }

            Debug.Log($"[DragonSpawner] Da spawn 2 con rong 2 ben be mat tai {surface.Center} " +
                      $"(be mat {surface.Size.x:F2}x{surface.Size.y:F2}m -> rong {visualSize:F2}m, cach tam {sideOffset:F2}m).");

            return new DragonSpawnResult(leftPivot, rightPivot, visualSize);
        }

        // Tinh kich thuoc rong va khoang cach tam theo kich thuoc THAT cua be mat vua phat
        // hien (don vi met).
        private static void ComputeResponsiveSizing(Vector2 planeSize, out float visualSize, out float sideOffset)
        {
            var planeSpan = Mathf.Min(planeSize.x, planeSize.y);
            visualSize = Mathf.Clamp(planeSpan * VisualSizeFractionOfPlane, MinVisualSize, MaxVisualSize);

            var rawOffset = planeSize.x * SideOffsetFractionOfPlaneWidth;
            var minOffset = visualSize * MinSideOffsetMultiplierOfSize;
            sideOffset = Mathf.Clamp(rawOffset, minOffset, MaxSideOffset);
        }

        private static GameObject SpawnOneDragon(TrackableSurfaceInfo surface, Vector3 worldOffset, float visualSize, int startAnimationIndex)
        {
            var dragonAsset = Resources.Load<GameObject>(ModelResourcePath);
            if (dragonAsset == null)
            {
                Debug.LogError($"[DragonSpawner] Khong tim thay model tai Resources/{ModelResourcePath}.fbx");
                return null;
            }

            var pivot = new GameObject($"Dragon_Pivot_{startAnimationIndex}");
            var dragon = Object.Instantiate(dragonAsset, pivot.transform);
            dragon.name = "Dragon";

            BoundsScaler.ScaleToFitAndAlignBottom(dragon, visualSize);

            pivot.transform.position = surface.Center + worldOffset;
            pivot.transform.rotation = FaceTowardUser(pivot.transform.position, surface.Normal);

            var controller = pivot.AddComponent<CreatureAnimationController>();
            controller.Init(dragon, ModelResourcePath, ControllerResourcePath, BaseAnimatorStateName, AnimationOrder, SecondsPerAnimation, startAnimationIndex);

            return pivot;
        }

        // Quay mat rong ve phia camera (nguoi dung) tai thoi diem spawn - chieu huong xuong
        // be mat (bo phan vuong goc voi normal) de rong khong bi nga/nghieng.
        private static Quaternion FaceTowardUser(Vector3 dragonPosition, Vector3 surfaceNormal)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return Quaternion.identity;
            }

            var directionToUser = camera.transform.position - dragonPosition;
            directionToUser = Vector3.ProjectOnPlane(directionToUser, surfaceNormal);

            if (directionToUser.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(directionToUser.normalized, surfaceNormal);
        }
    }

    public readonly struct DragonSpawnResult
    {
        public GameObject LeftPivot { get; }
        public GameObject RightPivot { get; }
        public float VisualSize { get; }

        public DragonSpawnResult(GameObject leftPivot, GameObject rightPivot, float visualSize)
        {
            LeftPivot = leftPivot;
            RightPivot = rightPivot;
            VisualSize = visualSize;
        }
    }
}
