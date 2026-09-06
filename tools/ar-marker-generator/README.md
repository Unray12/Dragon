# AR Marker Generator

Script tự sinh ảnh nhãn (Image Target) dùng cho `ARTrackedImageManager` — nhãn
dán lên trạm quan trắc để con rồng AR neo vào khi nhận diện được.

Đặt ngoài `Assets/` vì đây là **dev tool build-time** (chạy bằng Python trên
máy dev), không phải asset Unity — giống cách `3D-Model/` cũng nằm ngoài
`Assets/`. Kết quả sinh ra ghi thẳng vào `Assets/Art/Markers/`.

## Chạy

```bash
cd tools/ar-marker-generator
python generate_marker.py
```

Cần `Pillow` + `numpy` (`pip install Pillow numpy`). Ghi đè trực tiếp 2 file
sau, **giữ nguyên tên** để không phải trỏ lại `XRReferenceImageLibrary` trong
Unity:

| File | Dùng để |
|---|---|
| `Assets/Art/Markers/T_Marker_DragonEden_200mm_300dpi.png` | Gửi đi in — 200mm ở 300 DPI |
| `Assets/Art/Markers/T_Marker_DragonEden.png` | Unity dùng, đã gán vào `DragonEdenImageLibrary.asset` |

Script tự chấm điểm ngay sau khi sinh ảnh và báo PASS/FAIL theo ngưỡng ở
`marker_lib.py` — không cần đoán, không cần thiết bị thật mới biết ảnh có
đáng thử hay không.

## Vì sao nhãn cần thiết kế đúng cách, không phải ảnh bất kỳ

`ARTrackedImageManager` (ARCore Augmented Image / ARKit image detection)
**không dùng AI/machine learning** — nó so khớp bằng thị giác máy tính cổ
điển: dò điểm góc/cạnh (kiểu Harris/FAST), mô tả vùng lân cận mỗi điểm bằng
1 descriptor, rồi so khớp descriptor giữa ảnh camera và ảnh trong thư viện.
Từ đó suy ra 3 tiêu chí bắt buộc:

1. **Nhiều điểm góc/cạnh** — ảnh trơn, gradient mịn (như 1 tấm ảnh phong
   cảnh mờ) gần như không có gì để dò.
2. **Phủ đều toàn bộ diện tích** — nếu nửa ảnh là hoạ tiết dày, nửa kia bỏ
   trống, người dùng soi trúng nửa trống sẽ mất bám dù cả nhãn "trông" giàu
   chi tiết.
3. **Tương phản cao** — sáng/tối rõ ràng, không phải các tông màu gần nhau.
4. **Bất đối xứng** — nếu nhãn đối xứng qua 1 trục hoặc lặp lại khi xoay
   90°/180°, thuật toán có thể khớp nhầm hướng (con rồng quay lưng thay vì
   quay mặt — đã từng xảy ra với việc suy đoán hướng model, xem
   `context/DECISIONS_LOG.md`).

Mã QR **không tối ưu cho việc này** dù trông "nhiều chi tiết": các module QR
kích thước đều nhau tạo ra hoạ tiết lặp lại, và 3 ô định vị góc tạo đối xứng
cục bộ — cả hai đều làm giảm độ tin cậy so khớp so với 1 hoạ tiết được thiết
kế bất đối xứng có chủ đích.

## Thiết kế: rút gọn còn 3 thành phần

Bản đầu tiên có nhiều yếu tố trang trí (mảng low-poly, thanh waveform, 3 dòng
chữ) — đẹp nhưng không thành phần nào trong đó *cần thiết* cho tracking, và
đo được: chỉ phủ 59–78% lưới đặc trưng (một nửa nhãn "nghèo" hơn nửa kia).
Bản rút gọn này phủ **98.4%**, đơn giản hơn nhiều:

1. **Lưới ô vuông 2 màu ngẫu nhiên, phủ toàn bộ khung** — thành phần duy nhất
   tạo phần lớn đặc trưng. Kích thước ô (`CELL_SIZE_MM` = 7mm) là điểm cân
   bằng: đủ nhỏ để tạo nhiều cạnh, đủ to để không bị mờ/alias khi ảnh giảm
   còn 1024px cho Unity hoặc khi camera đứng xa 30–60cm.
2. **4 dấu hiệu góc khác nhau** (vuông/tròn/tam giác/cộng) — khoá hướng,
   giữ nguyên từ bản đầu vì đã kiểm chứng hoạt động.
3. **1 dòng thương hiệu duy nhất** — cho người đọc, đặt trong dải riêng phía
   dưới để không phá vỡ lưới đặc trưng.

## Chỉnh sửa

- Đổi kích thước in: sửa `MARKER_SIZE_MM`, rồi **phải cập nhật lại**
  `SetSize()` trong `DragonEdenImageLibrary.asset` (Unity) cho khớp — kích
  thước khai báo sai không cản việc nhận diện, nhưng làm tỉ lệ con rồng/
  khoảng cách bị sai theo (xem giải thích trong lịch sử chat / `DECISIONS_LOG.md`).
- Đổi mật độ hoạ tiết: sửa `CELL_SIZE_MM` (nhỏ hơn = nhiều đặc trưng hơn
  nhưng dễ mờ khi soi xa; to hơn = ít đặc trưng hơn nhưng rõ khi soi xa).
- Đổi màu/chữ: sửa hằng số `BG`/`LIGHT`/`DARK`/`ACCENT` và chuỗi text trong
  `build_marker()`.
- Sau khi sửa, luôn chạy lại script — nó tự chấm điểm và báo FAIL nếu thay
  đổi vô tình làm giảm chất lượng dưới ngưỡng (`marker_lib.py`:
  `MIN_KEYPOINTS`, `MIN_GRID_COVERAGE_PCT`, `MIN_CONTRAST_STD`).

## File trong thư mục này

| File | Vai trò |
|---|---|
| `generate_marker.py` | CLI chính — dựng ảnh, lưu, chấm điểm |
| `marker_lib.py` | Logic chấm điểm dùng chung (Harris keypoint thô + năng lượng gradient theo lưới) |
| `README.md` | File này |
