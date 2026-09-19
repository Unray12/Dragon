"""
Tao anh nhan AR (Image Target) cho tram quan trac Dragon Eden.

CACH DUNG
    python generate_marker.py

Ghi de truc tiep 2 file trong Assets/Art/Markers/ (giu nguyen ten cu, KHONG
doi duong dan) de khong phai cap nhat lai XRReferenceImageLibrary trong Unity:
    T_Marker_DragonEden_200mm_300dpi.png   - ban de IN, 200mm @ 300 DPI
    T_Marker_DragonEden.png                - ban 1024x1024 cho Unity

THIET KE
Nhan gom 4 dai xep chong theo chieu doc, moi dai mot vai tro ro rang:

    +--------------------------------------------------+
    | [goc]                                     [goc]  |
    |  DRAGON EDEN                                     |  <- dai chu (cho nguoi doc)
    |  TRAM QUAN TRAC MOI TRUONG                       |
    |  STATION ... - LONG MACH DAT LANH                |
    |                                                  |
    |  +------------------+   +--------------------+   |  <- dai dac trung (cho may)
    |  |  mang low-poly   |   |  luoi o vuong      |   |
    |  |  (tam giac)      |   |  ngau nhien        |   |
    |  +------------------+   +--------------------+   |
    |                                                  |
    |  ||l|.|ll|.|l|ll|.|ll|l|.|ll|                    |  <- dai waveform
    |  SOI DE XEM DU LIEU TRUC TIEP                    |
    | [goc]                                     [goc]  |
    +--------------------------------------------------+

Phan LAM NEN TRACKING la 2 khoi giua: mang low-poly tao canh xien nhieu huong,
luoi o vuong tao goc vuong sac net. Hai kieu hoa tiet khac han nhau dat canh
nhau khien moi vung nho tren nhan co mot "van tay" rieng, giam kha nang so khop
nham giua cac vung. Dai chu va waveform chu yeu de nguoi doc nhan ra day la cai
gi, dong gop it dac trung hon - do la ly do nhan nay do duoc ~77% do phu luoi
chu khong phai ~98% nhu mot nhan phu kin hoa tiet (xem README.md muc "Do phu").
Bu lai no van dat ~826 keypoint, thua nguong de ARCore/ARKit bam on dinh.

4 dau hieu o 4 goc deu KHAC NHAU (goc vuong / vong tron / goc vuong nguoc /
dau cong) de khoa huong: nhan khong con doi xung khi xoay 90/180 do, tranh
truong hop con rong quay lung vi thuat toan khop nham huong.
"""

from __future__ import annotations

import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from marker_lib import score_marker, passes_minimum_bar

# ---------------------------------------------------------------------------
# Cau hinh chung
# ---------------------------------------------------------------------------

MARKER_SIZE_MM = 200          # PHAI khop voi SetSize() trong XRReferenceImageLibrary
DPI = 300
SEED = 20260905               # co dinh de chay lai ra dung 1 ket qua, khong ngau nhien moi lan

STATION_CODE = "589CBAE0"     # 8 ky tu dau cua device id tren ThingsBoard

# Bang mau - do truc tiep tu ban nhan goc de giu dung nhan dien thuong hieu.
BG = (10, 31, 24)              # nen xanh rat toi
PANEL_DARK = (6, 20, 15)       # manh toi trong mang low-poly (toi hon ca nen)
LIGHT = (238, 248, 243)        # trang nga - manh sang / o sang / chu tieu de
SAGE = (120, 168, 148)         # xanh xam - manh trung gian / dong chu phu
ACCENT = (46, 214, 143)        # xanh la sang - dau goc, chu nhan manh

# ---------------------------------------------------------------------------
# Bo cuc - tat ca tinh bang MILIMET tren nhan 200mm that, khong phai pixel.
# Nho vay ban in 2362px va ban Unity 1024px ra cung mot bo cuc, khong lech.
# ---------------------------------------------------------------------------

PAD_MM = 7.0                   # vien trang quanh nhan (giup tach nhan khoi nen khi dan)
MARGIN_MM = 15.5               # le trai cua chu va cac khoi

CORNER_MARGIN_MM = 10.9        # tam dau goc cach mep nhan
CORNER_SIZE_MM = 10.0
CORNER_STROKE_MM = 2.0

