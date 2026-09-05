using System;
using System.Collections.Generic;
using DragonAR.Core;
using UnityEngine;

namespace DragonAR.Telemetry
{
    // Nguon so lieu GIA LAP - dung khi chua cau hinh ThingsBoard, khi test trong Editor,
    // hoac khi khong co mang. Giu nguyen hanh vi random cua dashboard truoc day de van co
    // cai ma nhin luc phat trien, nhung nay nam sau ITelemetrySource nen dashboard khong
    // biet minh dang xem so gia hay so that.
    //
    // Gia tri di chuyen theo buoc ngau nhien quanh gia tri hien tai (random walk) chu khong
    // nhay lung tung trong khoang - nhin giong cam bien that hon, va bieu do lich su moi ra
    // duong cong co y nghia.
    public sealed class SimulatedTelemetrySource : MonoBehaviour, ITelemetrySource
    {
        public event Action<IReadOnlyList<TelemetrySample>> SamplesReceived;
        public event Action<TelemetryHistory> HistoryReceived;

        private const float IntervalSeconds = 1.5f;
        private const int HistoryLength = 16;

        private readonly List<SimulatedMetric> _metrics = new();
        private readonly List<double> _history = new();
        private float _timer;

        public void Configure(IEnumerable<string> keys)
        {
            _metrics.Clear();
            foreach (var key in keys)
            {
                _metrics.Add(SimulatedMetric.ForKey(key));
            }
        }

        private void Start()
        {
            Emit();
        }

        private void Update()
        {
            if (_metrics.Count == 0)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < IntervalSeconds)
            {
                return;
            }

            _timer = 0f;
            Emit();
        }

        private void Emit()
        {
            var now = DateTime.UtcNow;
            var samples = new List<TelemetrySample>(_metrics.Count);

            for (var i = 0; i < _metrics.Count; i++)
            {
                var metric = _metrics[i];
                metric.Step();
                _metrics[i] = metric;
                samples.Add(new TelemetrySample(metric.Key, metric.Value, now));
            }

            SamplesReceived?.Invoke(samples);

            // Nguon that lay lich su tu server; ban gia lap tu tich lai gia tri cua chinh
            // no - de dashboard chi can 1 duong xu ly, khong phai biet dang dung nguon nao.
            if (_metrics.Count > 0)
            {
                _history.Add(_metrics[0].Value);
                while (_history.Count > HistoryLength)
                {
                    _history.RemoveAt(0);
                }

                HistoryReceived?.Invoke(new TelemetryHistory(_metrics[0].Key, _history));
            }
        }

        // Khoang gia tri lay theo so do that doc duoc tu tram cam bien, de so gia lap nhin
        // hop ly chu khong phai so bua.
        private struct SimulatedMetric
        {
            public string Key;
            public double Value;
            private double _min;
            private double _max;
            private double _step;

            public static SimulatedMetric ForKey(string key)
            {
                return key switch
                {
                    "temperature" => Create(key, 24f, 38f, 0.3f),
                    "humidity" => Create(key, 45f, 85f, 0.8f),
                    "pm25" => Create(key, 5f, 90f, 2f),
                    "pm10" => Create(key, 8f, 120f, 3f),
                    "co2" => Create(key, 400f, 900f, 8f),
                    "noise" => Create(key, 40f, 80f, 1.5f),
                    "pressure" => Create(key, 99f, 102f, 0.05f),
                    "light" => Create(key, 0f, 120000f, 3000f),
                    "rain" => Create(key, 0f, 15f, 0.2f),
                    "wind_direction" => Create(key, 0f, 359f, 10f),
                    _ => Create(key, 0f, 100f, 2f)
                };
            }

            private static SimulatedMetric Create(string key, double min, double max, double step)
            {
                return new SimulatedMetric
                {
                    Key = key,
                    _min = min,
                    _max = max,
                    _step = step,
                    Value = UnityEngine.Random.Range((float)min, (float)max)
                };
            }

            public void Step()
            {
                Value = Math.Clamp(Value + UnityEngine.Random.Range(-(float)_step, (float)_step), _min, _max);
            }
        }
    }
}
