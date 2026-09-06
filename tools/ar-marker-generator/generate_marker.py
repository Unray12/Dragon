"""
Tao anh nhan AR (Image Target) don gian nhat co the, van giu du dac tinh de
bam tot trong ARCore Augmented Image / ARKit image detection.

CACH DUNG
    python generate_marker.py

Ghi de truc tiep 2 file trong Assets/Art/Markers/ (giu nguyen ten cu, KHONG
doi duong dan) de khong phai cap nhat lai XRReferenceImageLibrary trong Unity:
    T_Marker_DragonEden_200mm_300dpi.png   - ban de IN, 200mm @ 300 DPI
    T_Marker_DragonEden.png                - ban 1024x1024 cho Unity

VI SAO DON GIAN LAI VAN BAM TOT DUOC (xem README.md de biet day du ly do)
Ban dau tien co qua nhieu yeu to trang tri (mang low-poly, thanh waveform,
3 dong chu) - dep nhung khong yeu to nao trong so do THUC SU can thiet cho
tracking. Thu thiet yeu duy nhat la: 1 hoa tiet PHU DEU toan bo khung hinh,
TUONG PHAN cao, va BAT DOI XUNG (khong lap lai giong nhau khi xoay/lat). Ban
nay rut con dung 3 thanh phan:
  1. Luoi o vuong 2 mau ngau nhien, PHU TOAN BO khung - dam bao moi vung nho
     tren nhan deu co canh/goc de thuat toan so khop, du nguoi dung soi trung
     mep hay trung giua.
  2. 4 dau hieu GOC KHAC NHAU (vuong/tron/tam giac/cong) - khoa huong, tranh
     nham nhan bi xoay 90/180 do (da dung tu ban truoc, giu nguyen vi da kiem
     chung hoat dong).
  3. 1 dong ten thuong hieu duy nhat - cho nguoi, khong anh huong tracking.
"""

from __future__ import annotations

import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from marker_lib import score_marker, passes_minimum_bar

# ---------------------------------------------------------------------------
# Cau hinh
# ---------------------------------------------------------------------------

MARKER_SIZE_MM = 200          # PHAI khop voi SetSize() trong XRReferenceImageLibrary
DPI = 300
SEED = 20260905               # co dinh de tao lai ra dung 1 ket qua, khong ngau nhien moi lan chay

# Kich thuoc 1 o luoi tinh bang mm - 7mm la diem can bang: du nho de tao nhieu
# canh (nhieu dac trung), du to de khong bi mo/alias khi anh giam con 1024px
# cho Unity hoac khi camera dung xa nhan (~30-60cm).
CELL_SIZE_MM = 7.0

BG = (8, 15, 13)               # nen toi
LIGHT = (232, 244, 238)        # o sang
DARK = (16, 92, 63)            # o toi (van sang hon BG - giu 2 muc, khong phai den tuyet doi)
ACCENT = (46, 214, 143)        # dau goc + chu

REPO_ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = REPO_ROOT / "Assets" / "Art" / "Markers"
OUT_PRINT = OUT_DIR / "T_Marker_DragonEden_200mm_300dpi.png"
OUT_UNITY = OUT_DIR / "T_Marker_DragonEden.png"


def _mm_to_px(mm: float, px_per_mm: float) -> int:
    return int(round(mm * px_per_mm))


