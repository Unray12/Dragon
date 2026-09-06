using UnityEngine;

namespace DragonAR.Creature
{
    // Vong sang duoi chan con rong luc vua spawn (Character Ground Glow) - kieu hieu ung
    // hay thay khi nhan vat xuat hien trong game. La 1 quad nam ngang dung shader rieng
    // (DragonAR.GroundGlow, xem S_GroundGlow.shader) - hoan toan la hieu ung RENDER, khong
    // dung toi mesh/rig cua con rong nen KHONG can sua gi trong Blender.
    //
    // _SpawnProgress cua shader di tu 0 (vong sang RONG HET CO, SANG HON) ve 1 (thu nho
    // dung kich thuoc, sang binh thuong) trong SpawnDurationSeconds dau tien - tao cam giac
    // "nang luong lan ra roi lang lai" thay vi bat sang dot ngot mot cai la xong.
    public sealed class GroundGlowEffect : MonoBehaviour
    {
        // PHAI load material co san trong Resources, KHONG Shader.Find() + new Material()
        // luc runtime: shader chi duoc tim bang ten se bi shader stripping loai khoi build
        // IL2CPP neu khong asset nao trong project tham chieu no - tren may that se ra
        // quad mau hong (dung loi da gap voi M_Bubble, sua tu dau cho hieu ung nay).
        private const string GlowMaterialResourcePath = "M_GroundGlow";
        private const float SpawnDurationSeconds = 1.1f;

        private static readonly int SpawnProgressId = Shader.PropertyToID("_SpawnProgress");

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _spawnTimer;

        // Anh sang THAT hat nguoc len tu vong glow, lam sang phan bung/chan con rong tu
        // duoi len - material con rong la URP Lit (co nhan sang that), nen 1 Point Light
        // nho o day tao dung hieu ung "dung tren vung sang phat quang" hay thay trong game,
        // khac voi shader cua quad (chi la hinh ve phang, khong tu chieu sang vat khac).
        //
        // Dat Range/Intensity vua du cho 1 hero object, KHONG bat shadow (Point Light co
        // bong do la 1 trong nhung thu ton kem nhat tren mobile URP) - anh sang nay chi de
        // "to mau" khong phai de do bong chinh xac.
        private const float UpLightRangeScale = 2.4f;   // x radius cua vong glow
        private const float UpLightIntensity = 0.7f;    // nhe - chi diem xuyet, khong lam chay sang bung/chan

        // Layer rieng cho con rong - CHI de gioi han cullingMask cua UpLight, khong lien
        // quan raycast/physics gi khac trong project. Khong co layer nay thi den hat sang
        // se chieu vao BAT KY renderer nao dung gan no (da xac nhan bang anh chup: chieu
        // ca vao mat phang moi truong test, gay chay sang trang xoa mat) - cullingMask gioi
        // han "chi con rong moi nhan duoc anh sang nay", du dung gan sang/vat the khac bao
        // nhieu cung khong bi anh huong. Layer nay PHAI da ton tai trong ProjectSettings
        // (tao 1 lan qua Editor - AddLayer, khong the tao luc runtime tren thiet bi that).
        private const string CreatureLayerName = "Creature";

