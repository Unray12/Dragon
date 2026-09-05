namespace DragonAR.UI
{
    // Cach hien thi 1 chi so tren dashboard: lay telemetry key nao, ghi nhan la gi, don vi
    // gi, lam tron may chu so. Tach ra khoi UI code de doi chi so hien thi la doi 1 dong
    // cau hinh trong AppBootstrapper, khong phai sua ham dung giao dien.
    //
    // Key phai TRUNG voi ten key tren ThingsBoard (vd "temperature", "humidity", "pm25") -
    // dashboard doi chieu theo ten nay khi nhan duoc TelemetrySample.
    public readonly struct MetricDisplay
    {
        public string Key { get; }
        public string Label { get; }
        public string Unit { get; }
        public int Decimals { get; }

        public MetricDisplay(string key, string label, string unit, int decimals = 0)
        {
            Key = key;
            Label = label;
            Unit = unit;
            Decimals = decimals;
        }

        // Ca so va don vi tren 1 dong - dung khi cho hep (the so lieu 1 hang).
        public string Format(double value)
        {
            return value.ToString("F" + Decimals) + Unit;
        }

        // Chi rieng con so - dung o bong bong 3D, noi don vi da nam san o dong duoi nen
        // ghep vao day se thanh lap lai ("437 ppm" + "ppm").
        public string FormatValueOnly(double value)
        {
            return value.ToString("F" + Decimals);
        }
    }
}
