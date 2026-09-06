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
    // chuyen voi nhau qua kieu dinh nghia o Core (TrackableSurfaceInfo, ITelemetrySource)
    // va qua Pose/su kien.
    //
    // Chay tu dong bang [RuntimeInitializeOnLoadMethod] - KHONG can keo script nao vao
    // scene bang tay. Doi lai: neu file nay khong ton tai thi app van compile sach nhung
    // KHONG SPAWN GI CA. Day chinh xac la thu da xay ra khi clone repo ve - folder
    // Assets/DragonAR.App/ bi dong "*.app" trong .gitignore nuot mat. Da sua .gitignore.
    //
    // LUONG HIEN TAI: mo app len KHONG hien gi. Nguoi dung phai soi vao NHAN dan tren tram
    // quan trac; nhan dien duoc thi con rong moi xuat hien, NEO CO DINH ben phai nhan va
    // ngang tam nhan, roi dung yen tai do - khong bam theo camera nua.
    public static class AppBootstrapper
    {
        private const float DragonVisualSize = 0.55f;

        // Con rong dat BEN PHAI nhan va CUNG DO CAO voi nhan.
        private const float DragonRightOffsetFromMarker = 0.55f;

        // Scene khong co ARTrackedImageManager (vd scene test trong Editor) thi khong cho
        // nhan nua, spawn luon truoc mat de con test duoc.
        private static readonly Vector3 EditorFallbackOffset = new(0f, -0.10f, 1.10f);

        private const string ThingsBoardConfigResourcePath = "ThingsBoardConfig";

        // Chi so hien trong bong bong. Key phai TRUNG ten key tren ThingsBoard - danh sach
        // that cua tram cam bien (device 589cbae0-...): co2, humidity, light, noise, pm10,
        // pm25, pressure, rain, temperature, wind_direction (+ cac key chuan doan thiet bi).
        //
        // Luu y: KHONG co key toc do gio, chi co wind_direction (do). Va cpu_temperature la
        // nhiet do CHIP ESP32 (~52C), khong phai nhiet do moi truong.
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

            if (ImageTargetTrackingManager.TryCreate(out var imageTracker))
            {
                Debug.Log("[AppBootstrapper] Dang cho nhan dien nhan tren tram quan trac...");
                imageTracker.TargetAcquiredOnce += marker =>
                    PlaceDragon(ComputeDragonPosition(marker, camera), camera);
            }
            else
            {
                PlaceDragon(camera.transform.TransformPoint(EditorFallbackOffset), camera);
            }

            // Core AR: van chay plane detection; tim thay be mat that thi tat detect va an
            // luoi highlight cho khung hinh sach.
            if (SurfaceTrackingManager.TryCreate(out var tracker))
            {
                tracker.SurfaceReady += _ => tracker.StopDetectionAndHideVisuals();
            }
        }

        // Ben phai nhan, ngang tam nhan.
        //
        // "Ben phai" tinh theo GOC NHIN cua nguoi dang soi chu khong theo truc rieng cua
        // tracked image: AR Foundation dat truc cho tracked image khong giong nhau giua
        // ARCore va ARKit, suy dien tu do rat de sai (da tung mat thoi gian vi doan huong o
        // vu xoay 180 do). Cach nay dung tren ca hai nen tang.
        //
        // Vector3.Cross(up, forward) = right trong he toa do trai cua Unity, va ket qua co
        // y = 0 nen do cao giu nguyen bang do cao cua nhan - dung yeu cau "ngang tam nhan".
        private static Vector3 ComputeDragonPosition(Pose marker, Camera camera)
        {
            var towardMarker = marker.position - camera.transform.position;
            towardMarker.y = 0f;
            if (towardMarker.sqrMagnitude < 0.0001f)
            {
                towardMarker = camera.transform.forward;
                towardMarker.y = 0f;
            }

            var right = Vector3.Cross(Vector3.up, towardMarker.normalized);
            return marker.position + right * DragonRightOffsetFromMarker;
        }

        private static void PlaceDragon(Vector3 worldPosition, Camera camera)
        {
            var pivot = DragonSpawner.SpawnAnchored(worldPosition, camera.transform.position, DragonVisualSize);
            if (pivot == null)
            {
                return;
            }

            // Neo vao ban do khong gian de con rong khong troi khi AR hieu chinh lai.
            ArAnchorService.TryAnchor(pivot);

            // Bong bong lam CON cua pivot con rong: pivot dung yen (khong con tu xoay nhu
            // ban CreatureTurntable cu) nen cho vong quy dao bam theo con rong la dung.
            MetricBubbleRing.Spawn(pivot.transform, Vector3.zero, CreateTelemetrySource(), BubbleMetrics);

            Debug.Log($"[AppBootstrapper] Da neo con rong tai {worldPosition}.");
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
