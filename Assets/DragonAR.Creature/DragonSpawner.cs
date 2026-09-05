using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Creature
{
    // Spawn 1 con rong DUY NHAT, GAN THEO CAMERA (khong can AR plane detection) - luon
    // dung yen truoc mat camera o 1 khoang cach/offset co dinh, du nguoi dung xoay dien
    // thoai huong nao (camera-relative).
    //
    // Model: prefab PF_Dragon (Assets/Art/Creatures/Dragon/Resources/) - mesh CH_Dragon.fbx
    // export tu 3D-Model/Dragon-AI/Untitled.blend (20.570 tris, da bo modifier Subdiv vi
    // level 2 = ~320k tris, vuot xa ngan sach mobile AR), material M_Dragon_Body (URP Lit)
    // dung 2 texture bake tu node graph Blender: T_Dragon_Albedo (2K, da ap Hue/Sat +
    // Brightness/Contrast) va T_Dragon_MetallicSmoothness (1K, R=metallic tu gold mask,
    // A=smoothness).
    //
    // Model KHONG co rig/animation (0 skins, 0 animations) - day la mesh tinh. Chuyen dong
    // duy nhat la CreatureTurntable (xoay tron 360 do tai cho bang code) - khong phai
    // animation that. Muon chuyen dong that thi phai rig/animate trong Blender truoc.
    public static class DragonSpawner
    {
        private const string PrefabResourcePath = "PF_Dragon";

        // Goc BAT DAU cua vong xoay 360 (CreatureTurntable se xoay tiep tu day). DA KIEM
        // CHUNG bang anh chup Scene View trong Unity voi dung pipeline export nay (FBX,
        // bake_space_transform, axis -Z/+Y):
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

            pivot.AddComponent<CreatureTurntable>();

            return pivot;
        }
    }
}
