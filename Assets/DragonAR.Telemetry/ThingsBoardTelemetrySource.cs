using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DragonAR.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonAR.Telemetry
{
    // Doc telemetry that tu ThingsBoard self-hosted bang REST polling.
    //
    // TAI SAO REST CHU KHONG PHAI MQTT: MQTT API cua ThingsBoard la de THIET BI day du lieu
    // len (dung device access token), khong cho client subscribe telemetry cua device khac.
    // App nay la ben DOC, nen chi con REST hoac WebSocket. Chon REST vi don gian va de chan
    // doan hon; cam bien nay cung chi day moi ~30s nen day tuc thi kieu WebSocket khong loi
    // duoc bao nhieu.
    //
    // TAI SAO PHAI DANG NHAP: device access token KHONG doc duoc timeseries (da thu that:
    // GET /api/plugins/telemetry/... voi token do tra ve 401 "Invalid username or password").
    // Doc timeseries bat buoc JWT cua USER.
    //
    // Endpoint dung o day:
    //   POST {host}/api/auth/login                 body {"username","password"} -> {"token"}
    //   GET  {host}/api/plugins/telemetry/DEVICE/{deviceId}/values/timeseries?keys=a,b,c
    //        (getLatestTimeseries) -> {"temperature":[{"ts":...,"value":"33.3"}], ...}
    //   GET  {host}/api/plugins/telemetry/DEVICE/{deviceId}/values/timeseries/history?...
    //        (getTimeseriesHistory) -> cung dang tra ve, nhung nhieu diem theo thoi gian
    //   Ca hai deu can header  X-Authorization: Bearer {token}
    public sealed class ThingsBoardTelemetrySource : MonoBehaviour, ITelemetrySource
    {
        public event Action<IReadOnlyList<TelemetrySample>> SamplesReceived;
        public event Action<TelemetryHistory> HistoryReceived;

        // Cua so lich su cho bieu do. Cam bien day moi ~30s, nen neu chi lay dung 160s gan
        // nhat (16 cot x 10s poll) thi chi co ~5 diem that. Gom 2 tieng thanh 16 o bang
        // agg=AVG cho ra duong xu huong co y nghia hon nhieu.
        private const int HistoryWindowHours = 2;
        private const int HistoryBuckets = 16;

        // Lam moi bieu do moi 6 lan poll (~60s voi poll 10s) thay vi moi lan - bieu do gom
        // theo o 7.5 phut nen poll nao cung goi lai chi ton request ma hinh khong doi.
        private const int HistoryRefreshEveryNPolls = 6;

        private ThingsBoardConfig _config;
        private string[] _keys;
        private string _token;
        private int _consecutiveFailures;
        private int _pollCount;

        // keys do AppBootstrapper truyen vao (lay tu danh sach chi so cua dashboard) chu
        // khong de trong config - de khong bao gio lech giua "key di lay" va "key dem ra
        // hien thi". keys[0] la chi so duoc ve len bieu do lich su.
        public void Configure(ThingsBoardConfig config, string[] keys)
        {
            _config = config;
            _keys = keys;
        }

        private void OnEnable()
        {
            StartCoroutine(PollLoop());
        }

        private IEnumerator PollLoop()
        {
            // AddComponent<T>() chay OnEnable NGAY, truoc khi nguoi goi kip Configure().
            // Cho toi khi co cau hinh thay vi tu tat component - neu tu tat thi Configure()
            // goi sau cung vo nghia, coroutine khong bao gio chay (loi da gap that).
            while (_config == null || _keys == null || _keys.Length == 0)
            {
                yield return null;
            }

            var wait = new WaitForSeconds(_config.PollSeconds);

            while (true)
            {
                if (string.IsNullOrEmpty(_token))
                {
                    yield return AcquireToken();
                }

                if (!string.IsNullOrEmpty(_token))
                {
                    if (_pollCount % HistoryRefreshEveryNPolls == 0)
                    {
                        yield return FetchHistory();
                    }

                    yield return FetchLatest();
                    _pollCount++;
                }

                yield return wait;
            }
        }

        private IEnumerator AcquireToken()
        {
            // JWT dan san chi de test - het han thi khong tu gia han duoc, phai dien
            // username/password moi co the login lai.
            if (!string.IsNullOrEmpty(_config.JwtOverride))
            {
                _token = _config.JwtOverride;
                yield break;
            }

            var body = JsonUtility.ToJson(new LoginRequest
            {
                username = _config.Username,
                password = _config.Password
            });

            using var request = UnityWebRequest.Post($"{_config.Host}/api/auth/login", body, "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportFailure($"login that bai ({request.responseCode}): {request.error}");
                yield break;
            }

            try
            {
                _token = JObject.Parse(request.downloadHandler.text).Value<string>("token");
            }
            catch (Exception e)
            {
                ReportFailure($"khong doc duoc token tu response login: {e.Message}");
                yield break;
            }

            if (string.IsNullOrEmpty(_token))
            {
                ReportFailure("response login khong co truong 'token'.");
            }
        }

        private IEnumerator FetchLatest()
        {
            var url = $"{_config.Host}/api/plugins/telemetry/DEVICE/{_config.DeviceId}" +
                      $"/values/timeseries?keys={string.Join(",", _keys)}";

            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("X-Authorization", $"Bearer {_token}");
            yield return request.SendWebRequest();

            // 401 = JWT het han hoac bi thu hoi -> xoa di, vong lap sau se login lai.
            if (request.responseCode == 401)
            {
                _token = null;
                ReportFailure("JWT het han hoac khong hop le, se dang nhap lai.");
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportFailure($"lay telemetry that bai ({request.responseCode}): {request.error}");
                yield break;
            }

            List<TelemetrySample> samples;
            try
            {
                samples = ParseTimeseries(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                ReportFailure($"khong parse duoc telemetry: {e.Message}");
                yield break;
            }

            if (_consecutiveFailures > 0)
            {
                Debug.Log($"[ThingsBoard] Da ket noi lai, nhan duoc {samples.Count} gia tri.");
                _consecutiveFailures = 0;
            }

            if (samples.Count > 0)
            {
                SamplesReceived?.Invoke(samples);
            }
        }

        // getTimeseriesHistory: lay HistoryBuckets diem trai deu tren HistoryWindowHours gio
        // gan nhat, moi diem la trung binh cua 1 o thoi gian (agg=AVG). Loi that su: bieu do
        // day ngay lan ve dau tien thay vi trong 160 giay dau.
        private IEnumerator FetchHistory()
        {
            var endTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowMs = (long)HistoryWindowHours * 60 * 60 * 1000;
            var startTs = endTs - windowMs;
            var key = _keys[0];

            var url = $"{_config.Host}/api/plugins/telemetry/DEVICE/{_config.DeviceId}" +
                      $"/values/timeseries/history?keys={key}" +
                      $"&startTs={startTs}&endTs={endTs}" +
                      $"&interval={windowMs / HistoryBuckets}&agg=AVG&limit={HistoryBuckets}&orderBy=ASC";

            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("X-Authorization", $"Bearer {_token}");
            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                _token = null;
                yield break; // FetchLatest se bao loi, khong can log 2 lan
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportFailure($"lay lich su that bai ({request.responseCode}): {request.error}");
                yield break;
            }

            List<double> values;
            try
            {
                values = ParseHistory(request.downloadHandler.text, key);
            }
            catch (Exception e)
            {
                ReportFailure($"khong parse duoc lich su: {e.Message}");
                yield break;
            }

            if (values.Count > 0)
            {
                HistoryReceived?.Invoke(new TelemetryHistory(key, values));
            }
        }

        private static List<double> ParseHistory(string json, string key)
        {
            var values = new List<double>();
            var series = JObject.Parse(json)[key];
            if (series == null)
            {
                return values;
            }

            foreach (var entry in series)
            {
                if (double.TryParse(entry.Value<string>("value"), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        // {"temperature":[{"ts":1788586528000,"value":"33.3"}], "humidity":[...]}
        // Gia tri luon la CHUOI trong response cua ThingsBoard, ke ca khi la so. Bo qua key
        // khong phai so (vd current_fw_title) thay vi nem loi - de dashboard cu hien phan
        // con lai.
        private static List<TelemetrySample> ParseTimeseries(string json)
        {
            var samples = new List<TelemetrySample>();

            foreach (var property in JObject.Parse(json).Properties())
            {
                var first = property.Value.First;
                if (first == null)
                {
                    continue;
                }

                var raw = first.Value<string>("value");
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    continue;
                }

                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(first.Value<long>("ts")).UtcDateTime;
                samples.Add(new TelemetrySample(property.Name, value, timestamp));
            }

            return samples;
        }

        // Chi log lan dau cua 1 chuoi loi lien tiep - poll moi 10s ma log moi lan se ngap
        // Console va che het log khac khi mat mang.
        private void ReportFailure(string message)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures == 1)
            {
                Debug.LogWarning($"[ThingsBoard] {message}");
            }
        }

        [Serializable]
        private struct LoginRequest
        {
            public string username;
            public string password;
        }
    }
}
