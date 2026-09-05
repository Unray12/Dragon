using UnityEngine;

namespace DragonAR.Telemetry
{
    // Cau hinh KET NOI toi ThingsBoard - chi host/device/credential. Hien thi chi so nao la
    // do AppBootstrapper quyet dinh (DashboardMetrics), khong de o day: neu ca 2 noi cung
    // khai bao danh sach key thi som muon cung lech nhau. Asset that nam o
    // Assets/Resources/ThingsBoardConfig.asset va DA BI GITIGNORE vi chua credential -
    // moi may tu tao ban cua minh (Assets > Create > DragonAR > ThingsBoard Config, hoac
    // chep gia tri tu file .env o goc repo). Khong co asset nay thi AppBootstrapper tu
    // rot ve SimulatedTelemetrySource, app van chay binh thuong.
    //
    // CANH BAO BAO MAT: moi thu build vao APK deu moi ra duoc (IL2CPP khong bao ve chuoi).
    // Dung tai khoan TENANT_ADMIN o day la sai - hay tao 1 Customer user chi co quyen DOC
    // dung device nay. Muon chac chan hon nua thi dung 1 proxy nho cua minh dung giua app
    // va ThingsBoard, credential nam o server.
    [CreateAssetMenu(fileName = "ThingsBoardConfig", menuName = "DragonAR/ThingsBoard Config")]
    public sealed class ThingsBoardConfig : ScriptableObject
    {
        [Header("Server")]
        [SerializeField] private string _host = "https://dragonedenem.dsolution.net";
        [SerializeField] private string _deviceId = "";

        [Header("Dang nhap (app tu lay JWT va tu gia han)")]
        [SerializeField] private string _username = "";
        [SerializeField] private string _password = "";

        [Header("Chi de TEST - JWT dan san, se het han sau vai gio")]
        [TextArea(2, 5)]
        [SerializeField] private string _jwtOverride = "";

        [Header("Truy van")]
        [SerializeField] private float _pollSeconds = 10f;

        public string Host => _host != null ? _host.TrimEnd('/') : string.Empty;
        public string DeviceId => _deviceId;
        public string Username => _username;
        public string Password => _password;
        public string JwtOverride => _jwtOverride != null ? _jwtOverride.Trim() : string.Empty;
        // Cam bien nay day len moi ~30s (TelePeriod cua gateway Tasmota) - poll nhanh hon
        // 5s chi ton pin/bang thong ma khong co so lieu moi.
        public float PollSeconds => Mathf.Max(5f, _pollSeconds);

        public bool IsUsable =>
            !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(DeviceId)
            && (!string.IsNullOrWhiteSpace(JwtOverride)
                || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
    }
}
