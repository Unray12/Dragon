using UnityEngine;

namespace DragonAR.Creature
{
    // Xoay lien tuc tron 360 do quanh truc dung cua chinh no ("turntable"), khong dich
    // chuyen vi tri - de nguoi dung xem duoc model tu moi goc ma khong phai di vong quanh.
    //
    // Day la chuyen dong bang CODE, khong phai animation that: model hien tai la mesh tinh
    // (0 skins, 0 animations). Khi nao model duoc rig/animate trong Blender thi bo component
    // nay va dung Animator thay the.
    //
    // Gan len PIVOT (khong phai truc tiep len model) vi BoundsScaler da can tam bounding box
    // cua model vao goc toa do cua pivot - xoay pivot tuc la xoay quanh dung tam con rong,
    // khong bi lech quy dao.
    public sealed class CreatureTurntable : MonoBehaviour
    {
        // 30 do/giay = 1 vong day du moi 12 giay - du cham de nhin ro chi tiet, du nhanh de
        // khong thay "dung yen". Doi tai day hoac qua DegreesPerSecond luc spawn.
        private const float DefaultDegreesPerSecond = 30f;

        [SerializeField] private float _degreesPerSecond = DefaultDegreesPerSecond;

        public float DegreesPerSecond
        {
            get => _degreesPerSecond;
            set => _degreesPerSecond = value;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
