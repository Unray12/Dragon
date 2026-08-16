# Dragon AR App

App mobile AR: mở camera, nhận diện 1 vật thể mục tiêu, con rồng 3D xuất hiện và chuyển
động quanh vật thể đó.

- Kiến trúc & luồng dữ liệu: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- Mô tả từng module: [docs/MODULES.md](docs/MODULES.md)
- Chức năng chi tiết: [docs/FEATURES.md](docs/FEATURES.md)
- Công nghệ/version cụ thể: [docs/TECH_STACK.md](docs/TECH_STACK.md)
- Trạng thái hiện tại & việc tiếp theo: [context/STATUS.md](context/STATUS.md)

---

## 1. Yêu cầu cài đặt

| Thành phần | Ghi chú |
|---|---|
| **Unity Hub** | [unity.com/download](https://unity.com/download) |
| **Unity Editor 6000.5.8f1** | Cài đúng version này qua Unity Hub (Installs → Install Editor) — project khoá version này trong `ProjectSettings/ProjectVersion.txt`, mở bằng version khác Unity sẽ cảnh báo/re-import lỗi. |
| Module **Android Build Support** (kèm OpenJDK, Android SDK & NDK Tools) | Bắt buộc nếu build Android — chọn khi cài Editor qua Unity Hub, không cần cài Android Studio riêng. |
| Module **iOS Build Support** | Bắt buộc nếu build iOS. |
| **macOS + Xcode** | Chỉ cần nếu build/chạy thử trên iPhone/iPad thật hoặc submit App Store. Không có Mac thì vẫn code/test trên PC bình thường (mục 2), chỉ không build iOS được. |
| **VSCode** + extension `visualstudiotoolsforunity.vstuc` | Đã cấu hình sẵn trong `.vscode/` — cài extension là dùng được ngay. |

Mở project: Unity Hub → **Open** → chọn thư mục [Dragon/](.) (thư mục chứa `Assets/`,
`ProjectSettings/` — chính là thư mục repo này).

---

## 2. Chạy & test trên PC (không cần điện thoại thật)

Project đã có sẵn **XR Simulation** (AR Foundation 6) — mô phỏng camera + tracking ngay
trong Editor, không cần build ra máy mỗi lần sửa code.

### 2.1. Chạy thử bằng môi trường giả lập (XR Simulation)

1. Mở scene chính (`Assets/Scenes/SampleScene.unity`, sẽ đổi thành scene thật khi có
   `DragonAR.App/Scenes/Main.unity` theo [docs/MODULES.md](docs/MODULES.md#dragonarapp)).
2. Mở **Window → XR → AR Foundation → XR Environment View** để xem/chỉnh môi trường giả
   lập (mặc định có sẵn 1 vài scene môi trường mẫu của Unity).
3. Nhấn **Play** — Game view sẽ hiển thị camera ảo trong môi trường giả lập thay vì camera
   thật.
4. Di chuyển camera ảo trong lúc Play: giữ **chuột phải + WASD** để bay quanh, **Q/E** để
   lên/xuống, giữ **Shift** để di chuyển nhanh hơn (giống điều khiển Scene View).
5. Để test tracking ảnh (Image Target): thêm ảnh cần nhận diện vào Reference Image
   Library, rồi đặt 1 bản sao ảnh đó vào trong scene môi trường giả lập (XR Environment) ở
   vị trí bất kỳ — Simulation sẽ tự nhận diện như thật.

Đây là cách nhanh nhất để lặp code hành vi con rồng (orbit, animation, state machine) mà
không cần build/cài lên điện thoại mỗi lần sửa 1 dòng.

### 2.2. Chạy automated test (Unity Test Framework)

1. Mở **Window → General → Test Runner**.
2. Tab **EditMode**: test logic thuần (state machine, toán orbit, event channel — xem
   phạm vi nên test tại [.claude/skills/unity-clean-architecture/SKILL.md §7](.claude/skills/unity-clean-architecture/SKILL.md#7-testing-strategy)).
   Nhấn **Run All** — chạy ngay trong Editor, không cần build, vài giây là xong.
3. Tab **PlayMode**: test cần frame tick thật (ví dụ animation controller phản ứng đúng
   sau khi nhận event) — nhấn **Run All**, Editor sẽ vào Play mode tự động chạy test.
4. Chạy test từ command line (hữu ích cho CI sau này):
   ```
   Unity -batchmode -projectPath . -runTests -testPlatform EditMode ^
         -testResults results.xml -quit
   ```
   (đường dẫn `Unity` cần trỏ đúng file `Unity.exe` của version 6000.5.8f1 trong Unity
   Hub, ví dụ `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`)

**Lưu ý**: tracking AR thật (độ chính xác nhận diện ảnh/vật thể ngoài đời, ánh sáng, độ che
khuất) **không test tự động được** — phần này luôn cần build lên thiết bị thật để kiểm tra
bằng mắt (mục 3).

---

## 3. Nạp xuống điện thoại (build & deploy)

### 3.1. Android

1. Bật **Developer Options** trên điện thoại (Settings → About phone → chạm 7 lần vào
   "Build number"), rồi bật **USB debugging** trong Developer Options.
2. Cắm điện thoại vào PC qua USB, chọn "Allow" khi điện thoại hỏi cấp quyền debug.
3. Trong Unity: **File → Build Settings** → chọn **Android** → **Switch Platform** (chỉ
   cần làm 1 lần, lần sau Unity nhớ platform).
4. Trước khi build, kiểm tra checklist ở
   [.claude/skills/unity-android-native/SKILL.md §5](.claude/skills/unity-android-native/SKILL.md#5-production-android-build-settings)
   (IL2CPP, ARM64, ASTC) — Player Settings hiện tại đã có `AndroidMinSdkVersion: 34`, ARCore
   chỉ yêu cầu tối thiểu 24 nên vẫn ổn, nhưng **`applicationIdentifier` hiện đang là giá trị
   mặc định của template** (`com.unity.template.ar_mobile`) — cần đổi sang package name của
   bạn (ví dụ `com.tencongty.dragonar`) trước khi build thật, xem
   [context/OPEN_QUESTIONS.md](context/OPEN_QUESTIONS.md) (thêm mục này nếu chưa có).
5. Điện thoại đang cắm sẵn → bấm **Build And Run**: Unity tự build APK và cài thẳng lên
   máy, mở app luôn.
   - Hoặc bấm **Build** để lấy file `.apk`/`.aab`, rồi cài thủ công:
     `adb install -r duong-dan-file.apk`
6. Lần đầu mở app, cấp quyền Camera khi được hỏi. Nếu lỡ từ chối và bị màn đen: vào
   Settings → Apps → Dragon → Permissions → bật lại Camera thủ công (xem hành vi mong muốn
   ở [.claude/skills/unity-android-native/SKILL.md §4](.claude/skills/unity-android-native/SKILL.md#4-arcore-specific-configuration)).
7. Xem log khi app đang chạy: **Window → Analysis → Android Logcat** (đã cài sẵn package
   `com.unity.mobile.android-logcat`).

**Build bản release** (để đưa lên Google Play): dùng **Android App Bundle (.aab)**, cần
tạo/keystore ký app (**Publishing Settings → Keystore Manager**), rồi upload lên Play
Console (khuyến nghị bắt đầu ở track **Internal testing**, không public thẳng).

### 3.2. iOS

Bắt buộc cần **máy Mac có Xcode**. Từ Windows (như PC hiện tại) không build/chạy thử iOS
trực tiếp được — chỉ có thể code và test bằng XR Simulation (mục 2), sau đó nhờ máy Mac để
build khi cần chạy thử trên iPhone thật.

1. Trên Mac: cài Unity Hub + đúng version Editor `6000.5.8f1`, mở project qua Unity Hub
   (project này, share qua git — xem mục 4).
2. **File → Build Settings** → chọn **iOS** → **Switch Platform** → **Build** — Unity xuất
   ra 1 project Xcode (không phải app cài được ngay, cần build tiếp bằng Xcode).
3. Mở file `.xcodeproj` (hoặc `.xcworkspace` nếu có CocoaPods) vừa xuất ra bằng **Xcode**.
4. Trong Xcode: chọn **Signing & Capabilities** → chọn Team (Apple ID cá nhân dùng được để
   test trên thiết bị của chính bạn, không cần trả phí Apple Developer Program — nhưng
   certificate loại này hết hạn sau 7 ngày, phải build lại).
5. Cắm iPhone/iPad vào Mac qua cáp, chọn máy đó làm **Run Destination** trong Xcode, bấm
   **Run (▶)** — app cài thẳng lên máy.
6. Lần đầu mở app trên máy: vào **Settings → General → VPN & Device Management** trên
   iPhone, tin tưởng (Trust) certificate của bạn nếu iOS chặn app "chưa xác minh".

**Build bản để test rộng hơn (TestFlight)**: cần **Apple Developer Program** (trả phí
hàng năm) — trong Xcode chọn **Product → Archive**, rồi **Distribute App → App Store
Connect**, sau đó bật TestFlight trên App Store Connect để mời người test qua link, không
cần cắm cáp từng máy.

---

## 4. Đồng bộ giữa máy Windows (code chính) và máy Mac (build iOS)

Project đã có git remote sẵn (`https://github.com/Unray12/Dragon.git`). Quy trình hợp lý:
code/test chính trên Windows (mục 2), commit + push lên remote, rồi trên Mac chỉ cần
`git pull` rồi mở lại Unity là build iOS được ngay — không cần đồng bộ thủ công qua USB/
cloud drive.

---

## 5. Khắc phục sự cố nhanh

| Vấn đề | Kiểm tra |
|---|---|
| Màn hình đen khi mở app trên điện thoại | Quyền Camera có được cấp chưa (Settings → Apps → Dragon → Permissions) |
| Android báo "cần cài Google Play Services for AR" | Bình thường nếu là lần đầu chạy trên máy đó — làm theo prompt để cài, hoặc thiết bị không hỗ trợ ARCore (kiểm tra trong [danh sách thiết bị hỗ trợ ARCore](https://developers.google.com/ar/devices)) |
| Build Android báo lỗi Gradle/AGP sau khi update Unity | Xem [.claude/skills/unity-android-native/SKILL.md §5](.claude/skills/unity-android-native/SKILL.md#5-production-android-build-settings) — có thể do Custom Gradle Template hoặc plugin bên thứ 3 chưa tương thích version AGP mới |
| Không thấy gì trong XR Environment View khi Play | Đảm bảo đã chọn 1 Simulation Environment asset trong **XR Simulation Settings** (`Assets/XR/Settings/XRSimulationSettings.asset`) |
