using UnityEngine;

namespace DragonAR.Core
{
    // Thong tin ve 1 be mat da duoc phat hien (san/ban...), tach rieng khoi AR Foundation -
    // DragonAR.Creature chi biet den struct nay, khong bao gio dung truc tiep kieu ARPlane.
    // Neu sau nay doi tu Plane Detection sang Image Target/Model Target, chi can DragonAR.AR
    // tao ra TrackableSurfaceInfo theo cach khac; Creature khong phai sua gi ca.
    public readonly struct TrackableSurfaceInfo
    {
        public Vector3 Center { get; }
        public Vector3 Normal { get; }
        public Vector3 RightAxis { get; }
        public Vector2 Size { get; }

        public TrackableSurfaceInfo(Vector3 center, Vector3 normal, Vector3 rightAxis, Vector2 size)
        {
            Center = center;
            Normal = normal;
            RightAxis = rightAxis;
            Size = size;
        }
    }
}