TITLE_BASELINE_MM = 40.4       # "DRAGON EDEN"
TITLE_SIZE_MM = 17.2
SUBTITLE_BASELINE_MM = 49.6    # "TRAM QUAN TRAC MOI TRUONG"
SUBTITLE_SIZE_MM = 7.35
STATION_BASELINE_MM = 57.0     # "STATION ... - LONG MACH DAT LANH"
STATION_SIZE_MM = 4.75

PANEL_TOP_MM = 63.1            # dai dac trung (2 khoi giua)
PANEL_BOTTOM_MM = 153.3
POLY_LEFT_MM = 13.1
POLY_RIGHT_MM = 108.2
GRID_LEFT_MM = 111.9
GRID_RIGHT_MM = 189.8

POLY_COLS = 7                  # so o luoi truoc khi jitter
POLY_ROWS = 10
# Xe dich theo CHIEU NGANG manh hon chieu doc: dinh bi keo ngang nhieu se bien o
# vuong thanh manh dai xien (kieu kinh vo), thay vi tam giac deu can doi - dung
# ve ngoai cua ban nhan goc, va canh xien dai cung cho nhieu huong gradient hon.
POLY_JITTER_X = 0.55
POLY_JITTER_Y = 0.34

GRID_CELL_MM = 3.52            # canh 1 o vuong trong luoi ngau nhien
GRID_FILL = 0.46               # ti le o duoc to - thua de tao canh, vua de khong bi ret

WAVE_BASELINE_MM = 179.3       # day cac thanh waveform
WAVE_MAX_HEIGHT_MM = 10.2
WAVE_LEFT_MM = 14.6
WAVE_RIGHT_MM = 183.6
WAVE_BAR_MM = 1.9
WAVE_GAP_MM = 1.6

CAPTION_BASELINE_MM = 184.2    # "SOI DE XEM DU LIEU TRUC TIEP"
CAPTION_SIZE_MM = 3.7
# Caption thut vao sau hon le trai chung: no nam CUNG HANG NGANG voi dau goc
# duoi-trai, nen phai lui qua khoi be rong dau goc do. Day dung la loi da mac 2
# lan o cac ban nhan truoc (chu de chong len dau goc) - gio tinh thang tu vi tri
# dau goc thay vi doan mot con so.
CAPTION_LEFT_MM = CORNER_MARGIN_MM + CORNER_SIZE_MM + 9.6

REPO_ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = REPO_ROOT / "Assets" / "Art" / "Markers"
OUT_PRINT = OUT_DIR / "T_Marker_DragonEden_200mm_300dpi.png"
OUT_UNITY = OUT_DIR / "T_Marker_DragonEden.png"

