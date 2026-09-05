using System;

namespace DragonAR.Core
{
    // 1 gia tri do duoc tu cam bien, da chuan hoa - tach roi khoi ThingsBoard hay bat ky
    // backend nao khac. DragonAR.UI chi biet den struct nay, khong bao gio biet du lieu den
    // tu REST, WebSocket, MQTT hay chi la gia lap.
    //
    // TimestampUtc la thoi diem CAM BIEN do duoc (server tra ve), khong phai luc app nhan
    // duoc - nho vay dashboard tinh duoc "so lieu nay cu bao lau roi" va bao cho nguoi dung
    // biet khi thiet bi ngung gui, thay vi hien so cu nhu that.
    public readonly struct TelemetrySample
    {
        public string Key { get; }
        public double Value { get; }
        public DateTime TimestampUtc { get; }

        public TelemetrySample(string key, double value, DateTime timestampUtc)
        {
            Key = key;
            Value = value;
            TimestampUtc = timestampUtc;
        }
    }
}
