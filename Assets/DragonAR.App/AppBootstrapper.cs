using DragonAR.AR;
using DragonAR.Core;
using DragonAR.Creature;
using DragonAR.Telemetry;
using DragonAR.UI;
using UnityEngine;

namespace DragonAR.App
{
    // Composition root DUY NHAT cua app - module duy nhat duoc phep reference ca AR,
    // Creature, Telemetry va UI cung luc. Cac module kia khong bao gio biet nhau; noi
    // chuyen voi nhau qua kieu dinh nghia o Core (TrackableSurfaceInfo, ITelemetrySource).
    //
    // Chay tu dong bang [RuntimeInitializeOnLoadMethod] - KHONG can keo script nao vao
    // scene bang tay. Doi lai: neu file nay khong ton tai thi app van compile sach nhung
    // KHONG SPAWN GI CA (khong rong, khong dashboard). Day chinh xac la thu da xay ra khi
    // clone repo ve - folder Assets/DragonAR.App/ bi dong "*.app" trong .gitignore nuot
    // mat, chua bao gio duoc commit. Da sua .gitignore (them "!Assets/DragonAR.App/").
    public static class AppBootstrapper
    {
        // Vi tri con rong so voi camera: lech trai, hoi thap, cach mat ~1.1m - du gan de
        // thay ro chi tiet, du xa de khong che het khung hinh.
        private static readonly Vector3 DragonOffsetFromCamera = new(-0.30f, -0.10f, 1.10f);
        private const float DragonVisualSize = 0.55f;

        private const string ThingsBoardConfigResourcePath = "ThingsBoardConfig";


        // Chi so hien tren dashboard. Key phai TRUNG ten key tren ThingsBoard - danh sach
        // that cua tram cam bien (device 589cbae0-...): co2, humidity, light, noise, pm10,
        // pm25, pressure, rain, temperature, wind_direction (+ cac key chuan doan thiet bi:
        // cpu_temperature, heap_free_size, ...).
        //
        // Luu y: KHONG co key toc do gio, chi co wind_direction (do). Va cpu_temperature la
        // nhiet do CHIP ESP32 (~52C), khong phai nhiet do moi truong - dung nham len the
        // TEMPERATURE. Chi so dau tien la cai duoc ve len bieu do lich su.
        // 6 bong bong chia deu quanh vong tron (60 do 1 cai). Nhan tieng Viet cho khop
        // trang kiosk cua portal.
        private static readonly MetricDisplay[] BubbleMetrics =
        {
            new("temperature", "Nhiệt độ", "°C", decimals: 1),
            new("humidity", "Độ ẩm", "%", decimals: 1),
            new("co2", "CO₂", "ppm", decimals: 0),
            new("pm25", "PM2.5", "µg/m³", decimals: 0),
            new("pm10", "PM10", "µg/m³", decimals: 0),
            new("noise", "Tiếng ồn", "dB", decimals: 1)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[AppBootstrapper] No main camera in this scene, skipping bootstrap.");
                return;
            }

            DragonSpawner.SpawnAttachedToCamera(DragonOffsetFromCamera, DragonVisualSize);

            // Bong bong bay quanh con rong: gan theo camera o DUNG vi tri con rong, nhung
            // KHONG lam con cua pivot con rong - de vong quy dao cua bong bong doc lap voi
            // moi chuyen dong cua chinh con rong.
            MetricBubbleRing.Spawn(camera.transform, DragonOffsetFromCamera, CreateTelemetrySource(), BubbleMetrics);

            // Core AR: van chay plane detection de biet khi nao co be mat that trong phong.
            // Hien noi dung khong phu thuoc ket qua nay (rong + dashboard da spawn ngay o
            // tren), nen viec duy nhat can lam khi tim thay be mat la TAT detect va an luoi
            // highlight cua cac mat phang cho khung hinh sach.
            if (!SurfaceTrackingManager.TryCreate(out var tracker))
            {
                return; // scene khong co AR (vd scene test trong Editor) - bo qua, khong loi
            }

            tracker.SurfaceReady += _ => tracker.StopDetectionAndHideVisuals();
        }

        // Co cau hinh ThingsBoard hop le thi doc cam bien that; khong thi rot ve gia lap de
        // app van chay duoc (may chua cau hinh, khong co mang, hoac dang test trong Editor).
        // Asset cau hinh bi gitignore vi chua credential - xem ThingsBoardConfig.cs.
        private static ITelemetrySource CreateTelemetrySource()
        {
            var host = new GameObject("TelemetrySource");
            Object.DontDestroyOnLoad(host);

            var keys = new string[BubbleMetrics.Length];
            for (var i = 0; i < BubbleMetrics.Length; i++)
            {
                keys[i] = BubbleMetrics[i].Key;
            }

            var config = Resources.Load<ThingsBoardConfig>(ThingsBoardConfigResourcePath);
            if (config != null && config.IsUsable)
            {
                var thingsBoard = host.AddComponent<ThingsBoardTelemetrySource>();
                thingsBoard.Configure(config, keys);
                Debug.Log($"[AppBootstrapper] Telemetry: ThingsBoard ({config.Host}), poll {config.PollSeconds}s.");
                return thingsBoard;
            }

            var simulated = host.AddComponent<SimulatedTelemetrySource>();
            simulated.Configure(keys);
            Debug.LogWarning("[AppBootstrapper] Telemetry: GIA LAP (chua co Resources/ThingsBoardConfig hop le).");
            return simulated;
        }


    }
}
