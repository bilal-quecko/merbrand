"""Renders a preview of MBPBackgroundLoop.shader outside Unity.
The maths here mirrors the shader term for term so the preview is honest."""
import numpy as np, subprocess, sys
from PIL import Image

TAU = 6.28318530718
SRC = '/mnt/user-data/uploads/MeraBrandPakistan.png'
OUT_W, OUT_H, FPS, LOOP_SECONDS = 1280, 720, 24, 8.0
FRAMES = int(FPS * LOOP_SECONDS)

# --- shader parameters (same defaults as the .shader) ---
DRIFT, PAN = 0.016, 0.0025
FLAG_RECT = (0.00, 0.54, 0.21, 0.96); FLAG_AMP, FLAG_FREQ, FLAG_CYCLES = 0.0055, 26.0, 2.0
RIBBON_TOP, RIBBON_I, RIBBON_FREQ, RIBBON_CYCLES, RIBBON_SHARP = 0.34, 1.15, 13.0, 1.0, 4.0
MAP_RECT = (0.55, 0.50, 0.94, 1.00); MAP_PULSE, MAP_CYCLES, SCAN_I, SCAN_W = 0.40, 2.0, 0.8, 0.13
LOGO_RECT = (0.31, 0.62, 0.72, 0.91); SHIMMER, SHIMMER_W = 0.7, 0.09
TWINKLE, HAZE, VIG_PULSE = 0.35, 0.30, 0.05

src = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32) / 255.0
SH, SW, _ = src.shape

ux = (np.arange(OUT_W) + 0.5) / OUT_W
uy = 1.0 - (np.arange(OUT_H) + 0.5) / OUT_H          # uv.y = 0 at the bottom, as in Unity
U0, V0 = np.meshgrid(ux, uy)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def rect_mask(u, v, r, feather):
    return (smoothstep(r[0], r[0] + feather, u) * smoothstep(r[1], r[1] + feather, v)
            * smoothstep(r[2], r[2] - feather, u) * smoothstep(r[3], r[3] - feather, v))


def sweep(coord, t, width):
    pad = width * 2.0
    center = -pad + t * (1.0 + pad * 2.0)
    d = np.abs(coord - center) / width
    return np.exp(-d * d * 3.0)


def sample(u, v):
    """bilinear tex2D"""
    x = np.clip(u, 0.0015, 0.9985) * (SW - 1)
    y = (1.0 - np.clip(v, 0.0015, 0.9985)) * (SH - 1)
    x0 = np.floor(x).astype(np.int32); y0 = np.floor(y).astype(np.int32)
    x1 = np.minimum(x0 + 1, SW - 1);   y1 = np.minimum(y0 + 1, SH - 1)
    fx = (x - x0)[..., None];          fy = (y - y0)[..., None]
    return ((src[y0, x0] * (1 - fx) + src[y0, x1] * fx) * (1 - fy)
            + (src[y1, x0] * (1 - fx) + src[y1, x1] * fx) * fy)


rng = np.random.default_rng(7)
star_hash = rng.random((OUT_H, OUT_W))
star_mask = (star_hash > 0.9955).astype(np.float32)
star_phase = star_hash * TAU