        // radius: ban kinh vanh ngoai cua hao quang, tinh bang met.
        //
        // glowLocalPosition va upLightLocalPosition la 2 VI TRI KHAC NHAU CO CHU DICH: vong
        // glow (de) dat CACH XA day con rong (tach roi, khong dinh - theo yeu cau), nhung
        // den hat sang thi PHAI o GAN THAN con rong moi to sang duoc nhan vat - neu dung
        // chung 1 vi tri voi de dang o xa, den se chieu manh vao nen/san ngay canh no thay
        // vi vao than con rong (dung loi da gap: gap tang len lam den "troi" theo de ra xa
        // khoi than, gay chay sang nen thay vi sang nhan vat).
        public static GameObject Spawn(Transform parent, Vector3 glowLocalPosition,
            Vector3 upLightLocalPosition, float radius, Color color)
        {
            var baseMaterial = Resources.Load<Material>(GlowMaterialResourcePath);
            if (baseMaterial == null)
            {
                Debug.LogError($"[GroundGlow] Khong thay Resources/{GlowMaterialResourcePath}.");
                return null;
            }

            var go = new GameObject("GroundGlow");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = glowLocalPosition;
            // Quad mac dinh dung trong mat phang XY, xoay 90 do quanh X de nam PHANG xuong
            // dat (mat huong len +Y) - dung huong "nhin tu tren xuong" cua vong sang duoi chan.
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * (radius * 2f);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = BuildQuadMesh();

            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(baseMaterial) { name = "M_GroundGlow (instance)" };
            material.SetColor("_GlowColor", color);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var effect = go.AddComponent<GroundGlowEffect>();
            effect.Init(renderer);

            SpawnUpLight(parent, upLightLocalPosition, radius, color);
            return go;
        }

        // GameObject Light RIENG (khong phai con cua quad GroundGlow, vi quad da bi xoay
        // 90 do quanh X - Light khong quan tam huong nen dat thang len parent cho don gian).
        private static void SpawnUpLight(Transform parent, Vector3 localPosition, float radius, Color color)
        {
            var lightGo = new GameObject("GroundGlow_UpLight");
            lightGo.transform.SetParent(parent, worldPositionStays: false);
            lightGo.transform.localPosition = localPosition;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = radius * UpLightRangeScale;
            light.intensity = UpLightIntensity;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel; // hero object - dang duoc no man hinh, khong de URP ha xuong vertex-lit

            var mask = LayerMask.GetMask(CreatureLayerName);
            if (mask == 0)
            {
                // Layer chua duoc tao trong ProjectSettings (xem ghi chu tren CreatureLayerName)
                // - fallback ve chieu SANG MOI THU thay vi khong sang gi ca, de loi nay de
                // nhan ra (sang lem ca moi truong) hon la "sao den khong co tac dung gi".
                Debug.LogWarning($"[GroundGlow] Layer '{CreatureLayerName}' chua ton tai - " +
                                  "UpLight se chieu vao moi vat, khong gioi han rieng con rong.");
                mask = ~0;
            }
            light.cullingMask = mask;
        }

        // Goi 1 LAN cho GameObject con rong ngay sau khi Instantiate, TRUOC khi UpLight
        // duoc tao - dat con rong (va toan bo children: mesh, xuong...) vao layer rieng de
        // UpLight.cullingMask co the gioi han CHI chieu sang no, khong lan sang bat ky
        // renderer nao khac dung gan (mat phang moi truong, bong bong nuoc...). Da xac nhan
        // qua anh chup: khong co buoc nay, UpLight chieu ca vao mat san moi truong test gay
        // chay sang trang xoa mat.
        public static void ApplyCreatureLayer(GameObject root)
        {
            var layer = LayerMask.NameToLayer(CreatureLayerName);
            if (layer < 0)
            {
                return; // se roi vao nhanh fallback (~0) trong SpawnUpLight, khong loi
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                t.gameObject.layer = layer;
            }
        }

        // Quad thu cong thay vi PrimitiveType.Quad: primitive quad di kem 1 Collider mac
        // dinh khong can thiet (hieu ung khong nhan tia cham), tu tao mesh de khoi phai
        // them roi lai Destroy no.
        private static Mesh BuildQuadMesh()
        {
            var mesh = new Mesh { name = "GroundGlowQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Init(MeshRenderer meshRenderer)
        {
            _renderer = meshRenderer;
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_spawnTimer >= SpawnDurationSeconds)
            {
                return;
            }

            _spawnTimer += Time.deltaTime;
            var progress = Mathf.Clamp01(_spawnTimer / SpawnDurationSeconds);
            // EaseOutCubic - lan ra nhanh luc dau, lang lai nhe o cuoi, tu nhien hon tuyen tinh.
            progress = 1f - Mathf.Pow(1f - progress, 3f);

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(SpawnProgressId, progress);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