def _corner_markers(draw: ImageDraw.ImageDraw, px: int, pad: int) -> None:
    """4 dau hieu KHAC NHAU o 4 goc - khoa huong nhan, tranh nham xoay 90/180."""
    m = pad + int(px * 0.018)
    s = int(px * 0.048)
    t = max(2, int(px * 0.010))

    # Top-left: goc vuong (L-shape)
    draw.rectangle([m, m, m + s, m + t], fill=ACCENT)
    draw.rectangle([m, m, m + t, m + s], fill=ACCENT)
    # Top-right: vong tron rong
    draw.ellipse([px - m - s, m, px - m, m + s], outline=ACCENT, width=t)
    # Bottom-left: tam giac dac
    draw.polygon([(m, px - m), (m + s, px - m), (m, px - m - s)], fill=ACCENT)
    # Bottom-right: dau cong
    draw.rectangle([px - m - s, px - m - s // 2 - t // 2, px - m, px - m - s // 2 + t // 2], fill=ACCENT)
    draw.rectangle([px - m - s // 2 - t // 2, px - m - s, px - m - s // 2 + t // 2, px - m], fill=ACCENT)


def build_marker(px: int) -> Image.Image:
    px_per_mm = px / MARKER_SIZE_MM
    rng = random.Random(SEED)

    img = Image.new("RGB", (px, px), (255, 255, 255))
    draw = ImageDraw.Draw(img)

    pad = _mm_to_px(6, px_per_mm)
    draw.rectangle([pad, pad, px - pad, px - pad], fill=BG)

    # Dai duoi cung danh cho 1 dong chu duy nhat - luoi KHONG ve de vao day de
    # chu khong bi la lien voi hoa tiet (van giu vung nay du nho, phan lon
    # dien tich nhan van la luoi dac trung).
    caption_h = _mm_to_px(11, px_per_mm)
    grid_top = pad + _mm_to_px(4, px_per_mm)
    grid_bottom = px - pad - caption_h

    cell = max(4, _mm_to_px(CELL_SIZE_MM, px_per_mm))
    inner_left, inner_right = pad + _mm_to_px(4, px_per_mm), px - pad - _mm_to_px(4, px_per_mm)
    cols = (inner_right - inner_left) // cell
    rows = (grid_bottom - grid_top) // cell
    grid_w, grid_h = cols * cell, rows * cell
    # Can giua phan du ra sau khi chia het cho cell, khong de lech ve 1 phia.
    ox = inner_left + ((inner_right - inner_left) - grid_w) // 2
    oy = grid_top + ((grid_bottom - grid_top) - grid_h) // 2

    # Luoi 2 mau random - PHU DEU toan bo khung, ~50/50 de giu tuong phan cao.
    # Day la thanh phan DUY NHAT tao ra hau het dac trung cho tracking.
    for r in range(rows):
        for c in range(cols):
            if rng.random() < 0.5:
                x0, y0 = ox + c * cell, oy + r * cell
                draw.rectangle([x0, y0, x0 + cell - 1, y0 + cell - 1],
                                fill=LIGHT if rng.random() < 0.6 else DARK)

    _corner_markers(draw, px, pad)

    # 1 dong chu duy nhat, dat trong dai duoi - KHONG dam vao vung luoi VA
    # khong dam vao dau tam giac o goc duoi-trai (da tung mac loi nay: chu de
    # len ngay tren dau goc, xem DECISIONS_LOG). Le trai cua chu phai lui qua
    # khoi be rong dau goc (~s + m, xem _corner_markers).
    font_size = _mm_to_px(6.2, px_per_mm)
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", font_size)
    text_y = px - pad - caption_h + (caption_h - font_size) // 2
    corner_clearance = pad + int(px * 0.018) + int(px * 0.048) + int(px * 0.012)
    draw.text((corner_clearance, text_y), "DRAGON EDEN · TRẠM QUAN TRẮC", font=font, fill=ACCENT)

    return img


def main() -> None:
    px = _mm_to_px(MARKER_SIZE_MM, DPI / 25.4)
    marker = build_marker(px)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    marker.save(OUT_PRINT, dpi=(DPI, DPI))

    unity_copy = marker.resize((1024, 1024), Image.LANCZOS)
    unity_copy.save(OUT_UNITY)

    print(f"Da tao:\n  in   : {OUT_PRINT}  ({px}x{px}px @ {DPI}DPI = {MARKER_SIZE_MM}mm)")
    print(f"  unity: {OUT_UNITY}  (1024x1024)")

    scores = score_marker(str(OUT_UNITY))
    ok, reasons = passes_minimum_bar(scores)
    print("\nCham diem (do tren ban 1024x1024, giong nhu Unity/camera se thay):")
    for k, v in scores.items():
        print(f"  {k:22s} {v}")

    if ok:
        print("\n=> DAT nguong toi thieu, du dung de bam AR.")
    else:
        print("\n=> CHUA DAT nguong toi thieu:")
        for r in reasons:
            print(f"   - {r}")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
