using System;
using System.Collections.Generic;

namespace DragonAR.Core
{
    // Nguon so lieu cam bien. Day la DUONG NOI duy nhat giua lop lay du lieu
    // (DragonAR.Telemetry - ThingsBoard REST, hoac gia lap) va lop hien thi (DragonAR.UI).
    //
    // Ly do phai co interface nay thay vi de dashboard tu goi HTTP: DragonAR.UI chi duoc
    // reference DragonAR.Core (xem asmdef + unity-clean-architecture §4), khong duoc biet
    // ThingsBoard ton tai. Doi backend hay chay offline chi la thay implementation, khong
    // sua 1 dong nao trong UI.
    //
    // Su kien - KHONG polling: dashboard subscribe roi cho, khong tu hoi moi frame
    // (unity-clean-architecture §6, anti-pattern 2 - ton pin tren dien thoai dang bat camera AR).
    public interface ITelemetrySource
    {
        // Gia tri MOI NHAT cua tung chi so - do vao cac the so lieu.
        event Action<IReadOnlyList<TelemetrySample>> SamplesReceived;

        // Chuoi lich su cua 1 chi so - ve len bieu do. Ban lan dau ngay khi ket noi duoc
        // (de bieu do khong trong), sau do lam moi dinh ky.
        event Action<TelemetryHistory> HistoryReceived;
    }
}
