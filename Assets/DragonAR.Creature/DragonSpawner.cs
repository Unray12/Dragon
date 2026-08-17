using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Creature
{
    // Spawn 1 con rong DUY NHAT, GAN THEO CAMERA (khong can AR plane detection) - luon
    // dung yen truoc mat camera o 1 khoang cach/offset co dinh, du nguoi dung xoay dien
    // thoai huong nao (camera-relative). Model la file glb AI-generated tu anh mascot tham
    // chieu (img-mascos-1.png, xem Assets/img-mascos-1.png o goc workspace) - da kiem tra
    // qua glTF JSON: 0 skins, 0 animations (mesh tinh, chua rig/animate), nen dung hieu ung
    // "lac lu tai cho" (xoay qua lai theo song sin) thay vi Animator that.
    //
    // LICH SU: truoc day co logic spawn 2 con rong tren mat phang AR phat hien duoc
    // (SpawnTwoDragons, dung model FBX BGE_Dragon_2.7_Animation_Only co animation that,
    // responsive theo kich thuoc mat phang, hanh vi "tuong tac" quay mat nhin nhau) - DA
    // XOA HOAN TOAN theo yeu cau don gian hoa app ve 1 con rong duy nhat dung dung model
    // AI-generated moi. Neu can animation that cho model nay sau nay, phai rig/animate no
    // truoc (Blender, hoac Unity AI RigMesh/GenerateHumanoidAnimation neu co model kha
    // dung - xem context/OPEN_QUESTIONS.md).
    public static class DragonSpawner
    {
        private const string ModelResourcePath = "MascotModel";

        private const float RotationDegreesPerSecond = 40f;

        public static GameObject SpawnAttachedToCamera(Vector3 localOffsetFromCamera, float visualSize = 0.55f)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[DragonSpawner] No main camera found, cannot spawn camera-attached dragon.");
                return null;
            }

            var dragonAsset = Resources.Load<GameObject>(ModelResourcePath);
            if (dragonAsset == null)
            {
                Debug.LogError($"[DragonSpawner] Model not found at Resources/{ModelResourcePath}.");
                return null;
            }

            var pivot = new GameObject("Dragon_CameraPreview");
            var dragon = Object.Instantiate(dragonAsset, pivot.transform);
            dragon.name = "Dragon";

            BoundsScaler.ScaleToFitAndCenter(dragon, visualSize);

            pivot.transform.SetParent(camera.transform, worldPositionStays: false);
            pivot.transform.localPosition = localOffsetFromCamera;
            // Khong xoay 180 do nua - model nay (AI-generated tu anh mascot) co "forward"
            // rieng cua no khac quy uoc thong thuong; 180 do lam mat quay LUNG ve phia
            // camera (da xac nhan qua phan hoi thuc te). De 0 do la dung huong.
            pivot.transform.localRotation = Quaternion.identity;

            pivot.AddComponent<SpinInPlace>();

            return pivot;
        }

        // Xoay tron lien tuc quanh truc dung, khong dich chuyen vi tri - "xoay tai cho" de
        // xem duoc model tu moi goc. Chi la hieu ung tam thoi cho toi khi model co
        // rig/animation that (xem ghi chu dau file).
        private sealed class SpinInPlace : MonoBehaviour
        {
            private void Update()
            {
                transform.Rotate(Vector3.up, RotationDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }
    }
}
