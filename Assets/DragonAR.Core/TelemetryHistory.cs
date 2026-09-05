using System.Collections.Generic;

namespace DragonAR.Core
{
    // Chuoi gia tri lich su cua DUNG 1 chi so, da sap xep cu -> moi. Tach khoi
    // TelemetrySample vi day la 2 viec khac nhau: Sample tra loi "bay gio bao nhieu"
    // (do vao the so lieu), History tra loi "2 tieng qua no chay the nao" (ve len bieu do).
    //
    // Co cai nay thi bieu do day ngay tu giay dau mo app, thay vi phai doi du 16 lan poll
    // (~160 giay) moi ve xong 16 cot.
    public readonly struct TelemetryHistory
    {
        public string Key { get; }
        public IReadOnlyList<double> Values { get; }

        public TelemetryHistory(string key, IReadOnlyList<double> values)
        {
            Key = key;
            Values = values;
        }
    }
}
