using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Creature
{
    // Spawn 1 con rong DUY NHAT, GAN THEO CAMERA (khong can AR plane detection) - luon
    // dung yen truoc mat camera o 1 khoang cach/offset co dinh, du nguoi dung xoay dien
    // thoai huong nao (camera-relative).
    //
    // Model: prefab PF_Dragon (Assets/Art/Creatures/Dragon/Resources/) - mesh CH_Dragon_Rigged.fbx
    // export tu 3D-Model/Dragon-AI/Dragon_Rigged.blend (20.570 tris, da bo modifier Subdiv vi
    // level 2 = ~320k tris, vuot xa ngan sach mobile AR), material M_Dragon_Body (URP Lit)
    // dung 2 texture bake tu node graph Blender: T_Dragon_Albedo (2K, da ap Hue/Sat +
    // Brightness/Contrast) va T_Dragon_MetallicSmoothness (1K, R=metallic tu gold mask,
    // A=smoothness).
    //
    // Model DA CO RIG (13 xuong) va animation that, lam trong Blender:
    //   Hover - bong benh 1 nhip, duoi tre pha, chan lung lang  (state mac dinh)
    //   Idle  - tho 2 nhip, song duoi, dau ngo nhe
    // Ca 2 clip deu loop kin (frame dau trung frame cuoi tuyet doi). Animator + controller
    // AC_Dragon nam san tren prefab PF_Dragon, nen o day khong phai lam gi them.
    //
    // CreatureTurntable (xoay tron 360 bang code) DA XOA - do la chuyen dong gia dung tam
    // khi chua co rig; giu lai se chong len animation that.
    public static class DragonSpawner
    {
        private const string PrefabResourcePath = "PF_Dragon";

        // Goc xoay quanh truc dung luc spawn. DA KIEM CHUNG bang anh chup thuc te trong
        // Unity:
        // 0 do  = mat huong VE camera (dung)
        // 180 do = quay LUNG ve camera
        // Model AI-generated nay co "forward" nguoc quy uoc thong thuong, nen KHONG duoc
        // suy dien 180 do tu thoi quen Blender->Unity - phai nhin anh chup that.
        private const float SpawnYawDegrees = 0f;

        public static GameObject SpawnAttachedToCamera(Vector3 localOffsetFromCamera, float visualSize = 0.55f)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[DragonSpawner] No main camera found, cannot spawn camera-attached dragon.");
                return null;
            }

            var dragonAsset = Resources.Load<GameObject>(PrefabResourcePath);
            if (dragonAsset == null)
            {
                Debug.LogError($"[DragonSpawner] Prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var pivot = new GameObject("Dragon_CameraPreview");
            var dragon = Object.Instantiate(dragonAsset, pivot.transform);
            dragon.name = "Dragon";

            // Can TAM (khong phai day) vi con rong lo lung truoc camera, khong dung tren
            // mat phang nao.
            BoundsScaler.ScaleToFitAndCenter(dragon, visualSize);

            pivot.transform.SetParent(camera.transform, worldPositionStays: false);
            pivot.transform.localPosition = localOffsetFromCamera;
            pivot.transform.localRotation = Quaternion.Euler(0f, SpawnYawDegrees, 0f);

            return pivot;
        }
    }
}