def frame(t):
    zoom = 1.0 + DRIFT * (0.5 - 0.5 * np.cos(TAU * t))
    u = (U0 - 0.5) / zoom + 0.5 + PAN * np.sin(TAU * t)
    v = (V0 - 0.5) / zoom + 0.5 + PAN * 0.5 * np.cos(TAU * t)

    fm = rect_mask(u, v, FLAG_RECT, 0.05)
    fall = np.clip((u - FLAG_RECT[0]) / (FLAG_RECT[2] - FLAG_RECT[0]), 0, 1) ** 2
    v = v + np.sin(u * FLAG_FREQ - TAU * t * FLAG_CYCLES + v * 4.0) * FLAG_AMP * fm * fall
    u = u + np.cos(v * FLAG_FREQ * 0.6 - TAU * t * FLAG_CYCLES) * FLAG_AMP * 0.35 * fm * fall

    col = sample(u, v)
    lum = col @ np.array([0.299, 0.587, 0.114], np.float32)
    gb = np.clip(col[..., 1] - col[..., 2], 0, 1)

    band = 1.0 - smoothstep(RIBBON_TOP * 0.55, RIBBON_TOP, v)
    rmask = smoothstep(0.05, 0.32, lum) * smoothstep(0.015, 0.11, gb) * band
    flow = (0.5 + 0.5 * np.sin(u * RIBBON_FREQ - TAU * t * RIBBON_CYCLES)) ** RIBBON_SHARP
    col = col + (rmask * flow * RIBBON_I)[..., None] * np.array([0.30, 1.0, 0.50], np.float32)

    mapm = rect_mask(u, v, MAP_RECT, 0.07)
    not_stone = 1.0 - smoothstep(0.05, 0.22, col[..., 0])
    nodem = smoothstep(0.09, 0.38, lum) * not_stone * mapm
    pulse = 0.5 + 0.5 * np.sin(TAU * t * MAP_CYCLES + u * 9.0 + v * 5.0)
    col = col + (nodem * pulse * MAP_PULSE)[..., None] * np.array([0.35, 1.0, 0.72], np.float32)

    diag = np.clip((u - MAP_RECT[0]) / (MAP_RECT[2] - MAP_RECT[0]) * 0.65
                   + (1 - np.clip((v - MAP_RECT[1]) / (MAP_RECT[3] - MAP_RECT[1]), 0, 1)) * 0.35, 0, 1)
    col = col + (nodem * sweep(diag, t, SCAN_W) * SCAN_I)[..., None] * np.array([0.45, 1.0, 0.80], np.float32)

    logom = rect_mask(u, v, LOGO_RECT, 0.03) * smoothstep(0.30, 0.75, lum)
    lx = np.clip((u - LOGO_RECT[0]) / (LOGO_RECT[2] - LOGO_RECT[0]), 0, 1)
    col = col + (logom * sweep(lx, t, SHIMMER_W) * SHIMMER)[..., None] * np.array([0.85, 1.0, 0.90], np.float32)

    star = star_mask * (0.5 + 0.5 * np.sin(TAU * t * 2.0 + star_phase))
    sky = smoothstep(0.55, 0.85, v) * (1.0 - smoothstep(0.06, 0.22, lum))
    col = col + (star * sky * TWINKLE)[..., None] * np.array([0.7, 1.0, 0.9], np.float32)

    hz = smoothstep(0.18, 0.42, v) * (1.0 - smoothstep(0.42, 0.66, v))
    col = col + (hz * (0.5 + 0.5 * np.sin(TAU * t + u * 3.5)) * HAZE * 0.05)[..., None] \
              * np.array([0.25, 1.0, 0.65], np.float32)

    dx, dy = U0 - 0.5, V0 - 0.5
    vig = 1.0 - (dx * dx + dy * dy) * (0.55 + VIG_PULSE * (0.5 - 0.5 * np.cos(TAU * t)))
    col = col * np.clip(vig, 0, 1)[..., None]

    return (np.clip(col, 0, 1) * 255).astype(np.uint8)


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == 'stills':
        for t in (0.0, 0.25, 0.5, 0.75):
            Image.fromarray(frame(t)).save(f'/home/claude/still_{int(t*100):03d}.png')
        print('stills written')
    else:
        p = subprocess.Popen(
            ['ffmpeg', '-y', '-loglevel', 'error', '-f', 'rawvideo', '-pix_fmt', 'rgb24',
             '-s', f'{OUT_W}x{OUT_H}', '-r', str(FPS), '-i', '-',
             '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '18', '-movflags', '+faststart',
             '/home/claude/mbp-bg-loop-preview.mp4'], stdin=subprocess.PIPE)
        for i in range(FRAMES):
            p.stdin.write(frame(i / FRAMES).tobytes())
        p.stdin.close(); p.wait()
        print('video written')
