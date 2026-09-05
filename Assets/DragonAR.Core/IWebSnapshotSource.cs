using System;
using UnityEngine;

namespace DragonAR.Core
{
    // Nguon anh cua 1 trang web de dap len khung AR.
    //
    // TAI SAO LA ANH CHU KHONG PHAI TRANG WEB SONG: Unity khong co engine render HTML.
    // Muon trang web that su chay trong texture 3D thi phai co 3D WebView (Vuplex, tra phi)
    // hoac tu viet plugin native (Android WebView -> SurfaceTexture -> external texture).
    // Voi trang kiosk cua Dragon Eden thi khac biet gan nhu bang 0: du lieu cam bien chi doi
    // moi ~30s va trang khong co nut bam nao, nen lam moi anh vai giay 1 lan la du.
    //
    // Interface nay chinh la cho de thay the: mua Vuplex thi viet 1 implementation khac
    // (ban frame cua webview ra thay vi tai anh ve), khung/keo tha/billboard giu nguyen.
    public interface IWebSnapshotSource
    {
        // Anh moi cua trang. Panel chi viec dap len, khong quan tam anh tu dau ra.
        event Action<Texture2D> SnapshotReceived;

        // Bao loi kem thong diep de panel hien ro "dang loi gi", thay vi de khung trang tron
        // khien nguoi dung tuong app treo.
        event Action<string> SnapshotFailed;
    }
}