# Font co dau tieng Viet. Thu lan luot theo he dieu hanh - may Mac dung de build
# iOS cung phai chay duoc script nay, khong chi rieng may Windows.
FONT_CANDIDATES = {
    "bold": [
        "C:/Windows/Fonts/arialbd.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ],
    "regular": [
        "C:/Windows/Fonts/arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ],
}


class Canvas:
    """Gom anh + he quy doi mm->px lai mot cho, de moi ham ve chi can noi
    chuyen bang milimet va khong ham nao phai tu nho ti le."""

    def __init__(self, size_px: int):
        self.size_px = size_px
        self.px_per_mm = size_px / MARKER_SIZE_MM
        self.image = Image.new("RGB", (size_px, size_px), (255, 255, 255))
        self.draw = ImageDraw.Draw(self.image)
        self.rng = random.Random(SEED)

    def px(self, mm: float) -> int:
        return int(round(mm * self.px_per_mm))

    def font(self, weight: str, size_mm: float) -> ImageFont.FreeTypeFont:
        size_px = self.px(size_mm)
        for path in FONT_CANDIDATES[weight]:
            if Path(path).exists():
                return ImageFont.truetype(path, size_px)
        raise FileNotFoundError(
            f"Khong tim thay font '{weight}'. Them duong dan font co dau tieng Viet "
            f"vao FONT_CANDIDATES trong {__file__}."
        )


def draw_background(c: Canvas) -> None:
    pad = c.px(PAD_MM)
    c.draw.rectangle([pad, pad, c.size_px - pad - 1, c.size_px - pad - 1], fill=BG)


def draw_corner_marks(c: Canvas) -> None:
    """4 dau KHAC NHAU o 4 goc - khoa huong nhan (xem ghi chu dau file)."""
    m = c.px(CORNER_MARGIN_MM)
    s = c.px(CORNER_SIZE_MM)
    t = max(2, c.px(CORNER_STROKE_MM))
    far = c.size_px - m

    # Tren-trai: goc vuong mo xuong-phai
    c.draw.rectangle([m, m, m + s, m + t], fill=ACCENT)
    c.draw.rectangle([m, m, m + t, m + s], fill=ACCENT)

    # Tren-phai: vong tron rong
    c.draw.ellipse([far - s, m, far, m + s], outline=ACCENT, width=t)

    # Duoi-trai: goc vuong mo len-phai
    c.draw.rectangle([m, far - t, m + s, far], fill=ACCENT)
    c.draw.rectangle([m, far - s, m + t, far], fill=ACCENT)

    # Duoi-phai: dau cong
    mid = far - s // 2
    c.draw.rectangle([far - s, mid - t // 2, far, mid + t // 2], fill=ACCENT)
    c.draw.rectangle([mid - t // 2, far - s, mid + t // 2, far], fill=ACCENT)


def draw_header(c: Canvas) -> None:
    """3 dong chu - chi phuc vu nguoi doc, khong phai nguon dac trung chinh."""
    x = c.px(MARGIN_MM)

    # anchor "ls" = neo theo DUONG CHAN CHU (baseline) thay vi dinh hop bao.
    # Hop bao thay doi theo dau thanh tieng Viet (chu co dau nang tut xuong duoi,
    # dau mu doi len tren), neo theo hop se lam 3 dong nhay len xuong khong deu.
    c.draw.text((x, c.px(TITLE_BASELINE_MM)), "DRAGON EDEN",
                font=c.font("bold", TITLE_SIZE_MM), fill=LIGHT, anchor="ls")
    c.draw.text((x, c.px(SUBTITLE_BASELINE_MM)), "TRẠM QUAN TRẮC MÔI TRƯỜNG",
                font=c.font("bold", SUBTITLE_SIZE_MM), fill=ACCENT, anchor="ls")
    c.draw.text((x, c.px(STATION_BASELINE_MM)),
                f"STATION {STATION_CODE} · LONG MẠCH ĐẤT LÀNH",
                font=c.font("regular", STATION_SIZE_MM), fill=SAGE, anchor="ls")


def draw_lowpoly_panel(c: Canvas) -> None:
    """Mang tam giac - nguon canh XIEN nhieu huong khac nhau.

    Dung luoi diem co jitter roi cat cheo moi o thanh 2 tam giac: jitter manh
    bien cac o vuong deu nhau thanh manh dai xien, nen khong co 2 vung nao
    trong mang giong nhau - dung dieu thuat toan so khop can.
    """
    left, right = c.px(POLY_LEFT_MM), c.px(POLY_RIGHT_MM)
    top, bottom = c.px(PANEL_TOP_MM), c.px(PANEL_BOTTOM_MM)
    cell_w = (right - left) / POLY_COLS
    cell_h = (bottom - top) / POLY_ROWS
    jitter_x, jitter_y = cell_w * POLY_JITTER_X, cell_h * POLY_JITTER_Y

    # Dinh luoi da xe dich. Dinh nam tren VIEN thi khong xe ra ngoai, de mang
    # giu duoc canh thang - khong bi rang cua lom chom o bon mep.
    points = []
    for r in range(POLY_ROWS + 1):
        row = []
        for col in range(POLY_COLS + 1):
            x = left + col * cell_w
            y = top + r * cell_h
            if 0 < col < POLY_COLS:
                x += c.rng.uniform(-jitter_x, jitter_x)
            if 0 < r < POLY_ROWS:
                y += c.rng.uniform(-jitter_y, jitter_y)
            row.append((x, y))
        points.append(row)

    palette = [LIGHT, LIGHT, ACCENT, ACCENT, SAGE, PANEL_DARK, PANEL_DARK]
    for r in range(POLY_ROWS):
        for col in range(POLY_COLS):
            tl, tr = points[r][col], points[r][col + 1]
            bl, br = points[r + 1][col], points[r + 1][col + 1]
            # Doi chieu cat cheo theo o de khong tao ra mot day tam giac cung
            # huong chay suot mang (trong se thanh hoa tiet lap, dung thu can tranh).
            if (r + col) % 2 == 0:
                tris = [(tl, tr, bl), (tr, br, bl)]
            else:
                tris = [(tl, tr, br), (tl, br, bl)]
            for tri in tris:
                c.draw.polygon(tri, fill=c.rng.choice(palette))


def draw_module_grid(c: Canvas) -> None:
    """Luoi o vuong ngau nhien - nguon goc VUONG sac net.

    Co chu dich de trong ~54% so o: cac o trong tao khoang ho quanh moi o duoc
    to, nho do moi o gop du 4 goc nhon vao ban do dac trung. To kin het se
    thanh mot mang lien khoi, so goc do duoc lai IT hon.
    """
    left, right = c.px(GRID_LEFT_MM), c.px(GRID_RIGHT_MM)
    top, bottom = c.px(PANEL_TOP_MM), c.px(PANEL_BOTTOM_MM)
    cell = max(3, c.px(GRID_CELL_MM))

    cols = (right - left) // cell
    rows = (bottom - top) // cell
    # Can giua phan du sau khi chia het cho cell, khong don het ve mot phia.
    ox = left + ((right - left) - cols * cell) // 2
    oy = top + ((bottom - top) - rows * cell) // 2

    for r in range(rows):
        for col in range(cols):
            if c.rng.random() >= GRID_FILL:
                continue
            x0, y0 = ox + col * cell, oy + r * cell
            # Chua khoang ho 1px giua cac o ke nhau de hai o cung mau nam sat
            # nhau van con duong bien - khong dinh thanh mot khoi lon.
            c.draw.rectangle([x0, y0, x0 + cell - 2, y0 + cell - 2],
                             fill=LIGHT if c.rng.random() < 0.62 else ACCENT)


def draw_waveform(c: Canvas) -> None:
    """Dai thanh doc goi y "du lieu dang chay" - trang tri cho nguoi xem."""
    baseline = c.px(WAVE_BASELINE_MM)
    max_h = c.px(WAVE_MAX_HEIGHT_MM)
    bar_w = max(2, c.px(WAVE_BAR_MM))
    step = bar_w + max(1, c.px(WAVE_GAP_MM))

    x = c.px(WAVE_LEFT_MM)
    right = c.px(WAVE_RIGHT_MM)
    while x + bar_w <= right:
        h = int(max_h * c.rng.uniform(0.18, 1.0))
        c.draw.rectangle([x, baseline - h, x + bar_w - 1, baseline],
                         fill=ACCENT if c.rng.random() < 0.5 else LIGHT)
        x += step


def draw_caption(c: Canvas) -> None:
    c.draw.text((c.px(CAPTION_LEFT_MM), c.px(CAPTION_BASELINE_MM)),
                "SOI ĐỂ XEM DỮ LIỆU TRỰC TIẾP",
                font=c.font("bold", CAPTION_SIZE_MM), fill=LIGHT, anchor="ls")


def build_marker(size_px: int) -> Image.Image:
    c = Canvas(size_px)
    draw_background(c)
    draw_header(c)
    draw_lowpoly_panel(c)
    draw_module_grid(c)
    draw_waveform(c)
    draw_caption(c)
    # Ve dau goc SAU CUNG de khong bi 2 khoi hoa tiet ve de len.
    draw_corner_marks(c)
    return c.image


def main() -> None:
    print_px = int(round(MARKER_SIZE_MM * DPI / 25.4))
    marker = build_marker(print_px)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    marker.save(OUT_PRINT, dpi=(DPI, DPI))
    marker.resize((1024, 1024), Image.LANCZOS).save(OUT_UNITY)

    print(f"Da tao:\n  in   : {OUT_PRINT}  ({print_px}x{print_px}px @ {DPI}DPI = {MARKER_SIZE_MM}mm)")
    print(f"  unity: {OUT_UNITY}  (1024x1024)")

    scores = score_marker(str(OUT_UNITY))
    ok, reasons = passes_minimum_bar(scores)
    print("\nCham diem (do tren ban 1024x1024, giong nhu Unity/camera se thay):")
    for key, value in scores.items():
        print(f"  {key:22s} {value}")

    if ok:
        print("\n=> DAT nguong toi thieu, du dung de bam AR.")
    else:
        print("\n=> CHUA DAT nguong toi thieu:")
        for reason in reasons:
            print(f"   - {reason}")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
