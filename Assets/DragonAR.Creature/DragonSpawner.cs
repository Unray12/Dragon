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

        // Mau + ban kinh cua Character Ground Glow luc spawn - xem GroundGlowEffect.cs.
        // Day HOAN TOAN la hieu ung render (quad + shader), khong dung mesh/rig cua con
        // rong nen khong can sua gi trong Blender.
        private static readonly Color GroundGlowColor = new(0.20f, 0.95f, 0.65f, 1f);
        private const float GroundGlowRadiusScale = 0.62f; // x kich thuoc con rong (visualSize)

        // KHOANG HO giua vong glow va day con rong - CO CHU DICH tach roi, khong de dinh
        // vao nhau: 1 vong sang lo lung duoi chan (giong hieu ung hay thay trong game) phai
        // nhin RO la 2 vat the rieng biet, khong phai chan cam thang vao giua vong hay long
        // vao nhau. Ti le theo visualSize de doi kich thuoc con rong khong pha vo ty le nay.
        private const float GroundGlowGapScale = 0.28f; // x visualSize

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

            // De GroundGlowEffect gioi han UpLight CHI chieu sang con rong (xem ghi chu
            // trong GroundGlowEffect.ApplyCreatureLayer) - khong lan sang moi truong xung
            // quanh (da xac nhan qua anh chup: thieu buoc nay gay chay sang trang mat phang
            // moi truong ngay canh).
            GroundGlowEffect.ApplyCreatureLayer(dragon);

            // Can TAM (khong phai day) vi con rong lo lung ngang tam nhan, khong dung tren
            // mat phang nao.
            var worldBounds = BoundsScaler.ScaleToFitAndCenter(dragon, visualSize);

            // Doi WORLD Y CUA DAY sang LOCAL cua pivot NGAY LUC NAY - khi pivot con o goc
            // toa do voi rotation identity (chua goi 2 dong set position/rotation ben duoi),
            // nen world = local tai chinh xac thoi diem nay. TUYET DOI khong duoc goi
            // InverseTransformPoint() SAU KHI da doi pivot.position/rotation voi 1 toa do
            // world dinh tu THOI DIEM CU o tren - do la bug thuc su da gap khi test: 2 thoi
            // diem (truoc/sau khi doi transform) bi tron lan, ra ket qua vo nghia (-0.809
            // thay vi ~-0.27 hop ly voi rong cao 0.55m).
            var bottomLocalY = worldBounds.min.y - pivot.transform.position.y;

            pivot.transform.position = worldPosition;
            pivot.transform.rotation = FacingRotation(worldPosition, viewerPosition);

            // 2 VI TRI KHAC NHAU co chu dich (xem ghi chu trong GroundGlowEffect.Spawn):
            //  - vong glow (de): CACH XA day mot khoang - tach roi, khong dinh vao chan.
            //  - den hat sang: GAN DAY THAT (gan than con rong) - de con to sang duoc nhan
            //    vat, khong bi "troi" ra xa theo de va chieu nham vao nen/san.
            var glowLocalPos = new Vector3(0f, bottomLocalY - visualSize * GroundGlowGapScale, 0f);
            var upLightLocalPos = new Vector3(0f, bottomLocalY, 0f);

            GroundGlowEffect.Spawn(pivot.transform, glowLocalPos, upLightLocalPos,
                visualSize * GroundGlowRadiusScale, GroundGlowColor);

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
