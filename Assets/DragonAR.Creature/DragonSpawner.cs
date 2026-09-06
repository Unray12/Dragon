using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Creature
{
    // Spawn 1 con rong DUY NHAT, NEO CO DINH vao 1 toa do that trong khong gian AR.
    //
    // KHONG con gan theo camera nhu ban truoc: con rong dung yen tai cho, nguoi dung di
    // chuyen hay xoay dien thoai thi no van nam nguyen do - dung nhu 1 vat the that trong
    // phong. Viec giu no khong bi TROI khi AR hieu chinh lai ban do la viec cua
    // ArAnchorService (DragonAR.AR), khong phai cua lop nay.
    //
    // Model: prefab PF_Dragon (Assets/Art/Creatures/Dragon/Resources/) - mesh
    // CH_Dragon_Rigged.fbx export tu 3D-Model/Dragon-AI/Dragon_Rigged.blend (20.570 tris, da
    // bo modifier Subdiv vi level 2 = ~320k tris, vuot xa ngan sach mobile AR), material
    // M_Dragon_Body (URP Lit) dung 2 texture bake tu node graph Blender.
    //
    // Model DA CO RIG (13 xuong) va animation that, lam trong Blender:
    //   Hover - bong benh 1 nhip, duoi tre pha, chan lung lang  (state mac dinh)
    //   Idle  - tho 2 nhip, song duoi, dau ngo nhe
    // Ca 2 clip deu loop kin (frame dau trung frame cuoi tuyet doi). Animator + controller
    // AC_Dragon nam san tren prefab PF_Dragon, nen o day khong phai lam gi them.
    public static class DragonSpawner
    {
        private const string PrefabResourcePath = "PF_Dragon";

        // Neo con rong tai worldPosition, quay mat ve phia nguoi dang nhin (viewerPosition).
        // Sau khi spawn, pivot KHONG bao gio duoc dat lai vi tri hay huong nua.
        public static GameObject SpawnAnchored(Vector3 worldPosition, Vector3 viewerPosition,
            float visualSize = 0.55f)
        {
            var dragonAsset = Resources.Load<GameObject>(PrefabResourcePath);
            if (dragonAsset == null)
            {
                Debug.LogError($"[DragonSpawner] Prefab not found at Resources/{PrefabResourcePath}.");
                return null;
            }

            var pivot = new GameObject("Dragon_Anchored");
            var dragon = Object.Instantiate(dragonAsset, pivot.transform);
            dragon.name = "Dragon";

            // Can TAM (khong phai day) vi con rong lo lung ngang tam nhan, khong dung tren
            // mat phang nao.
            BoundsScaler.ScaleToFitAndCenter(dragon, visualSize);

            pivot.transform.position = worldPosition;
            pivot.transform.rotation = FacingRotation(worldPosition, viewerPosition);
            return pivot;
        }

        // DA KIEM CHUNG bang anh chup thuc te: model AI-generated nay hien dung MAT khi
        // "forward" cua pivot huong RA XA nguoi xem (cung quy uoc voi WorldPanelBillboard).
        // KHONG duoc suy dien 180 do tu thoi quen Blender->Unity - da tung sai vi doan.
        private static Quaternion FacingRotation(Vector3 worldPosition, Vector3 viewerPosition)
        {
            var away = worldPosition - viewerPosition;
            away.y = 0f;   // chi xoay quanh truc dung, khong cho con rong nga truoc/sau

            return away.sqrMagnitude < 0.0001f
                ? Quaternion.identity
                : Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
