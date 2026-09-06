"""
Thu vien dung chung: dung anh nhan AR + cham diem do giau dac trung.

Tach rieng khoi generate_marker.py de logic "ve" va logic "do" khong lan vao
nhau - muon doi thiet ke thi sua o day, muon doi tieu chi cham diem thi cung
sua o day, khong dam vao script CLI.

Vi sao nhung tieu chi nay quan trong (xem chi tiet trong README.md o thu muc
nay): ARCore/ARKit Image Tracking so khop bang DAC TRUNG ANH CO DIEN (Harris/
FAST-like keypoint + descriptor), khong phai machine learning. Anh cang nhieu
goc/canh, tuong phan cang cao, cang bam tot. Anh mo/it chi tiet/lap hoa tiet se
bam kem hoac khong bam duoc.
"""

from __future__ import annotations

import numpy as np
from PIL import Image


# ---------------------------------------------------------------------------
# Cham diem: do lai chinh xac phuong phap da dung khi thiet ke nhan dau tien
# (Harris keypoint tho + nang luong gradient tung o luoi) - KHONG dung OpenCV,
# de script nay chi can Pillow + numpy, khong them dependency nao khac.
# ---------------------------------------------------------------------------

def _harris_response(gray: np.ndarray, k: float = 0.04, box_radius: int = 3) -> np.ndarray:
    """Tra ve ban do phan hoi Harris (cang cao = cang giong 1 goc/canh ro)."""
    iy, ix = np.gradient(gray)

    def box_sum(a: np.ndarray, r: int) -> np.ndarray:
        c = np.pad(np.cumsum(np.cumsum(a, axis=0), axis=1), ((1, 0), (1, 0)))
        s = c[2 * r:, 2 * r:] - c[:-2 * r, 2 * r:] - c[2 * r:, :-2 * r] + c[:-2 * r, :-2 * r]
        return np.pad(s, ((r, r), (r, r)), mode="edge")[: a.shape[0], : a.shape[1]]

    sxx, syy, sxy = box_sum(ix * ix, box_radius), box_sum(iy * iy, box_radius), box_sum(ix * iy, box_radius)
    return (sxx * syy - sxy ** 2) - k * (sxx + syy) ** 2


def score_marker(image_path: str, grid: int = 8, sample_size: int = 800) -> dict:
    """
    Cham diem 1 file anh nhan. Tra ve dict de in ra hoac assert trong test.

    - keypoints: so diem dac trung tho (cang nhieu cang de bam)
    - grid_coverage_pct: % o luoi (grid x grid) co it nhat 1 vung nang luong
      gradient dang ke - PHAI cao, neu khong nguoi dung soi trung tam nhan se
      mat bam vi vung do khong co gi de so khop.
    - contrast_std: do lech chuan luminance toan anh, >0.20 la tot
    """
    img = Image.open(image_path).convert("L").resize((sample_size, sample_size), Image.LANCZOS)
    gray = np.asarray(img, dtype=np.float64) / 255.0

    iy, ix = np.gradient(gray)
    grad_energy = np.hypot(ix, iy)

    response = _harris_response(gray)
    threshold = np.quantile(response, 0.995)
    keypoints_mask = response > threshold

    # Non-max suppression tho: gop moi o 4x4 thanh 1 diem, tranh dem trung 1
    # goc nhieu lan do lam min voi anh nhieu.
    h = (sample_size // 4) * 4
    coarse = keypoints_mask[:h, :h].reshape(h // 4, 4, h // 4, 4).any(axis=(1, 3))
    keypoint_count = int(coarse.sum())

    cell = sample_size // grid
    energy_per_cell = grad_energy[: grid * cell, : grid * cell].reshape(
        grid, cell, grid, cell
    ).mean(axis=(1, 3))
    covered_cells = int((energy_per_cell > grad_energy.mean() * 0.45).sum())

    return {
        "keypoints": keypoint_count,
        "grid_coverage_pct": round(100.0 * covered_cells / (grid * grid), 1),
        "contrast_std": round(float(gray.std()), 3),
        "mean_gradient_energy": round(float(grad_energy.mean()), 4),
    }


# Nguong toi thieu de coi la "du bam tot" - dung chung cho moi lan tao nhan
# moi, tranh moi nguoi tu dat 1 con so khac nhau roi quen mat vi sao chon.
# Tham khao Google arcoreimg: khuyen nghi diem chat luong >= 75/100; hai tieu
# chi duoi day la proxy tuong duong do bang cong cu thuan Python.
MIN_KEYPOINTS = 400
MIN_GRID_COVERAGE_PCT = 90.0
MIN_CONTRAST_STD = 0.20


def passes_minimum_bar(scores: dict) -> tuple[bool, list[str]]:
    """Doi chieu 1 ket qua score_marker() voi nguong toi thieu. Tra ve
    (dat/khong dat, danh sach ly do neu khong dat)."""
    reasons = []
    if scores["keypoints"] < MIN_KEYPOINTS:
        reasons.append(f"keypoints {scores['keypoints']} < {MIN_KEYPOINTS}")
    if scores["grid_coverage_pct"] < MIN_GRID_COVERAGE_PCT:
        reasons.append(f"grid_coverage_pct {scores['grid_coverage_pct']} < {MIN_GRID_COVERAGE_PCT}")
    if scores["contrast_std"] < MIN_CONTRAST_STD:
        reasons.append(f"contrast_std {scores['contrast_std']} < {MIN_CONTRAST_STD}")
    return (len(reasons) == 0, reasons)
